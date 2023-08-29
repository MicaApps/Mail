using System.Net;
using FluentAssertions;
using MicaApps.Mail.MailServices;
using NSubstitute;

namespace MicaApps.Mail.Tests;


public class ProtocolMailServiceTests : IAsyncLifetime
{

    private readonly ProtocolMailService _mailService;

    

    public ProtocolMailServiceTests(ProtocolMailService mailService)
    {
        _mailService = new ProtocolMailService();
        _mailService.Name = "测试服务";
        _mailService.Host = Secrets.ProtocolMailSettings.Host;
        _mailService.UseSsl = Secrets.ProtocolMailSettings.UseSsl;
        _mailService.Port = Secrets.ProtocolMailSettings.Port;
        _mailService.Credential =
            new NetworkCredential(Secrets.ProtocolMailSettings.Username, Secrets.ProtocolMailSettings.Password);
        
    }
    
    [Fact]
    public async void FetchMessages()
    {
        // Arrange
        var folders = await _mailService.GetMailFoldersAsync();
        
        // Action
        var mails = await _mailService.GetMailsInFolderAsync(folders[0]);
        
        // Assert
        mails.Should().NotBeEmpty();
    }

    public async Task InitializeAsync()
    {
        await _mailService.ConnectAsync();
    }

    public async Task DisposeAsync()
    {
        await _mailService.DisconnectAsync();
        _mailService.Dispose();
    }
}