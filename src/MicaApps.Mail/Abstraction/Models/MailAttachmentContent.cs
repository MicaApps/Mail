namespace MicaApps.Mail.Abstraction.Models;

public class MailAttachmentContent
{
    public MailAttachmentContent(string id, string? name, string? contentType, long length)
    {
        Id = id;
        Name = name;
        ContentType = contentType;
        Length = length;
    }

    public virtual Task WriteToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public string Id { get; }
    public string? Name { get; }
    public string? ContentType { get; }
    public long Length { get; }
}