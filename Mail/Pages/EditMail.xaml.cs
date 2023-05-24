using Windows.UI.Xaml.Controls;
using Mail.Models;
using Mail.Services.Data;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Mail.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EditMail : Page
    {
        private MailMessageListDetailViewModel Model;

        public EditMail()
        {
            Model = MailMessageListDetailViewModel.Empty(new MailMessageRecipientData(string.Empty, string.Empty));
            InitializeComponent();
        }
    }
}