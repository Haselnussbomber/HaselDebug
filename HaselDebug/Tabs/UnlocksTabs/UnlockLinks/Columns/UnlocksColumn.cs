using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient, AutoConstruct]
public partial class UnlocksColumn : ColumnString<UnlockLinkEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(300);
    }

    public override string ToName(UnlockLinkEntry entry)
        => string.Join(' ', entry.Unlocks.Select(unlock => $"{unlock.RowType.Name}#{unlock.RowId}"));

    public override unsafe void DrawColumn(UnlockLinkEntry entry)
    {
        foreach (var unlock in entry.Unlocks)
        {
            if (ImGui.Selectable($"{unlock.RowType.Name}#{unlock.RowId}{unlock.ExtraSheetText}"))
            {
                var title = $"{unlock.RowType.Name}#{unlock.RowId} ({_languageProvider.ClientLanguage})";
                _windowManager.CreateOrOpen(title, () => new ExcelRowTab(_windowManager, _textService, _languageProvider, _debugRenderer, unlock.RowType, unlock.RowId, _languageProvider.ClientLanguage, title));
            }

            _imGuiContextMenu.Draw($"Entry{entry.Index}_{unlock.RowType.Name}{unlock.RowId}_RowIdContextMenu", builder =>
            {
                builder.AddCopyRowId(_textService, unlock.RowId);
            });
        }
    }
}
