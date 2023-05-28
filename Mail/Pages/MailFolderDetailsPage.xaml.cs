using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Mail.Services.Collection;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
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
            MailFolderData data = NavigationData;
            if (data == null) return;
            try
            {
                IMailService Service = App.Services.GetService<OutlookService>()!;
                MailFolderDetailData MailFolder = await Service.GetMailFolderDetailAsync(data.Id);
                
                // Load Contacts to Cache
                var contacts = await Service.GetContactsAsync();
                App.Services.GetService<ICacheService>()!.AddOrReplaceCache<IReadOnlyList<ContactModel>>(contacts);
                
                DetailsView.ItemsSource = PreviewSource =
                    new MailIncrementalLoadingObservableCollection<MailMessageListDetailViewModel>(Service, data.Type,
                        MailFolder, (messageData) => new MailMessageListDetailViewModel(messageData),
                        IsFocusTab: IsFocusedTab);

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
            if (Model.IsEmpty) return;
            if (sender is not ListDetailsView View) return;

            using (await SelectionChangeLocker.LockAsync())
            {
                for (var Retry = 0; Retry < 10; Retry++)
                {
                    if (View.FindChildOfType<WebView>() is WebView Browser)
                    {
                        LoadImageAndCacheAsync(Model, Browser);
                        LoadAttachmentsList(View.FindChildOfName<ListBox>("AttachmentsListBox"), Model);
                        Browser.Height = 100;

                        var Replace = Model.Content;

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

        private async Task LoadImageAndCacheAsync(MailMessageListDetailViewModel model, WebView browser)
        {
            IMailService Service = App.Services.GetService<OutlookService>()!;
            if (Service.GetCache().Get(model.Id) is null)
            {
                await Service.LoadAttachmentsAndCacheAsync(model.Id);
            }

            if (browser.DataContext is not MailMessageListDetailViewModel Context) return;
            if (!Context.Id.Equals(model.Id)) return;

            var Replace = Rgx.Replace(model.Content, ReplaceWord);

            if (model.ContentType == MailMessageContentType.Text)
            {
                Replace =
                    @$"<html><head><style type=""text/css"">body{{color: #000; background-color: transparent;}}</style></head><body>{Replace}</body></html>";
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => browser.NavigateToString(Replace));
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
                View.FindChildOfName<Button>("DetailsViewGoBack") is Button detailsViewGoBack)
            {
                detailsViewGoBack.Visibility = DetailsView.ViewState == ListDetailsViewState.Details
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
        private string CurrentMailAttachmentsListBoxMessageId = "";

        /// <summary>
        /// 加载附件列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="model"></param>
        private async Task LoadAttachmentsList(ItemsControl sender, MailMessageListDetailViewModel model)
        {
            if (CurrentMailAttachmentsListBoxMessageId.Equals(model.Id)) return;
            if (model.IsEmpty) return;
            CurrentMailAttachmentsListBoxMessageId = model.Id;
            IMailService service = App.Services.GetService<OutlookService>()!;
            var messageAttachmentsCollectionPage = service.GetMailAttachmentFileAsync(model);
            var listBoxItems = sender.Items!;
            listBoxItems.Clear();
            await foreach (var attachment in messageAttachmentsCollectionPage)
            {
                // TODO Style
                var ListBoxItem = new Button
                {
                    Content = $"{attachment.Name}\r\nSize: {attachment.AttachmentSize} Byte",
                    DataContext = attachment
                };
                ListBoxItem.Click += MailFileAttachmentDownload;
                listBoxItems.Add(ListBoxItem);
            }
        }

        private async void MailFileAttachmentDownload(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?
                .DataContext is not MailMessageFileAttachmentData attachment) return;
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
            };
            var storageFolder = await folderPicker.PickSingleFolderAsync();
            if (storageFolder == null) return;

            // TODO tips file be overwritten need user confirm
            var storageFile = await storageFolder.CreateFileAsync(attachment.Name,
                CreationCollisionOption.ReplaceExisting);
            using var result = await storageFile.OpenStreamForWriteAsync();

            await result.WriteAsync(attachment.ContentBytes, 0, attachment.ContentBytes.Length);
            await result.FlushAsync();
        }

        private void CreateMail_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewSource is null) return;

            var newItem = PreviewSource.FirstOrDefault();
            if (newItem is not { IsEmpty: true })
            {
                var model = MailMessageListDetailViewModel.Empty(
                    new MailMessageRecipientData(string.Empty, string.Empty));
                PreviewSource.Insert(0,
                    model);
                //EditMail.CreateEditWindow(new EditMailOption {Model = model,EditMailType = EditMailType.Send });
            }

            DetailsView.SelectedIndex = 0;
        }

        private void SendMail_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?
                .DataContext is not MailMessageListDetailViewModel { IsEmpty: true } Model) return;

            var info = Model.EditInfo;

            //TODO combine information and send email
        }

        private async void MailForwardAsync(object Sender, RoutedEventArgs E)
        {
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;

            await EditMail.CreateEditWindow(new EditMailOption
            {
                Model = model, EditMailType = EditMailType.Forward
            });
        }

        private void Frame_OnNavigating(object Sender, NavigatingCancelEventArgs E)
        {
            if (Sender is not Frame frame) return;
            var model = PreviewSource?.FirstOrDefault();
            if (model?.IsEmpty != true) return;

            frame.Navigate(typeof(EditMail), new EditMailOption
            {
                Model = model, EditMailType = EditMailType.Send
            });
        }

        private void FrameworkElement_OnLoading(FrameworkElement Sender, object Args)
        {
            if (Sender is not Frame frame) return;
            var model = frame.DataContext as MailMessageListDetailViewModel;
            if (model?.IsEmpty != true) return;
            frame.Navigate(typeof(EditMail), new EditMailOption
            {
                Model = model, EditMailType = EditMailType.Send
            });
        }
    }

    public class DetailsSelector : DataTemplateSelector
    {
        public DataTemplate EditTemplate { get; set; }

        public DataTemplate DefaultTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is MailMessageListDetailViewModel { IsEmpty: true })
            {
                return EditTemplate;
            }

            return DefaultTemplate;
        }
    }
}