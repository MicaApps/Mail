using MicaApps.Mail.Abstraction.Models;

namespace MicaApps.Mail.Abstraction.MailService;

public abstract class MailServiceBase
{
    public abstract string Id { get; }
    public abstract string Name { get; }

    public abstract Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);

    public abstract Task<List<MailFolder>> GetMailFoldersAsync(CancellationToken cancellationToken = default);

    public abstract Task<List<SingleMailMessage>> GetMailsInFolderAsync(
        MailFolder mailFolder, CancellationToken cancellationToken = default);

    public abstract Task<SingleMailMessage>
        GetMailDetailAsync(string id, CancellationToken cancellationToken = default);


    public abstract Task<bool> SendMailAsync(SingleMailMessage mailMessage, CancellationToken cancellationToken = default);
}