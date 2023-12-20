namespace Mail.Models;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/20
/// </summary>
public sealed class LoadMailMessageOption
{
    public string FolderId { get; set; } = string.Empty;

    public int StartIndex { get; set; }

    public int LoadCount { get; set; } = 30;

    public bool IsFocusedTab { get; set; } = true;
}