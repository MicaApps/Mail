namespace Mail.Models;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/20
/// </summary>
public class LoadMailMessageOption
{
    public LoadMailMessageOption(string FolderId)
    {
        this.FolderId = FolderId;
    }

    public LoadMailMessageOption(string FolderId, bool IsFocusedTab)
    {
        this.FolderId = FolderId;
        this.IsFocusedTab = IsFocusedTab;
    }

    public string FolderId { get; set; }
    public bool IsFocusedTab { get; set; } = true;
    public int StartIndex { get; set; }
    public int LoadCount { get; set; } = 30;
}