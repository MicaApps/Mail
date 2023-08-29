namespace MicaApps.Mail.Abstraction.Models.Messages.Interfaces;

public interface IHasCc
{
    public List<EmailAccount> Cc { get; set; }
}