using MicaApps.Mail.Abstraction.Models.Messages.Interfaces;

namespace MicaApps.Mail.Abstraction.Models.Messages;

public class ImapMailMessage : MailMessage, IHasBcc, IHasCc, IHasHtmlBody
{
    public List<EmailAccount> Bcc { get; set; } = new();
    public List<EmailAccount> Cc { get; set; } = new();
    public string? HtmlBody { get; set; }
}