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
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
#nullable enable


namespace Mail.Pages
{
    public sealed partial class MailFolderDetailsPage : Page
    {
        private IMailService? CurrentService;
        private MailFolderData? NavigationData;
        private MailIncrementalLoadingObservableCollection? PreviewSource;
        private CancellationTokenSource? SelectionChangeCancellation;
        private readonly AsyncLock SelectionChangeLocker = new AsyncLock();

        public MailFolderDetailsPage()
        {
            this.InitializeComponent();
        }


        private static int count = 0;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            CurrentService = App.Services.GetService<OutlookService>()!;

            if (e.Parameter is MailFolderData Data)
            {
                this.NavigationData = Data;
                this.FolderName.Text = Data.Name;
                this.NavigationTab.SelectedItem = this.FocusedTab;
            }
        }

        private async Task InitializeDataFromMailFolderAsync(MailFolderData MailFolder)
        {
            this.EmptyContentText.Text = @"Syncing you email";

            try
            {
                DetailsView.ItemsSource = PreviewSource = new MailIncrementalLoadingObservableCollection((Instance, RequestCount, Token) =>
                {
                    LoadMailMessageOption Options = new LoadMailMessageOption
                    {
                        FolderId = MailFolder.Id,
                        StartIndex = Instance.Count,
                        LoadCount = (int)Math.Max(Instance.MinIncrementalLoadingStep, RequestCount),
                        IsFocusedTab = this.FocusedTab.Equals(NavigationTab.SelectedItem as NavigationViewItem) && MailFolder.Type == MailFolderType.Inbox
                    };

                    if (Options.IsFocusedTab && CurrentService is IMailService.IFocusFilterSupport filterSupport)
                    {
                        return filterSupport.GetMailMessageAsync(Options, CancelToken: Token);
                    }
                    else
                    {
                        return this.CurrentService!.GetMailMessageAsync(Options, CancelToken: Token);
                    }

                }, Convert.ToUInt32(MailFolder.TotalItemCount));

                await PreviewSource.LoadMoreItemsAsync(30);

                if (PreviewSource.Count == 0)
                {
                    this.EmptyContentText.Text = @"No available email";
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e);
#endif
                this.EmptyContentText.Text = @"Sync failed";
            }
        }

