using Mail.Services.Data;
using System.Collections.Generic;

namespace Mail.Models
{
    internal sealed class MailMessageListDetailViewModel
    {
        public string Title => InnerData.Title;

        public string Id => InnerData.Id;

        public string PreviewText => InnerData.Content.ContentPreview;

        public string SenderName => InnerData.Sender.Name;

        public MailMessageRecipientData Sender => InnerData.Sender;

        public IReadOnlyList<MailMessageRecipientData> To => InnerData.To;

        public string Content => InnerData.Content.Content;

        public MailMessageContentType ContentType => InnerData.Content.ContentType;

        public string SentTime => InnerData.SentTime.HasValue ? InnerData.SentTime.Value.DateTime.ToString("yyyy/M/dd") : string.Empty;

        private readonly MailMessageData InnerData;

        public MailMessageListDetailViewModel(MailMessageData Data)
        {
            InnerData = Data;
        }
    }
}
