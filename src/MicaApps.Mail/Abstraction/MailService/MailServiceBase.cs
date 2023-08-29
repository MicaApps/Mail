using MicaApps.Mail.Abstraction.Models;
using MicaApps.Mail.Abstraction.Models.Messages;

namespace MicaApps.Mail.Abstraction.MailService;

public abstract class MailServiceBase
{
    public abstract string Id { get; set; }
    public abstract string Name { get; set; }

    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
    public abstract Task DisconnectAsync(CancellationToken cancellationToken = default);

    public abstract Task<List<MailFolder>> GetMailFoldersAsync(CancellationToken cancellationToken = default);

    public abstract Task<List<MailMessage>> GetMailsInFolderAsync(
        MailFolder mailFolder, CancellationToken cancellationToken = default);

    public abstract Task<MailMessage?> GetMailDetailAsync(string id, CancellationToken cancellationToken = default);


    public abstract Task SendMailAsync(MailMessage sendingMailMessage, CancellationToken cancellationToken = default);
}