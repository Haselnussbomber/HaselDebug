using System.Threading;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Windows;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient, AutoConstruct]
public partial class UnlocksColumn : ColumnString<UnlockLinkEntry>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(320);
    }

    public override string ToName(UnlockLinkEntry entry)
        => string.Join(' ', entry.Unlocks.Select(GetSheetRowLabel));

    public override void DrawColumn(UnlockLinkEntry entry)
    {
        foreach (var unlock in entry.Unlocks)
        {
            if (unlock.RowType == null)
            {
                ImGui.Text("");
            }
            else
            {
                if (ImGui.Selectable($"{GetSheetRowLabel(unlock)}{unlock.ExtraSheetText}"))
                {
                    var title = $"{GetSheetRowLabel(unlock)} ({_languageProvider.ClientLanguage})";
                    _windowManager.CreateOrOpen(title, () => new ExcelRowTab(_windowManager, _textService, _serviceProvider, unlock.RowType, unlock.RowId, _languageProvider.ClientLanguage, title));
                }

                ImGuiContextMenu.Draw($"Entry{entry.Index}_{unlock.RowType.Name}{unlock.RowId}_RowIdContextMenu", builder =>
                {
                    builder.AddCopyRowId(unlock.RowId);
                });
            }
        }
    }

    private string GetSheetRowLabel(UnlockEntry unlock)
    {
        if (unlock.RowType == null)
            return string.Empty;

        var label = $"{unlock.RowType.Name}#{unlock.RowId}";

        if (unlock.SubrowId != null)
            label += $".{unlock.SubrowId}";

        return label;
    }
}
