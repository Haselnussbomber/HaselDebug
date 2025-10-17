using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;

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
        using var table = ImRaii.Table("UIColorTable"u8, 9, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn(_textService.GetAddonText(4232)); // Dark
        ImGui.TableSetupColumn(_textService.GetAddonText(4233)); // Light
        ImGui.TableSetupColumn(_textService.GetAddonText(4234)); // Classic FF
        ImGui.TableSetupColumn(_textService.GetAddonText(4235)); // Clear Blue
        ImGui.TableSetupColumn("Unknown0"u8);
        ImGui.TableSetupColumn("Unknown1"u8);
        ImGui.TableSetupColumn("Unknown2"u8);
        ImGui.TableSetupColumn("Unknown3"u8);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var row in _excelService.GetSheet<UIColor>())
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(row.RowId.ToString());

            ImGui.TableNextColumn();
            var color = (Vector4)Color.FromABGR(row.Dark);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Dark", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Light);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Light", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.ClassicFF);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_ClassicFF", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.ClearBlue);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_ClearBlue", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Unknown0);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Unknown0", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Unknown1);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Unknown1", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Unknown2);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Unknown2", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Unknown3);
            ImGui.SetNextItemWidth(-1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Unknown3", ref color, ImGuiColorEditFlags.DisplayHex);
        }
    }
}
