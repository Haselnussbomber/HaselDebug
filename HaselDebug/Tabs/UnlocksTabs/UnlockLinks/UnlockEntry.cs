using HaselCommon.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks;

public class UnlockEntry
{
    public required Type RowType { get; set; }
    public uint RowId { get; set; }
    public uint? SubrowId { get; set; }
    public string ExtraSheetText { get; set; } = string.Empty;
    public uint IconId { get; set; }
    public string TexturePath { get; set; } = string.Empty;
    public DrawInfo DrawInfo { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
