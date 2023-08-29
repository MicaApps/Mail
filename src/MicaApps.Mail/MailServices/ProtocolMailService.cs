using System.Net;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MicaApps.Mail.Abstraction.MailService;
using MicaApps.Mail.Abstraction.Models;
using MicaApps.Mail.Abstraction.Models.Messages;
using MicaApps.Mail.Abstraction.Models.Messages.Interfaces;
using MimeKit;
using MailFolder = MicaApps.Mail.Abstraction.Models.MailFolder;

namespace MicaApps.Mail.MailServices;

public class ProtocolMailService : MailServiceBase, IProgressiveEmailFetchable, IAttachmentGettable, IDisposable
{
    public override string Id { get; set; } = "protocol";
    public override string Name { get; set; } = "SMTP/IMAP 邮件服务";

    private readonly SmtpClient _smtpClient = new SmtpClient();
    private readonly ImapClient _imapClient = new ImapClient();

    public ProtocolMailSettings SmtpSettings = new ProtocolMailSettings();
    public ProtocolMailSettings ImapSettings = new ProtocolMailSettings();


    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _smtpClient.ConnectAsync(SmtpSettings.Host, SmtpSettings.Port, (SecureSocketOptions)SmtpSettings.SecureType,
                                       cancellationToken: cancellationToken);
        await _smtpClient.AuthenticateAsync(SmtpSettings.Username, SmtpSettings.Password,
                                            cancellationToken: cancellationToken);

        await _imapClient.ConnectAsync(ImapSettings.Host, ImapSettings.Port, (SecureSocketOptions)ImapSettings.SecureType,
                                       cancellationToken: cancellationToken);
        await _imapClient.AuthenticateAsync(ImapSettings.Username, ImapSettings.Password,
                                            cancellationToken: cancellationToken);
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
        return await GetMailsInFolderAsync(mailFolder, 0, 50, cancellationToken);
    }

    public async Task<List<MailMessage>> GetMailsInFolderAsync(MailFolder mailFolder, int start, int count,
                                                               CancellationToken cancellationToken = default)
    {
        if (!_imapClient.Inbox.IsOpen)
            await _imapClient.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var results = (await _imapClient.Inbox.FetchAsync(
            _imapClient.Inbox.Count - start - count, _imapClient.Inbox.Count - start, MessageSummaryItems.Envelope | MessageSummaryItems.PreviewText,
            cancellationToken: cancellationToken)).Reverse();
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

            if (messageSummary.Attachments.Any())
            {
                foreach (var messageSummaryAttachment in messageSummary.Attachments)
                {
                    message.Attachments.Add(new ImapMailAttachment(messageSummaryAttachment.ContentId,
                                                                   messageSummaryAttachment.FileName,
                                                                   messageSummaryAttachment.ContentType.MimeType,
                                                                   messageSummaryAttachment.Octets,
                                                                   messageSummaryAttachment, messageSummary.UniqueId));
                }
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

    public async Task<MailAttachmentContent?> GetAttachmentContentAsync(MailAttachment attachment)
    {
        if (attachment is not ImapMailAttachment mailAttachment) return null;
        var mime = (MimePart)await _imapClient.Inbox.GetBodyPartAsync(mailAttachment.MailId, mailAttachment.Body);
        return new ImapMailAttachmentContent(mime.ContentId, mime.FileName, mime.ContentType.MimeType,
                                             attachment.Length, mime);
    }
}

public class ProtocolMailSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public SecureType SecureType { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public enum SecureType
{
    /// <summary>No SSL or TLS encryption should be used.</summary>
    None,

    /// <summary>
    /// Allow the <see cref="T:MailKit.IMailService" /> to decide which SSL or TLS
    /// options to use (default). If the server does not support SSL or TLS,
    /// then the connection will continue without any encryption.
    /// </summary>
    Auto,

    /// <summary>
    /// The connection should use SSL or TLS encryption immediately.
    /// </summary>
    SslOnConnect,

    /// <summary>
    /// Elevates the connection to use TLS encryption immediately after
    /// reading the greeting and capabilities of the server. If the server
    /// does not support the STARTTLS extension, then the connection will
    /// fail and a <see cref="T:System.NotSupportedException" /> will be thrown.
    /// </summary>
    StartTls,

    /// <summary>
    /// Elevates the connection to use TLS encryption immediately after
    /// reading the greeting and capabilities of the server, but only if
    /// the server supports the STARTTLS extension.
    /// </summary>
    StartTlsWhenAvailable,
}

public class ImapMailMessage : MailMessage, IHasBcc, IHasCc, IHasHtmlBody, IHasAttachments
{
    public List<EmailAccount> Bcc { get; set; } = new();
    public List<EmailAccount> Cc { get; set; } = new();
    public string? HtmlBody { get; set; }
    public List<MailAttachment> Attachments { get; set; } = new();
}

public class ImapMailAttachment : MailAttachment
{
    public BodyPartBasic Body { get; }
    public UniqueId MailId { get; }

    public ImapMailAttachment(string id, string? name, string? contentType, long length, BodyPartBasic body,
                              UniqueId mailId) : base(id, name, contentType, length)
    {
        Body = body;
        MailId = mailId;
    }
}

public class ImapMailAttachmentContent : MailAttachmentContent
{
    public MimePart MimePart { get; set; }

    public ImapMailAttachmentContent(string id, string? name, string? contentType, long length, MimePart mimePart) :
        base(id, name, contentType, length)
    {
        MimePart = mimePart;
    }

    public override async Task WriteToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await MimePart.WriteToAsync(stream, cancellationToken);
    }
}