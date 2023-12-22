namespace MicaApps.Mail.Abstraction.Models;

public class MailAttachment
{
    public string Id { get; }
    public string? Name { get; }
    public string? ContentType { get; }
    public long Length { get; }

    public MailAttachment(string id,string? name,string? contentType,long length)
    {
        Id = id;
        Name = name;
        ContentType = contentType;
        Length = length;
    }
}