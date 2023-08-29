namespace MicaApps.Mail.Abstraction.Models.Messages.Interfaces;

public interface IHasBcc
{
    public List<EmailAccount> Bcc { get; set; }
}