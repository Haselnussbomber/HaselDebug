/*
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using ImGuiNET;
using HaselDebug.Abstracts;

namespace HaselDebug.Tabs;

public unsafe class WindowFlagTab : DebugTab
{
    private string addonName = string.Empty;

    public override void Draw()
    {
        ImGui.InputTextWithHint("##WindowName", "Addon Name", ref addonName, 255);

        if (!TryGetAddon<AtkUnitBase>(addonName, out var addon))
            return;

        var windowFlags = *(uint*)((nint)addon + 0x1C0);
        ImGui.TextUnformatted($"{addonName} WindowFlags: {Convert.ToString(windowFlags, 2)} - 0x{windowFlags:X}");
        using var table = ImRaii.Table("WindowFlagsTable", 5);
        if (!table) return;

        for (var i = 0; i < 31; i++)
        {
            ImGui.TableNextRow();
            var bit = 1 << i;
            var isSet = (windowFlags & bit) == bit;
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, (uint)(isSet ? Colors.Green : Colors.Red));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{i}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"0x{bit:X}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{Enum.GetName(typeof(WindowFlag), bit)}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isSet}");
            ImGui.TableNextColumn();
            textColor?.Dispose();
            if (ImGui.Button($"Toggle##ToggleFlag{i}"))
            {
                *(uint*)((nint)addon + 0x1C0) ^= (uint)bit;
            }
        }
    }
}

[Flags]
public enum WindowFlag : uint
{
    Unk0 = 1 << 0, // 0x1
    Unk1 = 1 << 1, // 0x2 - set after Hide(), unset after Show()
    NoTitleBarContextMenu = 1 << 2, // 0x4
    Unk3 = 1 << 3, // 0x8
    IgnoreUiHidden = 1 << 4, // 0x10
    Unk5 = 1 << 5, // 0x20 - see RaptureAtkUnitManager_vf41
    Unk6 = 1 << 6, // 0x40
    Unk7 = 1 << 7, // 0x80
    Unk8 = 1 << 8, // 0x100 - something with HostID
    Unk9 = 1 << 9, // 0x200
    Unk10 = 1 << 10, // 0x400 - something with HostID
    NoScaling = 1 << 11, // 0x800
    NoInputs = 1 << 12, // 0x1000 - just handles focus and dragging title bar
    Unk13 = 1 << 13, // 0x2000 - set before Show()
    Unk14 = 1 << 14, // 0x4000
    Unk15 = 1 << 15, // 0x8000
    Unk16 = 1 << 16, // 0x10000
    Unk17 = 1 << 17, // 0x20000
    Unk18 = 1 << 18, // 0x40000
    Unk19 = 1 << 19, // 0x80000
    PreferCallbackToClose = 1 << 20, // 0x100000 - check again wtf this is
    Unk21 = 1 << 21, // 0x200000
    Unk22 = 1 << 22, // 0x400000
    Unk23 = 1 << 23, // 0x800000
    Unk24 = 1 << 24, // 0x1000000
    Unk25 = 1 << 25, // 0x2000000
    Unk26 = 1 << 26, // 0x4000000
    Unk27 = 1 << 27, // 0x8000000
    Unk28 = 1 << 28, // 0x10000000
    Unk29 = 1 << 29, // 0x20000000
    Unk30 = 1 << 30, // 0x40000000
}
*/
