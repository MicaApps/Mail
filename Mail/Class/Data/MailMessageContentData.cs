using Mail.Enum;

namespace Mail.Class.Data
{
    internal sealed class MailMessageContentData
    {
        public string Content { get; }

        public string ContentPreview { get; }

        public MailMessageContentType ContentType { get; }

        public MailMessageContentData(string Content, string ContentPreview, MailMessageContentType ContentType)
        {
            this.Content = Content;
            this.ContentPreview = ContentPreview;
            this.ContentType = ContentType;
        }
    }
}
