namespace Mail.Services.Data
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

        public static MailMessageContentData Empty()
        {
            return new MailMessageContentData(string.Empty, string.Empty, MailMessageContentType.Text);
        }
    }
}
