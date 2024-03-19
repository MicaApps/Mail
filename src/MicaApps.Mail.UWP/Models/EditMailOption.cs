using Mail.Models.Enums;

namespace Mail.Models;

public class EditMailOption
{
    public EditMailType EditMailType { get; set; }
    public MailMessageListDetailViewModel? Model { get; set; }
    public bool IsReplyAll { get; set; } = false;

    /// <summary>
    /// 0 == 
    /// </summary>
    public uint AutoSaveTimer { get; set; }
}
