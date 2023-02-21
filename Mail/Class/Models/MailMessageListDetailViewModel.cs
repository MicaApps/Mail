using Mail.Class.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Mail.Class.Models
{
    internal sealed class MailMessageListDetailViewModel
    {
        public string Title => InnerData.Title;

        public string PreviewText => InnerData.Content.ContentPreview;

        public string SenderName => InnerData.Sender.Name;

        public string Content => InnerData.Content.Content;

        private readonly MailMessageData InnerData;

        public MailMessageListDetailViewModel(MailMessageData Data)
        {
            InnerData = Data;
        }
    }
}
