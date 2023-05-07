using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Mail.Services.Collection;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace Mail.Pages
{
    public sealed partial class MailFolderDetailsPage : Page
    {
        private readonly AsyncLock SelectionChangeLocker = new AsyncLock();
        private MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel> PreviewSource;
        private MailFolderData navigationData;
        private bool IsFocusedTab { get; set; } = true;

        public MailFolderDetailsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MailFolderData data && e.NavigationMode == NavigationMode.New)
            {
                navigationData = data;
                FolderName.Text = data.Name;
                IMailService Service = App.Services.GetService<OutlookService>();
                if (data.Type == MailFolderType.Inbox && Service is IMailService.IFocusFilterSupport)
                {
                    NavigationTab.Visibility = Visibility.Visible;
                    FolderName.Visibility = Visibility.Collapsed;
                    var isPreviousSelected = FocusedTab.IsSelected;
                    FocusedTab.IsSelected = true;
                    if (isPreviousSelected)
                    {
                        await RefreshData();
                    }
                }
                else
                {
                    NavigationTab.Visibility = Visibility.Collapsed;
                    FolderName.Visibility = Visibility.Visible;
                    await RefreshData();
                }
                
            }
            else
            {
                navigationData = null;
            }
        }

        private async Task RefreshData()
        {
            PreviewSource?.Clear();
            DetailsView.SelectedItem = null;
            EmptyContentText.Text = "Syncing you email";
            var data = navigationData;
            if (data == null) return;
            try
            {
                IMailService Service = App.Services.GetService<OutlookService>();
                MailFolderDetailData MailFolder = await Service.GetMailFolderDetailAsync(data.Id);

                DetailsView.ItemsSource = PreviewSource = new MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel>(Service, data.Type, MailFolder, (Data) => new MailMessageListDetailViewModel(Data), IsFocusTab: IsFocusedTab);

                IAsyncEnumerable<MailMessageData> dataSet;
                if (data.Type == MailFolderType.Inbox && Service is IMailService.IFocusFilterSupport FilterService)
                {
                    dataSet = FilterService.GetMailMessageAsync(data.Id, IsFocusedTab);
                }
                else
                {
                    dataSet = Service.GetMailMessageAsync(MailFolder.Id);
                }
                await foreach (MailMessageData MessageData in dataSet)
                {
                    PreviewSource.Add(new MailMessageListDetailViewModel(MessageData));
                }

                if (PreviewSource.Count == 0)
                {
                    EmptyContentText.Text = "No available email";
                }
            }
            catch (Exception)
            {
                EmptyContentText.Text = "Sync failed";
            }
        }

        private async void ListDetailsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is MailMessageListDetailViewModel Model)
            {
                if (sender is ListDetailsView View)
                {
                    using (await SelectionChangeLocker.LockAsync())
                    {
                        for (int Retry = 0; Retry < 10; Retry++)
                        {
                            if (View.FindChildOfType<WebView>() is WebView Browser)
                            {
                                Browser.Height = 100;
                                if (Model.ContentType == MailMessageContentType.Text)
                                {
                                    Browser.NavigateToString(@$"<html><head><style type=""text/css"">body{{color: #000; background-color: transparent;}}</style></head><body>{Model.Content.Replace("cid:", "http://cid.resource.application/")}</body></html>");
                                }
                                else
                                {
                                    Browser.NavigateToString(Model.Content.Replace("cid:", "http://cid.resource.application/"));
                                }

                                break;
                            }

                            await Task.Delay(300);
                        }
                    }
                }
            }
        }

        private async Task SetWebviewHeight(WebView webView)
        {
            var heightString = await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            if (int.TryParse(heightString, out int height))
            {
                webView.Height = height + 50;
            }
        }

        private async void Browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            await SetWebviewHeight((WebView)sender);
        }

        private void ListDetailsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListDetailsView View)
            {
                if (View.FindChildOfType<ListView>() is ListView InnerView)
                {
                    InnerView.IncrementalLoadingTrigger = IncrementalLoadingTrigger.Edge;
                    InnerView.IncrementalLoadingThreshold = 3;
                    InnerView.DataFetchSize = 3;
                }
            }
        }

        private void DetailsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (sender is ListDetailsView View)
            //{
                //View.DetailsPaneBackground = new SolidColorBrush(View.SelectedIndex >= 0 ? Colors.White : Colors.Transparent);
            //}
        }

        private void DetailsViewGoBack_Click(object sender, RoutedEventArgs e)
        {
            if (DetailsView.ViewState == ListDetailsViewState.Details)
            {
                DetailsView.SelectedItem = null;
            }
        }

        private void DetailsView_ViewStateChanged(object sender, ListDetailsViewState e)
        {
            if (sender is ListDetailsView View && View.FindChildOfName<Button>("DetailsViewGoBack") is Button DetailsViewGoBack)
            {
                DetailsViewGoBack.Visibility = DetailsView.ViewState == ListDetailsViewState.Details ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            IsFocusedTab = args.SelectedItemContainer == FocusedTab;
            await RefreshData();
        }

        private async void WebView_WebResourceRequested(WebView sender, WebViewWebResourceRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            IMailService Service = App.Services.GetService<OutlookService>();
            var uri = args.Request.RequestUri.AbsoluteUri;
            Trace.WriteLine($"web resource uri: {uri}");
            if (uri.StartsWith("http://cid.resource.application/"))
            {

                MailMessageListDetailViewModel Model = null;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (sender.DataContext is MailMessageListDetailViewModel model)
                    {
                        Model = model;
                    }
                });
                if (Model == null)
                {
                    Trace.WriteLine("model is null");
                    deferral.Complete();
                    return;
                }
                var resourceContent = await Service.GetMailMessageFileAttachmentContent(Model.Id, uri.Replace("http://cid.resource.application/", ""));
                if (resourceContent == null)
                {
                    deferral.Complete();
                } 
                else
                {
                    await sender.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        args.Response = new Windows.Web.Http.HttpResponseMessage()
                        {
                            Content = new HttpBufferContent(resourceContent.AsBuffer())
                        };
                    });
                    
                    deferral.Complete();
                }
                
            } else
            {
                deferral.Complete();
            }
            
        }
    }
}
