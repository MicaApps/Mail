namespace MicaApps.Mail.Abstraction.Models;

public class SingleMailMessage
{
    public string MailId { get; set; } = null!;
    public string? Subject { get; set; }
    public string Body { get; set; } = null!;
    public EmailAccount Sender { get; set; } = null!;
    public List<EmailAccount> Recipients { get; set; } = new();
}