        private string ConvertContentTheme(string original, bool rawText = false)
        {
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                if (rawText)
                {
                    return original;
                }
                else
                {
                    const string darkCss =
                        "<style>body{color: white;background: transparent !important; background-color: transparent !important;}</style>";
                    const string regexPattern =
                        """(bgcolor|background|color|background-color)\s*(\:|\=\")\s*(\S*)([\;\"])""";

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
                                originalColor = Color.FromArgb((byte)(double.Parse(rgbaArr[3]) * 255),
                                    Byte.Parse(rgbaArr[0]),
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
                original = Regex.Replace(original, @"border(-left|-right|-top|-bottom)*:(\S*)\s*windowtext",
                    "border$1:$2 white");
            }

            return original;
        }

        private async Task LoadImageAndCacheAsync(MailMessageListDetailViewModel Model, WebView Browser, CancellationToken CancelToken = default)
        {
            IReadOnlyList<MailMessageFileAttachmentData> AttachmentFileList = await CurrentService.GetMailAttachmentFileAsync(Model, CancelToken).ToArrayAsync(CancelToken);

            if (AttachmentFileList.Count > 0)
            {
                string ReplaceHtmlInnerImageCidToBase64(string CidPlaceHolder, IEnumerable<MailMessageFileAttachmentData> MessageAttachmentFileList)
                {
                    if (MessageAttachmentFileList.FirstOrDefault((Attachment) => Attachment.Id.Equals(CidPlaceHolder.Replace("cid:", ""))) is MailMessageFileAttachmentData TargetAttachment)
                    {
                        return $"data:{TargetAttachment.ContentType};base64,{Convert.ToBase64String(TargetAttachment.ContentBytes)}";
                    }

                    return CidPlaceHolder;
                }

                Browser.NavigateToString(ConvertContentTheme(new Regex("cid:[^\"]+").Replace(Model.Content, (Match) => ReplaceHtmlInnerImageCidToBase64(Match.Value, AttachmentFileList)), Model.ContentType == MailMessageContentType.Text));
            }
            else
            {
                Browser.NavigateToString(ConvertContentTheme(Model.Content, Model.ContentType == MailMessageContentType.Text));
            }
        }

        private async void Browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            string HeightRaw = await sender.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });

            if (int.TryParse(HeightRaw, out int height))
            {
                sender.Height = height + 50;
            }
        }

        private void ListDetailsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListDetailsView view)
            {
                if (view.FindChildOfType<ListView>() is ListView innerView)
                {
                    innerView.IncrementalLoadingTrigger = IncrementalLoadingTrigger.Edge;
                    innerView.IncrementalLoadingThreshold = 3;
                    innerView.DataFetchSize = 3;
                }
            }
        }

        private async void DetailsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.SingleOrDefault() is MailMessageListDetailViewModel Model)
            {
                if (sender is ListDetailsView view && !Model.IsEmpty)
                {
                    SelectionChangeCancellation?.Cancel();
                    SelectionChangeCancellation?.Dispose();
                    SelectionChangeCancellation = new CancellationTokenSource();

                    try
                    {
                        using (await SelectionChangeLocker.LockAsync(SelectionChangeCancellation.Token))
                        {
                            for (int Retry = 0; Retry < 10; Retry++)
                            {
                                if (view.FindChildOfType<WebView>() is WebView Browser
                                    && view.FindChildOfName<ListView>("AttachmentsListView") is ListView AttachmentView
                                    && view.FindChildOfName<StackPanel>("AttachmentsArea") is StackPanel AttachmentPanel)
                                {
                                    AttachmentPanel.Visibility = Visibility.Collapsed;

                                    try
                                    {
                                        Browser.NavigateToString(string.Empty);
                                        await LoadImageAndCacheAsync(Model, Browser, SelectionChangeCancellation.Token);
                                        await LoadAttachmentsList(AttachmentView, Model, SelectionChangeCancellation.Token);
                                    }
                                    finally
                                    {
                                        if (AttachmentView.Items.Count > 0)
                                        {
                                            AttachmentPanel.Visibility = Visibility.Visible;
                                        }
                                    }

                                    break;
                                }

                                await Task.Delay(300);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // No need to handle this exception
                    }
                }
            }
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
            if (sender is ListDetailsView View)
            {
                if (View.FindChildOfName<FrameworkElement>("DetailsViewGoBack") is FrameworkElement GoBackButton)
                {
                    GoBackButton.Visibility = View.ViewState == ListDetailsViewState.Details ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            await InitializeDataFromMailFolderAsync(this.NavigationData);
        }

        private async void Browser_OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            string TargetUrlPath = args.Uri?.AbsoluteUri ?? string.Empty;

            if (!string.IsNullOrEmpty(TargetUrlPath))
            {
                args.Cancel = true;

                if (!await Launcher.LaunchUriAsync(new Uri(TargetUrlPath)))
                {
                    //Handle system browser activation failure here
                }
            }
        }

        /// <summary>
        /// 加载附件列表
        /// </summary>
        private async Task LoadAttachmentsList(ItemsControl Sender, MailMessageListDetailViewModel Model, CancellationToken CancelToken = default)
        {
            Sender.Items.Clear();

            await foreach (MailMessageFileAttachmentData attachment in CurrentService!.GetMailAttachmentFileAsync(Model, CancelToken))
            {
                Sender.Items.Add(attachment);
            }
        }

        private async void MailFileAttachmentDownload(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is MailMessageFileAttachmentData Attachment)
            {
                FolderPicker Picker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                };

                if (await Picker.PickSingleFolderAsync() is StorageFolder Folder)
                {
                    // TODO tips file be overwritten need user confirm
                    StorageFile SaveStorageFile = await Folder.CreateFileAsync(Attachment.Name, CreationCollisionOption.ReplaceExisting);

                    using (Stream FileStream = await SaveStorageFile.OpenStreamForWriteAsync())
                    {
                        await FileStream.WriteAsync(Attachment.ContentBytes, 0, Attachment.ContentBytes.Length);
                        await FileStream.FlushAsync();
                    }
                }
            }
        }

        private void CreateMail_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewSource.FirstOrDefault() is not { IsEmpty: true })
            {
                MailMessageListDetailViewModel NewEmptyModel = MailMessageListDetailViewModel.Empty(new MailMessageRecipientData(string.Empty, string.Empty));
                PreviewSource.Insert(0, NewEmptyModel);
                DetailsView.SelectedItem = NewEmptyModel;

                //EditMail.CreateEditWindow(new EditMailOption {Model = model,EditMailType = EditMailType.Send });
            }
        }

        private async void MailForwardAsync(object Sender, RoutedEventArgs E)
        {
            if ((Sender as FrameworkElement)?.DataContext is MailMessageListDetailViewModel Model)
            {
                await EditMail.CreateEditWindow(new EditMailOption
                {
                    Model = Model,
                    EditMailType = EditMailType.Forward
                });
            }
        }

        private void FrameworkElement_OnLoading(FrameworkElement Sender, object Args)
        {
            if (Sender is Frame frame)
            {
                if (frame.DataContext is MailMessageListDetailViewModel Model && !Model.IsEmpty)
                {
                    frame.Navigate(typeof(EditMail), new EditMailOption
                    {
                        Model = Model,
                        EditMailType = EditMailType.Send
                    });
                }
            }
        }

        private async void MailReplyAsync(object Sender, RoutedEventArgs E)
        {
            if ((Sender as FrameworkElement)?.DataContext is MailMessageListDetailViewModel Model)
            {
                await EditMail.CreateEditWindow(new EditMailOption
                {
                    Model = Model,
                    EditMailType = EditMailType.Reply
                });
            }
        }

        private async void MailMoveAsync(object Sender, RoutedEventArgs E)
        {
            if ((Sender as FrameworkElement)?.DataContext is MailMessageListDetailViewModel Model)
            {
                //TODO 目标文件夹id来源需要前台处理
                await this.CurrentService!.MailMoveAsync(Model.Id, @"sQACTCKK1QAAAA==");
            }
        }

        private async void MailRemoveAsync(object Sender, RoutedEventArgs E)
        {
            // TODO refresh folder
            if ((Sender as FrameworkElement)?.DataContext is MailMessageListDetailViewModel Model)
            {
                if (!await this.CurrentService!.MailRemoveAsync(Model))
                {
                    Trace.WriteLine($"无法删除邮件: {Model.Id}");
                }
            }
        }

        private async void MailMoveArchiveAsync(object Sender, RoutedEventArgs E)
        {
            if ((Sender as FrameworkElement)?.DataContext is MailMessageListDetailViewModel Model)
            {
                var folder = this.CurrentService!.GetCache().Get<MailFolderData>("archive");
                if (folder == null)
                {
                    //TODO Outlook是加入了缓存, 其他服务需要处理
                    return;
                }

                await CurrentService.MailMoveAsync(Model.Id, folder.Id);
            }
        }
    }
}