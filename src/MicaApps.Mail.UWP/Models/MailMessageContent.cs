using Mail.Models.Enums;

namespace Mail.Models;

public class MailMessageContent
{
    public MailMessageContent()
    {
    }

    public MailMessageContent(string content, string contentPreview, MailMessageContentType contentType)
    {
        Content = content;
        ContentPreview = contentPreview;
        ContentType = contentType;
    }

    public string Content { get; set; }
    public string ContentPreview { get; set; }
    public MailMessageContentType ContentType { get; set; }


}
