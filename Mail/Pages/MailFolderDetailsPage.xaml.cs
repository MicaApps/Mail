using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Mail.Services.Collection;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Mail.Pages
{
    public sealed partial class MailFolderDetailsPage : Page
    {
        private readonly AsyncLock SelectionChangeLocker = new();
        private MailIncrementalLoadingObservableCollection? PreviewSource;
        private MailFolderData? NavigationData;
        private bool IsFocusedTab { get; set; } = true;
        private IMailService Service;

        public MailFolderDetailsPage()
        {
            Service = App.Services.GetService<OutlookService>()!;
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Service = App.Services.GetService<OutlookService>()!;

            if (e is { Parameter: MailFolderData data, NavigationMode: NavigationMode.New })
            {
                NavigationData = data;
                FolderName.Text = data.Name;
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
                IMailService service = App.Services.GetService<OutlookService>()!;
                var mailFolder = await service.GetMailFolderDetailAsync(data.Id);

                // Load Contacts to Cache
                /*
                 var contacts = await service.GetContactsAsync();
                 App.Services.GetService<ICacheService>()!.AddOrReplaceCache(contacts);
                */

                var isFocusedTab = IsFocusedTab && data.Type == MailFolderType.Inbox;
                var option = new LoadMailMessageOption(data.Id, isFocusedTab);

                DetailsView.ItemsSource = PreviewSource =
                    new MailIncrementalLoadingObservableCollection(service,
                        mailFolder,
                        IsFocusTab: isFocusedTab);

                IAsyncEnumerable<MailMessageData> dataSet;
                if (data.Type == MailFolderType.Inbox && service is IMailService.IFocusFilterSupport filterService)
                {
                    dataSet = filterService.GetMailMessageAsync(option);
                }
                else
                {
                    dataSet = service.GetMailMessageAsync(option);
                }

                await foreach (var messageData in dataSet)
                {
                    PreviewSource.Add(new MailMessageListDetailViewModel(messageData));
                }

                if (PreviewSource.Count == 0)
                {
                    EmptyContentText.Text = "No available email";
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e);
#endif
                EmptyContentText.Text = "Sync failed";
            }
        }

        private string ConvertContentTheme(string original, bool rawText = false)
        {
            if (Application.Current.RequestedTheme != ApplicationTheme.Dark) return original;

            if (rawText)
            {
                return original;
            }
            else
            {
                const string darkCss =
                    "<style>body{color: white;background: transparent !important; background-color: transparent !important;}</style>";
                const string regexPattern = """(bgcolor|background|color|background-color)\s*(\:|\=\")\s*(\S*)([\;\"])""";

                var matches = Regex.Matches(original, regexPattern);
                foreach (Match match in matches)
                {
                    try
                    {
                        Color originalColor;
                        if (match.Groups[3].Value.StartsWith("rgba"))
                        {
                            var rgbaStr = match.Groups[3].Value.Substring(5).TrimEnd(')');
                            var rgbaArr = rgbaStr.Split(',');
                            originalColor = Color.FromArgb((byte)(double.Parse(rgbaArr[3]) * 255), Byte.Parse(rgbaArr[0]),
                                                           Byte.Parse(rgbaArr[1]), Byte.Parse(rgbaArr[2]));
                        }
                        else if (match.Groups[3].Value.StartsWith("rgb"))
                        {
                            var rgbStr = match.Groups[3].Value.Substring(4).TrimEnd(')');
                            var rgbArr = rgbStr.Split(',');
                            originalColor = Color.FromArgb(255, Byte.Parse(rgbArr[0]),
                                                           Byte.Parse(rgbArr[1]), Byte.Parse(rgbArr[2]));
                        }
                        else
                        {
                            originalColor = ColorExtensions.ParseColor(match.Groups[3].Value, null);
                        }


                        var hslColor = originalColor.ToHsl();

                        // 合理调整此处区间以便正常拉取色调
                        if (hslColor.L >= 0.70)
                        {
                            hslColor.L = 1 - hslColor.L;
                        }
                        else if (hslColor.L <= 0.30)
                        {
                            hslColor.L = 1 - hslColor.L;
                        }

                        var color = Microsoft.Toolkit.Uwp.Helpers.ColorHelper.FromHsl(
                            hslColor.H, hslColor.S, hslColor.L, hslColor.A);

                        if (match.Groups[1].Value.StartsWith('b') && hslColor.L is > 0.90 or < 0.10)
                        {


                            original = original.Replace(match.Value,
                                                    match.Groups[1].Value + match.Groups[2].Value + "transparent" +
                                                    match.Groups[4].Value);
                            continue;

                        }

                        original = original.Replace(match.Value,
                                                        match.Groups[1].Value + match.Groups[2].Value + $"#{color.R:X2}{color.G:X2}{color.B:X2}" +
                                                        match.Groups[4].Value);
                    }
                    catch
                    {
                        //ignore
                    }
                }


                if (original.Contains("<body>"))
                {
                    original = original.Insert(original.IndexOf("<body>", StringComparison.Ordinal) + 6, darkCss);
                }

                if (original.Contains("<html>"))
                {
                    original = original.Insert(original.IndexOf("<html>", StringComparison.Ordinal) + 6, darkCss);
                }
            }

            // 处理表格
            original = Regex.Replace(original, @"border(-left|-right|-top|-bottom)*:(\S*)\s*windowtext", "border$1:$2 white");

            return original;
        }

        private async void ListDetailsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is MailMessageListDetailViewModel model)
            {
                if (sender is ListDetailsView view && !model.IsEmpty)
                {
                    using (await SelectionChangeLocker.LockAsync())
                    {
                        for (var retry = 0; retry < 10; retry++)
                        {
                            if (view.FindChildOfType<WebView>() is WebView browser
                                && view.FindChildOfName<ListView>("AttachmentsListView") is ListView attachmentView
                                && view.FindChildOfName<StackPanel>("AttachmentsArea") is StackPanel attachmentPanel)
                            {
                                attachmentPanel.Visibility = Visibility.Collapsed;

                                try
                                {
                                    browser.NavigateToString(string.Empty);
                                    await LoadImageAndCacheAsync(model, browser);
                                    await LoadAttachmentsList(attachmentView, model);
                                }
                                finally
                                {
                                    if (attachmentView.Items.Count > 0)
                                    {
                                        attachmentPanel.Visibility = Visibility.Visible;
                                    }
                                }

                                break;
                            }

                            await Task.Delay(300);
                        }
                    }
                }
            }
        }

        private readonly Regex Rgx = new("cid:[^\"]+");

        private async Task LoadImageAndCacheAsync(MailMessageListDetailViewModel model, WebView browser)
        {
            if (Service.GetCache().Get(model.Id) is null)
            {
                await Service.LoadAttachmentsAndCacheAsync(model.Id);
            }

            if (browser.DataContext is MailMessageListDetailViewModel context)
            {
                if (context.Id.Equals(model.Id))
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => browser.NavigateToString(ConvertContentTheme(Rgx.Replace(model.Content, ReplaceWord), model.ContentType == MailMessageContentType.Text)));
                }
            }
        }

        private string ReplaceWord(Capture match)
        {
            var fileAttachment = Service.GetCache().Get<MailMessageFileAttachmentData>(match.Value.Replace("cid:", ""));
            return fileAttachment is null
                ? match.Value
                : $"data:{fileAttachment.ContentType};base64,{Convert.ToBase64String(fileAttachment.ContentBytes)}";
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
            if (sender is not ListDetailsView view) return;
            if (view.FindChildOfType<ListView>() is not { } innerView) return;

            innerView.IncrementalLoadingTrigger = IncrementalLoadingTrigger.Edge;
            innerView.IncrementalLoadingThreshold = 3;
            innerView.DataFetchSize = 3;
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
            if (sender is ListDetailsView view &&
                view.FindChildOfName<Button>("DetailsViewGoBack") is { } detailsViewGoBack)
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

            sender.Items.Clear();

            await foreach (MailMessageFileAttachmentData attachment in App.Services.GetService<OutlookService>().GetMailAttachmentFileAsync(model))
            {
                sender.Items.Add(attachment);
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

        private async void MailForwardAsync(object Sender, RoutedEventArgs E)
        {
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;

            await EditMail.CreateEditWindow(new EditMailOption
            {
                Model = model,
                EditMailType = EditMailType.Forward
            });
        }

        private void FrameworkElement_OnLoading(FrameworkElement Sender, object Args)
        {
            if (Sender is not Frame frame) return;
            var model = frame.DataContext as MailMessageListDetailViewModel;
            if (model?.IsEmpty != true) return;
            frame.Navigate(typeof(EditMail), new EditMailOption
            {
                Model = model,
                EditMailType = EditMailType.Send
            });
        }

        private async void MailReplyAsync(object Sender, RoutedEventArgs E)
        {
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;

            await EditMail.CreateEditWindow(new EditMailOption
            {
                Model = model,
                EditMailType = EditMailType.Reply
            });
        }

        private async void MailMoveAsync(object Sender, RoutedEventArgs E)
        {
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;
            //TODO 目标文件夹id来源需要前台处理
            await Service.MailMoveAsync(model.Id, "sQACTCKK1QAAAA==");
        }

        private async void MailRemoveAsync(object Sender, RoutedEventArgs E)
        {
            // TODO refresh folder
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;
            var mailRemoveAsync = await Service.MailRemoveAsync(model);
            Trace.WriteLine($"删除邮件: {mailRemoveAsync}");
        }

        private async void MailMoveArchiveAsync(object Sender, RoutedEventArgs E)
        {
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;

            var folder = Service.GetCache().Get<MailFolderData>("archive");
            if (folder == null)
            {
                //TODO Outlook是加入了缓存, 其他服务需要处理
                return;
            }

            await Service.MailMoveAsync(model.Id, folder.Id);
        }
    }

    public class DetailsSelector : DataTemplateSelector
    {
        public DataTemplate EditTemplate { get; set; } = default!;

        public DataTemplate DefaultTemplate { get; set; } = default!;

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item is MailMessageListDetailViewModel { IsEmpty: true } ? EditTemplate : DefaultTemplate;
        }
    }
}