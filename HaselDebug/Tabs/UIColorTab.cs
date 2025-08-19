using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class UIColorTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;

    public override string Title => "UIColor";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("UIColorTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn(_textService.GetAddonText(4232)); // Dark
        ImGui.TableSetupColumn(_textService.GetAddonText(4233)); // Light
        ImGui.TableSetupColumn(_textService.GetAddonText(4234)); // Classic FF
        ImGui.TableSetupColumn(_textService.GetAddonText(4235)); // Clear Blue
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        foreach (var row in _excelService.GetSheet<UIColor>())
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(row.RowId.ToString());

            ImGui.TableNextColumn();
            var color = (Vector4)Color.FromABGR(row.Dark);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Dark", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Light);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Light", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.ClassicFF);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_ClassicFF", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.ClearBlue);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_ClearBlue", ref color, ImGuiColorEditFlags.DisplayHex);
        }
    }
}
