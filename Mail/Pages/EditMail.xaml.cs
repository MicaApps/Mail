using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;
using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nito.AsyncEx;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Mail.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EditMail
    {
        private readonly IMailService? Service;
        private MailMessageListDetailViewModel Model { get; set; }

        /// <summary>
        /// TODO edit address UI not changed
        /// </summary>
        private MailMessageRecipientData MailSender { get; set; }

        /// <summary>
        /// TODO multiple recipient
        /// </summary>
        private MailMessageRecipientData To { get; set; } = new(string.Empty, string.Empty);

        private EditMailOption EditMailOption { get; set; }
        private string ReplyOrForwardContent { get; set; } = string.Empty;

        public EditMail()
        {
            Service = App.Services.GetService<OutlookService>()!;

            MailSender = new MailMessageRecipientData(string.Empty,
                Service?.CurrentAccount?.Address ?? string.Empty);
            Model = new MailMessageListDetailViewModel(MailMessageData.Empty(MailSender));

            Model.ToRecipients.Add(To);
            EditMailOption = new EditMailOption { Model = Model, EditMailType = EditMailType.Send };

            // init 由导航执行
            // InitBaseData(EditMailOption);
            InitializeComponent();
        }

        private void InitBaseData(EditMailOption MailOption)
        {
            EditMailOption = MailOption;
            Model = MailOption.Model ?? Model;

            void CopyContent()
            {
                ReplyOrForwardContent = Model.Content;
                Model.Content = string.Empty;
            }

            switch (MailOption.EditMailType)
            {
                case EditMailType.Forward:
                    Model.ToRecipients.Clear();
                    Model.Sender.Address = MailSender.Address;
                    CopyContent();
                    break;
                case EditMailType.Send:
                    break;
                case EditMailType.Reply:
                    CopyContent();
                    break;
                case EditMailType.Draft:
                default:
                    break;
            }

            var to = Model.ToRecipients.FirstOrDefault();
            if (to is null)
            {
                Model.ToRecipients.Add(To);
            }
            else
            {
                To = to;
            }

            if (Model.Sender.Address.IsNullOrEmpty())
            {
                Model.Sender.Address = Service!.CurrentAccount!.Address;
            }

            MailSender = Model.Sender;

            // save draft
            if (Model.Id.IsNullOrEmpty())
            {
                Service?.MailDraftSaveAsync(Model);
            }
        }

        private static readonly SemaphoreSlim SaveMailLock = new(1);

        private async void SaveDraft(object Sender, RoutedEventArgs E)
        {
            await SaveMailLock.LockAsync();
            try
            {
                await Service!.MailDraftSaveAsync(Model);
            }
            finally
            {
                SaveMailLock.Release();
            }
        }

        private async void SaveMailAndSend(object Sender, RoutedEventArgs E)
        {
            switch (EditMailOption.EditMailType)
            {
                case EditMailType.Reply:
                case EditMailType.Forward:
                    (Model.Content, ReplyOrForwardContent) = (ReplyOrForwardContent, Model.Content);
                    break;
                case EditMailType.Send:
                case EditMailType.Draft:
                default:
                    break;
            }

            await SaveMailLock.LockAsync();
            try
            {
                var result = EditMailOption.EditMailType switch
                {
                    EditMailType.Reply => await Service!.MailReplyAsync(Model, ReplyOrForwardContent,
                        EditMailOption.IsReplyAll),
                    EditMailType.Forward => await Service!.MailForwardAsync(Model, ReplyOrForwardContent),
                    EditMailType.Send => await Service!.MailSendAsync(Model),
                    _ => await Service!.MailDraftSaveAsync(Model)
                };

                if (result)
                {
                    // TODO close frame
                }
                else
                {
                    // TODO tips fail
                }
            }
            catch (Exception e)
            {
                // TODO Exception tips
                Trace.WriteLine(e);
                throw;
            }
            finally
            {
                SaveMailLock.Release();
            }
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is EditMailOption option)
            {
                InitBaseData(option);
            }

            base.OnNavigatedTo(e);
        }

        public static async Task CreateEditWindow(EditMailOption? Option = null)
        {
            var appWindow = await AppWindow.TryCreateAsync();
            if (appWindow is null)
            {
                return;
            }

            var appWindowContentFrame = new Frame();
            appWindowContentFrame.Navigate(typeof(EditMail), Option);
            ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);
            await appWindow.TryShowAsync();
        }

        private async void UploadAttachment(object Sender, RoutedEventArgs E)
        {
            var openPicker = new FileOpenPicker
            {
                FileTypeFilter = { "*" },
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            try
            {
                if (this.FindChildOfName<ListBox>("AttachmentListBox") is not { } listBox)
                {
                    return;
                }

                var listItems = listBox.Items;
                listItems?.Clear();
                foreach (var storageFile in await openPicker.PickMultipleFilesAsync())
                {
                    var basicProperties = await storageFile.GetBasicPropertiesAsync();
                    if (basicProperties.Size < 3 * 1024 * 1024)
                    {
                        var attachment = await AttachmentUploadAsync(storageFile);
                        // TODO render attachment list
                        listItems?.Add(new ListBoxItem
                        {
                            Name = attachment?.Name
                        });
                    }
                    else
                    {
                        await AttachmentUploadSessionAsync(basicProperties, storageFile,
                            currenOffset =>
                            {
                                Trace.WriteLine($"Uploaded {currenOffset} bytes of {basicProperties.Size} bytes");
                            });
                    }
                }
            }
            catch (Exception e)
            {
                // TODO upload fail tips
                Trace.WriteLine(e);
            }
        }

        /// <summary>
        /// 大文件上传处理
        /// </summary>
        /// <param name="BasicProperties"></param>
        /// <param name="StorageFile"></param>
        /// <param name="UploadedSliceCallback">每上传完一次数据包会执行一次回调</param>
        private async Task AttachmentUploadSessionAsync(BasicProperties BasicProperties, StorageFile StorageFile,
            Action<long> UploadedSliceCallback)
        {
            await Service!.UploadAttachmentSessionAsync(Model, BasicProperties, StorageFile, UploadedSliceCallback);
        }

        private async Task<MailMessageFileAttachmentData?> AttachmentUploadAsync(StorageFile StorageFile)
        {
            return await Service!.UploadAttachmentAsync(Model, StorageFile);
        }

        private async void RemoveMail(object Sender, RoutedEventArgs E)
        {
            // TODO remove Folder MailList
            await Service!.RemoveMailAsync(Model);
        }

        private void TextBox_OnTextCompositionEnded(TextBox Sender, TextCompositionEndedEventArgs Args)
        {
            Service?.MailDraftSaveAsync(Model);
        }
    }
}