using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;
using Mail.Models;
using Mail.Services;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

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

        public EditMail()
        {
            // TODO abstract MailService
            var service = App.Services.GetService<OutlookService>()!;
            MailSender = new MailMessageRecipientData(string.Empty,
                service.CurrentAccount?.Address ?? string.Empty);
            Model = new MailMessageListDetailViewModel(MailMessageData.Empty(MailSender));

            Model.ToRecipients.Add(To);
            InitializeComponent();
        }


        private async void SaveDraft(object Sender, RoutedEventArgs E)
        {
            var service = App.Services.GetService<OutlookService>()!;
            Model.IsDraft = true;
            var mailMessageRecipientData = await service.MailDraftSaveAsync(Model);

            Trace.WriteLine(JsonConvert.SerializeObject(mailMessageRecipientData));
        }

        private async void SaveMailAndSend(object Sender, RoutedEventArgs E)
        {
            var service = App.Services.GetService<OutlookService>()!;
            var mailMessageRecipientData = await service.MailSendAsync(Model);

            Trace.WriteLine(mailMessageRecipientData);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MailMessageListDetailViewModel model)
            {
                Model = model;
                Model.ToRecipients.Add(To);
                MailSender = Model.Sender;
            }

            base.OnNavigatedTo(e);
        }

        public static async Task CreateEditWindow(MailMessageListDetailViewModel? Model = null)
        {
            var appWindow = await AppWindow.TryCreateAsync();
            if (appWindow is null)
            {
                return;
            }

            var appWindowContentFrame = new Frame();
            appWindowContentFrame.Navigate(typeof(EditMail), Model);
            ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);
            await appWindow.TryShowAsync();
        }
    }
}