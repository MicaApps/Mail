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
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Mail.Pages
{
    public sealed partial class MailFolderDetailsPage : Page
    {
        private readonly AsyncLock SelectionChangeLocker = new AsyncLock();
        private MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel> PreviewSource;

        public MailFolderDetailsPage()
        {
            InitializeComponent();
            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MailFolderType Type && e.NavigationMode == NavigationMode.New)
            {
                PreviewSource?.Clear();
                DetailsView.SelectedItem = null;
                EmptyContentText.Text = "Syncing you email";

                try
                {
                    OutlookService Service = App.Services.GetService<OutlookService>();
                    MailFolderDetailData MailFolder = await Service.GetMailFolderDetailAsync(Type);

                    DetailsView.ItemsSource = PreviewSource = new MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel>(Service, MailFolder, (Data) => new MailMessageListDetailViewModel(Data));

                    await foreach (MailMessageData MessageData in Service.GetMailMessageAsync(MailFolder.Id))
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
    }
}
