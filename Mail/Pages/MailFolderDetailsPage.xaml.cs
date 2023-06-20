﻿using System;
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
                var contacts = await service.GetContactsAsync();
                App.Services.GetService<ICacheService>()!.AddOrReplaceCache(contacts);

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

        private async void ListDetailsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is not MailMessageListDetailViewModel model) return;
            if (model.IsEmpty) return;
            if (sender is not ListDetailsView view) return;

            using (await SelectionChangeLocker.LockAsync())
            {
                for (var retry = 0; retry < 10; retry++)
                {
                    if (view.FindChildOfType<WebView>() is { } browser)
                    {
                        LoadImageAndCacheAsync(model, browser);
                        LoadAttachmentsList(view.FindChildOfName<ListBox>("AttachmentsListBox"), model);
                        browser.Height = 100;

                        var replace = model.Content;

                        if (model.ContentType == MailMessageContentType.Text)
                        {
                            replace =
                                @$"<html><head><style type=""text/css"">body{{color: #000; background-color: transparent;}}</style></head><body>{replace}</body></html>";
                        }

                        browser.NavigateToString(replace);
                        break;
                    }

                    await Task.Delay(300);
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

            if (browser.DataContext is not MailMessageListDetailViewModel context) return;
            if (!context.Id.Equals(model.Id)) return;

            var replace = Rgx.Replace(model.Content, ReplaceWord);

            if (model.ContentType == MailMessageContentType.Text)
            {
                replace =
                    @$"<html><head><style type=""text/css"">body{{color: #000; background-color: transparent;}}</style></head><body>{replace}</body></html>";
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => browser.NavigateToString(replace));
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
            IMailService service = App.Services.GetService<OutlookService>()!;
            var messageAttachmentsCollectionPage = service.GetMailAttachmentFileAsync(model);
            var listBoxItems = sender.Items!;
            listBoxItems.Clear();
            await foreach (var attachment in messageAttachmentsCollectionPage)
            {
                // TODO Style
                var listBoxItem = new Button
                {
                    Content = $"{attachment.Name}\r\nSize: {attachment.AttachmentSize} Byte",
                    DataContext = attachment
                };
                listBoxItem.Click += MailFileAttachmentDownload;
                listBoxItems.Add(listBoxItem);
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
                Model = model, EditMailType = EditMailType.Forward
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

        private async void MailReplyAsync(object Sender, RoutedEventArgs E)
        {
            if (Sender is not MenuFlyoutItem { DataContext: MailMessageListDetailViewModel model }) return;

            await EditMail.CreateEditWindow(new EditMailOption
            {
                Model = model, EditMailType = EditMailType.Reply
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