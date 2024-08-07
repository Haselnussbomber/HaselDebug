/*
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.Interop;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AllocFunctionsTab : IDebugWindowTab
{
    public string Title => "AllocFunctions";

    public unsafe void Draw()
    {
        var raptureAtkModule = RaptureAtkModule.Instance();

        using var table = ImRaii.Table("AllocFunctions", 9);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Unk8", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("UnkC", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Dependencies", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("NumDependencies", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Unk1C", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("AlwaysLoaded", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        for (var i = 0; i < raptureAtkModule->AddonAllocatorsSpan.Length; i++)
        {
            var entry = raptureAtkModule->AddonAllocatorsSpan.GetPointer(i);
            if (entry == null) continue;

            var name = raptureAtkModule->AddonNames.Get((ulong)i);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"#{i}");

            ImGui.TableNextColumn();
            ImGui.Text($"{name}");

            ImGui.TableNextColumn();
            if ((nint)entry->AllocFunction != 0)
                Debug.DrawCopyableText($"ffxiv_dx11.exe+{(nint)entry->AllocFunction - Service.SigScanner.Module.BaseAddress:X}");

            ImGui.TableNextColumn();
            if (entry->Unk8 != 0)
                ImGui.Text($"{entry->Unk8}");
            ImGui.TableNextColumn();
            if (entry->UnkC != 32758)
                ImGui.Text($"{entry->UnkC}");
            ImGui.TableNextColumn();
            if (entry->Dependencies != 0)
                Debug.DrawCopyableText($"{entry->Dependencies:X}");
            ImGui.TableNextColumn();
            if (entry->NumDependencies != 0)
                ImGui.Text($"{entry->NumDependencies}");
            ImGui.TableNextColumn();
            if (entry->Unk1C != 0)
                ImGui.Text($"{entry->Unk1C}");
            ImGui.TableNextColumn();
            if (entry->AlwaysLoaded)
                ImGui.Text($"{entry->AlwaysLoaded}");
        }
    }
}
*/
