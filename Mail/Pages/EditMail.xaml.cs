using System;
using System.Threading.Tasks;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;
using Mail.Models;
using Mail.Services;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.DependencyInjection;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Mail.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EditMail
    {
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
            // TODO abstract MailService
            var service = App.Services.GetService<OutlookService>()!;
            MailSender = new MailMessageRecipientData(string.Empty,
                service.CurrentAccount?.Address ?? string.Empty);
            Model = new MailMessageListDetailViewModel(MailMessageData.Empty(MailSender));

            Model.ToRecipients.Add(To);
            EditMailOption = new EditMailOption { Model = Model, EditMailType = EditMailType.Send };

            InitBaseData(EditMailOption);
            InitializeComponent();
        }

        private void InitBaseData(EditMailOption MailOption)
        {
            Model = MailOption.Model ?? Model;

            switch (MailOption.EditMailType)
            {
                case EditMailType.Forward:
                    Model.ToRecipients.Clear();
                    break;
                case EditMailType.Reply:
                case EditMailType.Send:
                case EditMailType.Draft:
                default:
                    break;
            }

            Model.ToRecipients.Add(To);
            MailSender = Model.Sender;
        }

        private void SaveDraft(object Sender, RoutedEventArgs E)
        {
            EditMailOption.EditMailType = EditMailType.Draft;
            SaveMailAndSend(MailSender, E);
        }

        private async void SaveMailAndSend(object Sender, RoutedEventArgs E)
        {
            var service = App.Services.GetService<OutlookService>()!;

            var result = EditMailOption.EditMailType switch
            {
                EditMailType.Reply => await service.MailReplyAsync(Model, ReplyOrForwardContent,
                    EditMailOption.IsReplyAll),
                EditMailType.Forward => await service.MailForwardAsync(Model, ReplyOrForwardContent),
                EditMailType.Send => await service.MailSendAsync(Model),
                _ => await service.MailDraftSaveAsync(Model)
            };

            if (result)
            {
            }
            else
            {
                // TODO tips fail
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
    }
}