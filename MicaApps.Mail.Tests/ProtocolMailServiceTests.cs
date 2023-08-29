using MicaApps.Mail.MailServices;
using NSubstitute;

namespace MicaApps.Mail.Tests;


public class ProtocolMailServiceTests
{

    private readonly ProtocolMailService _mailService;
    
    public ProtocolMailServiceTests(ProtocolMailService mailService)
    {
        _mailService = new ProtocolMailService();
        _mailService.Name = "测试服务";
        _mailService.Host = "";
    }
    
    [Fact]
    public async void FetchMessages()
    {
        
    }
}