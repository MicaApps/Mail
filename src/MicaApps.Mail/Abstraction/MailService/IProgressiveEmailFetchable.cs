using MicaApps.Mail.Abstraction.Models;
using MicaApps.Mail.Abstraction.Models.Messages;

namespace MicaApps.Mail.Abstraction.MailService;

public interface IProgressiveEmailFetchable
{
    public Task<List<MailMessage>> GetMailsInFolderAsync(MailFolder mailFolder, int start, int count, 
                                                         CancellationToken cancellationToken = default);
}