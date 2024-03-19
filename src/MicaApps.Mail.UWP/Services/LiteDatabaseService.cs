using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using LiteDB;
using Mail.Models;

namespace Mail.Services;

public class LiteDatabaseService
{
    ILiteCollection<MailFolder> _mailFolders = null;
    ILiteCollection<MailMessage> _mailMessages = null;
    //ILiteCollection<MailMessageContent> _mailMessageContents;
    //ILiteCollection<MailMessageRecipient> _mailMessageRecipients;

    public LiteDatabaseService(ILiteDatabase liteDatabase)
    {
        LiteDatabase = liteDatabase;
    }

    public ILiteDatabase LiteDatabase { get; }
    public ILiteCollection<MailFolder> MailFolders => _mailFolders ??= LiteDatabase.GetCollection<MailFolder>();
    public ILiteCollection<MailMessage> MailMessages => _mailMessages ??= LiteDatabase.GetCollection<MailMessage>();
    //public ILiteCollection<MailMessageContent> MailMessageContents => _mailMessageContents ??= LiteDatabase.GetCollection<MailMessageContent>();
    //public ILiteCollection<MailMessageRecipient> MailMessageRecipients => _mailMessageRecipients ??= LiteDatabase.GetCollection<MailMessageRecipient>();
}