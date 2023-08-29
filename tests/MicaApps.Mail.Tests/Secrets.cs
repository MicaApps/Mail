﻿using MicaApps.Mail.Abstraction.Models;

namespace MicaApps.Mail.Tests;

// THIS FILE SHOULD BE FILLED BY GITHUB ACTION
// **DO NOT** COMMIT YOUR SECRETS TO ORIGIN
public static class Secrets
{
    // TODO: Please fill this secrets file
    public static ProtocolMailSettings SmtpSettings =
        new()
        {
            Host = "",
            Port = 0,
            UseSsl = true,
            Username = "",
            Password = ""
        };
    public static ProtocolMailSettings ImapSettings =
        new()
        {
            Host = "",
            Port = 0,
            UseSsl = true,
            Username = "",
            Password = ""
        };
}