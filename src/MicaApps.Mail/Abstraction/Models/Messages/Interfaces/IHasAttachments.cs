namespace MicaApps.Mail.Abstraction.Models.Messages.Interfaces;

public interface IHasAttachments
{
    public List<MailAttachment> Attachments { get; set; }
}