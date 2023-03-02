using Mail.Services.Data;

namespace Mail.Models
{
    internal sealed class MailMessageListDetailViewModel
    {
        public string Title => InnerData.Title;

        public string PreviewText => InnerData.Content.ContentPreview;

        public string SenderName => InnerData.Sender.Name;

        public string Content => InnerData.Content.Content;

        public MailMessageContentType ContentType => InnerData.Content.ContentType;

        private readonly MailMessageData InnerData;

        public MailMessageListDetailViewModel(MailMessageData Data)
        {
            InnerData = Data;
        }
    }
}
