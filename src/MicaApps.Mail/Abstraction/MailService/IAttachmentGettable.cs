using MicaApps.Mail.Abstraction.Models;
using MicaApps.Mail.Abstraction.Models.Messages;

namespace MicaApps.Mail.Abstraction.MailService;

public interface IAttachmentGettable
{
    public Task<MailAttachmentContent?> GetAttachmentContentAsync(MailAttachment attachment);
}