/*
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AllocFunctions : IDebugTab
{
    public string Title => "AllocFunctions";
    public string InternalName => "AllocFunctions";
    public bool DrawInChild => true;

    public unsafe void Draw()
    {
        var raptureAtkModule = RaptureAtkModule.Instance();

        using var table = ImRaii.Table("AllocFunctions", 3 /*9* /);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        //ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 150);
        //ImGui.TableSetupColumn("Unk8", ImGuiTableColumnFlags.WidthFixed, 100);
        //ImGui.TableSetupColumn("UnkC", ImGuiTableColumnFlags.WidthFixed, 100);
        //ImGui.TableSetupColumn("Dependencies", ImGuiTableColumnFlags.WidthFixed, 100);
        //ImGui.TableSetupColumn("NumDependencies", ImGuiTableColumnFlags.WidthFixed, 100);
        //ImGui.TableSetupColumn("Unk1C", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("AlwaysLoaded", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        var addonAllocators = new Span<AddonAllocator>((void*)((nint)raptureAtkModule + 0x87F8), 873);

        for (var i = 0; i < addonAllocators.Length; i++)
        {
            var entry = addonAllocators.GetPointer(i);
            if (entry == null)
                continue;
            if (entry->AlwaysLoaded == 0)
                continue;

            var name = raptureAtkModule->AddonNames[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"#{i}");

            ImGui.TableNextColumn();
            ImGui.Text($"{name}");

            ImGui.TableNextColumn();
            if (entry->AlwaysLoaded == 1)
                ImGui.Text($"{entry->AlwaysLoaded}");
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct AddonAllocator
{
    // [FieldOffset(0x00)] public nint AllocFunction;
    // [FieldOffset(0x08)] public int ThisOffset;
    // [FieldOffset(0x0C)] public uint UnkC;
    // [FieldOffset(0x10)] public nint Dependencies;
    // [FieldOffset(0x18)] public int NumDependencies;
    // [FieldOffset(0x1C)] public int Unk1C;
    [FieldOffset(0x20)] public byte AlwaysLoaded; // bool?
}
*/
