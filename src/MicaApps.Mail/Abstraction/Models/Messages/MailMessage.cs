namespace MicaApps.Mail.Abstraction.Models.Messages;

public class MailMessage
{
    public string MailId { get; set; } = null!;
    public string? Subject { get; set; }
    public string Body { get; set; } = null!;
    public DateTimeOffset SendDate { get; set; }
    public EmailAccount Sender { get; set; } = null!;
    public List<EmailAccount> Recipients { get; set; } = new();
}