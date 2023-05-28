namespace Mail.Services.Data
{
    public sealed class MailMessageContentData
    {
        public string Content { get; set; }

        public string ContentPreview { get; set; }

        public MailMessageContentType ContentType { get; set; }

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