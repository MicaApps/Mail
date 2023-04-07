using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Mail.Services.Collection;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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
            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");
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
                    FocusedTab.IsSelected = true;
                    await RefreshData();
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
                            if (View.FindChildOfType<WebView2>() is WebView2 Browser)
                            {
                                await Browser.EnsureCoreWebView2Async();

                                if (Model.ContentType == MailMessageContentType.Text)
                                {
                                    Browser.NavigateToString(@$"<html><head><style type=""text/css"">body{{color: #000; background-color: transparent;}}</style></head><body>{Model.Content}</body></html>");
                                }
                                else
                                {
                                    Browser.NavigateToString(Model.Content);
                                }

                                break;
                            }

                            await Task.Delay(300);
                        }
                    }
                }
            }
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
            DetailsViewGoBack.Visibility = DetailsView.ViewState == ListDetailsViewState.Details ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            IsFocusedTab = args.SelectedItemContainer == FocusedTab;
            await RefreshData();
        }
    }
}
