namespace Mail.Models;

/// <summary>
/// 文件附件对象<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/05/21
/// </summary>
public class AttachmentDataModel
{
    public string ContentType { get; }
    public string AttachmentId { get; }

    public AttachmentDataModel(string attachmentId, string name, byte[] contentBytes, int size, string contentType)
    {
        AttachmentId = attachmentId;
        Size = size;
        ContentType = contentType;
        Name = name;
        ContentBytes = contentBytes;
    }

    /// <summary>
    /// example: example.png
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 附件数据
    /// </summary>
    public byte[] ContentBytes { get; }

    public int Size { get; }
}