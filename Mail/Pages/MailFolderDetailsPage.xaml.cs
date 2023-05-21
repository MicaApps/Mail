using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Mail.Services.Collection;
using Mail.Services.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Nito.AsyncEx;

namespace Mail.Pages
{
    public sealed partial class MailFolderDetailsPage : Page
    {
        private readonly AsyncLock SelectionChangeLocker = new();
        private MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel>? PreviewSource;
        private MailFolderData? NavigationData;
        private bool IsFocusedTab { get; set; } = true;

        public MailFolderDetailsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MailFolderData data && e.NavigationMode == NavigationMode.New)
            {
                NavigationData = data;
                FolderName.Text = data.Name;
                IMailService Service = App.Services.GetService<OutlookService>()!;
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
                NavigationData = null;
            }
        }

        private async Task RefreshData()
        {
            PreviewSource?.Clear();
            DetailsView.SelectedItem = null;
            EmptyContentText.Text = "Syncing you email";
            var data = NavigationData;
            if (data == null) return;
            try
            {
                IMailService Service = App.Services.GetService<OutlookService>()!;
                MailFolderDetailData MailFolder = await Service.GetMailFolderDetailAsync(data.Id);

                DetailsView.ItemsSource = PreviewSource =
                    new MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel>(Service, data.Type,
                        MailFolder, (Data) => new MailMessageListDetailViewModel(Data), IsFocusTab: IsFocusedTab);

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
            if ((e.OriginalSource as FrameworkElement)?.DataContext is not MailMessageListDetailViewModel Model) return;
            if (sender is not ListDetailsView View) return;
            using (await SelectionChangeLocker.LockAsync())
            {
                await LoadImageAndCacheAsync(Model);
                for (var Retry = 0; Retry < 10; Retry++)
                {
                    if (View.FindChildOfType<WebView>() is WebView Browser)
                    {
                        Browser.Height = 100;

                        var Replace = Rgx.Replace(Model.Content, ReplaceWord);

                        if (Model.ContentType == MailMessageContentType.Text)
                        {
                            Replace =
                                @$"<html><head><style type=""text/css"">body{{color: #000; background-color: transparent;}}</style></head><body>{Replace}</body></html>";
                        }

                        Browser.NavigateToString(Replace);
                        break;
                    }

                    await Task.Delay(300);
                }
            }
        }

        private readonly Regex Rgx = new("cid:[^\"]+");

        private static async Task LoadImageAndCacheAsync(MailMessageListDetailViewModel model)
        {
            IMailService Service = App.Services.GetService<OutlookService>()!;
            await Service.LoadAttachmentsAndCacheAsync(model.Id);
        }

        private string ReplaceWord(Capture match)
        {
            IMailService Service = App.Services.GetService<OutlookService>()!;

            var FileAttachment = Service.GetCache().Get<MailMessageFileAttachmentData>(match.Value.Replace("cid:", ""));
            return FileAttachment is null
                ? match.Value
                : $"data:{FileAttachment.ContentType};base64,{Convert.ToBase64String(FileAttachment.ContentBytes)}";
        }

        private async Task SetWebviewHeight(WebView webView)
        {
            var heightString =
                await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            if (int.TryParse(heightString, out int height))
            {
                webView.Height = height + 50;
            }
        }

        private async void Browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            await SetWebviewHeight(sender);
        }

        private void ListDetailsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListDetailsView View) return;
            if (View.FindChildOfType<ListView>() is not ListView InnerView) return;

            InnerView.IncrementalLoadingTrigger = IncrementalLoadingTrigger.Edge;
            InnerView.IncrementalLoadingThreshold = 3;
            InnerView.DataFetchSize = 3;
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
            if (sender is ListDetailsView View &&
                View.FindChildOfName<Button>("DetailsViewGoBack") is Button DetailsViewGoBack)
            {
                DetailsViewGoBack.Visibility = DetailsView.ViewState == ListDetailsViewState.Details
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender,
            Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            IsFocusedTab = args.SelectedItemContainer == FocusedTab;
            await RefreshData();
        }

        private async void WebView_WebResourceRequested(WebView sender, WebViewWebResourceRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            // TODO 这个方法是不是不需要了
            deferral.Complete();
        }

        private async void WebView_OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Trace.WriteLine($"WebViewNavigationStartingEventArgs: {args.Uri}");
            // 捕获WebView中发生的点击导航事件
            if (args.Uri?.ToString().Contains("click?") is not true) return;

            args.Cancel = true;
            // 委托系统浏览器访问
            await Windows.System.Launcher.LaunchUriAsync(args.Uri);
        }

        /// <summary>
        /// lock
        /// TODO refresh data be ignore
        /// </summary>
        private string CurrentMailMessageId = "";

        private async void AttachmentsListBox_OnDataContextChanged(FrameworkElement sender,
            DataContextChangedEventArgs args)
        {
            if (args.NewValue is not MailMessageListDetailViewModel Model) return;
            if (CurrentMailMessageId.Equals(Model.Id)) return;

            CurrentMailMessageId = Model.Id;
            Trace.WriteLine($"DataContextChanged: {Model.Title}");
            if (sender is not ListBox ListBox) return;

            IMailService Service = App.Services.GetService<OutlookService>()!;
            // TODO abstract result support other mail
            var MessageAttachmentsCollectionPage = Service.GetMailAttachmentFileAsync(Model);

            var ListBoxItems = ListBox.Items!;
            ListBoxItems.Clear();
            await foreach (var Attachment in MessageAttachmentsCollectionPage)
            {
                // TODO Style
                var ListBoxItem = new Button()
                {
                    Content = $"{Attachment.Name}\r\nSize: {Attachment.AttachmentSize} Byte",
                    DataContext = Attachment
                };
                ListBoxItem.Click += MailFileAttachmentDownload;
                ListBoxItems.Add(ListBoxItem);
            }
        }


        private async void MailFileAttachmentDownload(object sender, RoutedEventArgs e)
        {
            // TODO FileAttachment not abstract
            if ((sender as FrameworkElement)?
                .DataContext is not MailMessageFileAttachmentData Attachment) return;
            var FolderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
            };
            var StorageFolder = await FolderPicker.PickSingleFolderAsync();
            if (StorageFolder == null) return;

            // TODO tips file be overwritten need user confirm 
            var StorageFile = await StorageFolder.CreateFileAsync(Attachment.Name,
                CreationCollisionOption.ReplaceExisting);
            using var Result = await StorageFile.OpenStreamForWriteAsync();

            await Result.WriteAsync(Attachment.ContentBytes, 0, Attachment.ContentBytes.Length);
            await Result.FlushAsync();
        }
    }
}