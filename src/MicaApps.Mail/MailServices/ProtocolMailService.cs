using System.Net;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MicaApps.Mail.Abstraction.MailService;
using MicaApps.Mail.Abstraction.Models;
using MicaApps.Mail.Abstraction.Models.Messages;
using MicaApps.Mail.Abstraction.Models.Messages.Interfaces;
using MimeKit;
using MailFolder = MicaApps.Mail.Abstraction.Models.MailFolder;

namespace MicaApps.Mail.MailServices;

public class ProtocolMailService : MailServiceBase, IDisposable
{
    public override string Id => "protocol";
    public override string Name => "SMTP/IMAP 邮件服务";

    private readonly SmtpClient _smtpClient = new SmtpClient();
    private readonly ImapClient _imapClient = new ImapClient();

    public NetworkCredential? Credential { get; set; }
    public string Host { get; } = null!;
    public int Port { get; } = 0;
    public bool UseSsl { get; } = false;

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _smtpClient.ConnectAsync(Host, Port, UseSsl, cancellationToken: cancellationToken);
        await _smtpClient.AuthenticateAsync(Credential, cancellationToken);

        await _imapClient.ConnectAsync(Host, Port, UseSsl, cancellationToken: cancellationToken);
        await _imapClient.AuthenticateAsync(Credential, cancellationToken);
    }

    public override async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _smtpClient.DisconnectAsync(true, cancellationToken);
        await _smtpClient.DisconnectAsync(true, cancellationToken);
    }

    public override Task<List<MailFolder>> GetMailFoldersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<MailFolder>()
                               {
                                   new()
                                   {
                                       Id = _imapClient.Inbox.Id,
                                       Name = _imapClient.Inbox.Name
                                   }
                               });
    }

    public override async Task<List<MailMessage>> GetMailsInFolderAsync(
        MailFolder mailFolder, CancellationToken cancellationToken = default)
    {
        if (!_imapClient.Inbox.IsOpen)
            await _imapClient.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var results = await _imapClient.Inbox.FetchAsync(
            0, 50, MessageSummaryItems.Envelope | MessageSummaryItems.PreviewText,
            cancellationToken: cancellationToken);
        var ret = new List<MailMessage>();
        foreach (var messageSummary in results)
        {
            var message = new ImapMailMessage
                          {
                              MailId = messageSummary.UniqueId.Id.ToString(),
                              Subject = messageSummary.Envelope.Subject,
                              Body = messageSummary.PreviewText,
                              SendDate = messageSummary.Date
                          };
            if (messageSummary.Envelope.Sender.FirstOrDefault() is MailboxAddress senderAddr)
            {
                message.Sender = new EmailAccount(senderAddr.Address, senderAddr.Name);
            }

            foreach (var recipient in (messageSummary.Envelope.To.OfType<MailboxAddress>()))
            {
                message.Recipients.Add(new EmailAccount(recipient.Address, recipient.Name));
            }

            foreach (var cc in (messageSummary.Envelope.Cc.OfType<MailboxAddress>()))
            {
                message.Recipients.Add(new EmailAccount(cc.Address, cc.Name));
            }

            foreach (var bcc in (messageSummary.Envelope.Bcc.OfType<MailboxAddress>()))
            {
                message.Recipients.Add(new EmailAccount(bcc.Address, bcc.Name));
            }

            ret.Add(message);
        }

        return ret;
    }

    public override async Task<MailMessage?> GetMailDetailAsync(
        string id, CancellationToken cancellationToken = default)
    {
        if (!uint.TryParse(id, out var uniqueId))
            return null;
        if (!_imapClient.Inbox.IsOpen)
            await _imapClient.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var messageSummary = await _imapClient.Inbox.GetMessageAsync(new UniqueId(uniqueId), cancellationToken);

        var message = new ImapMailMessage
                      {
                          MailId = id,
                          Subject = messageSummary.Subject,
                          Body = messageSummary.TextBody,
                          HtmlBody = messageSummary.HtmlBody,
                          SendDate = messageSummary.Date
                      };
        if (messageSummary.Sender is { } senderAddr)
        {
            message.Sender = new EmailAccount(senderAddr.Address, senderAddr.Name);
        }

        foreach (var recipient in (messageSummary.To.OfType<MailboxAddress>()))
        {
            message.Recipients.Add(new EmailAccount(recipient.Address, recipient.Name));
        }

        foreach (var cc in (messageSummary.Cc.OfType<MailboxAddress>()))
        {
            message.Recipients.Add(new EmailAccount(cc.Address, cc.Name));
        }

        foreach (var bcc in (messageSummary.Bcc.OfType<MailboxAddress>()))
        {
            message.Recipients.Add(new EmailAccount(bcc.Address, bcc.Name));
        }

        return message;
    }

    public override async Task SendMailAsync(MailMessage sendingMailMessage,
                                             CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(sendingMailMessage.Sender.Name, message.Sender.Address));
        foreach (var (email, name) in sendingMailMessage.Recipients)
        {
            message.To.Add(new MailboxAddress(name, email));
        }

        message.Subject = sendingMailMessage.Subject;
        message.Body = new TextPart("plain")
                       {
                           Text = sendingMailMessage.Body
                       };

        if (sendingMailMessage is IHasBcc hasBcc && hasBcc.Bcc is { Count: > 0 })
        {
            foreach (var (email, name) in hasBcc.Bcc)
            {
                message.Bcc.Add(new MailboxAddress(name, email));
            }
        }

        if (sendingMailMessage is IHasCc hasCc && hasCc.Cc is { Count: > 0 })
        {
            foreach (var (email, name) in hasCc.Cc)
            {
                message.Cc.Add(new MailboxAddress(name, email));
            }
        }

        await _smtpClient.SendAsync(message, cancellationToken);
    }

    public void Dispose()
    {
        _smtpClient.Dispose();
        _imapClient.Dispose();
    }
}