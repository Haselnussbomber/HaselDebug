using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using ImGuiNET;
using InteropGenerator.Runtime.Attributes;

namespace HaselDebug.Tabs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0)]
public unsafe partial struct AddonConfigFunctions
{
    [MemberFunction("E8 ?? ?? ?? ?? 41 8B CE E8 ?? ?? ?? ?? 48 8B C8")]
    public static partial byte* GetNameByIndex(uint index);

    [MemberFunction("E8 ?? ?? ?? ?? 3B C7 74 1E")]
    public static partial uint GetNameHash(byte* name);
}

public unsafe class AddonConfigTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly Dictionary<uint, string> _addonNames = [];

    public AddonConfigTab(DebugRenderer DebugRenderer)
    {
        _debugRenderer = DebugRenderer;

        for (var i = 0u; i < 99; i++)
        {
            var namePtr = AddonConfigFunctions.GetNameByIndex(i);
            var name = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(namePtr));
            var hash = AddonConfigFunctions.GetNameHash(namePtr);
            _addonNames[hash] = name;
        }

        foreach (var addonName in RaptureAtkModule.Instance()->AddonNames)
        {
            if (addonName.IsEmpty || addonName.StringPtr == null)
                continue;

            var hash = AddonConfigFunctions.GetNameHash(addonName.StringPtr);
            _addonNames[hash] = addonName.ToString();

            for (var i = 0u; i < 10; i++)
            {
                var numName = Utf8String.CreateEmpty();
                numName->SetString($"{addonName.ToString().TrimEnd('\0')}{i}");

                if (numName->StringPtr == null)
                    continue;

                hash = AddonConfigFunctions.GetNameHash(numName->StringPtr);
                _addonNames[hash] = numName->ToString();

                numName->SetString($"{addonName.ToString().TrimEnd('\0')}{i:00}");

                if (numName->StringPtr == null)
                    continue;

                hash = AddonConfigFunctions.GetNameHash(numName->StringPtr);
                _addonNames[hash] = numName->ToString();
                numName->Dtor(true);
            }
        }
    }

    public override void Draw()
    {
        var addonConfig = AddonConfig.Instance();

        using var tabbar = ImRaii.TabBar("AddonConfigTabBar");
        if (!tabbar) return;

        using (var tab = ImRaii.TabItem("Global Configs"))
        {
            if (tab)
            {
                DrawTable(addonConfig->ModuleData->ConfigEntries);
            }
        }

        using (var tab = ImRaii.TabItem("HudLayout Configs"))
        {
            if (tab)
            {
                using var tabbar2 = ImRaii.TabBar("AddonConfigTabBar");
                if (tabbar)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        using var hudLayoutTab = ImRaii.TabItem($"HudLayout {i}");
                        if (hudLayoutTab)
                        {
                            DrawHudLayoutTable(addonConfig->ModuleData->HudLayoutConfigEntries, i);
                        }
                    }
                }
            }
        }
    }

    private void DrawTable(Span<AddonConfigEntry> configEntries)
    {
        using var table = ImRaii.Table("AddonConfigTable", 14, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Addon", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("X");
        ImGui.TableSetupColumn("Y");
        ImGui.TableSetupColumn("Scale");
        ImGui.TableSetupColumn("ElementFlags");
        ImGui.TableSetupColumn("Width");
        ImGui.TableSetupColumn("Height");
        ImGui.TableSetupColumn("ByteValue1");
        ImGui.TableSetupColumn("ByteValue2");
        ImGui.TableSetupColumn("ByteValue3");
        ImGui.TableSetupColumn("Alpha");
        ImGui.TableSetupColumn("HasValue");
        ImGui.TableSetupColumn("IsOpen");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < configEntries.Length; i++)
        {
            DrawRow(i, configEntries.GetPointer(i));
        }
    }

    private void DrawHudLayoutTable(Span<AddonConfigEntry> configEntries, int hudLayoutIndex)
    {
        using var table = ImRaii.Table("AddonConfigTable", 14, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Addon", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("X");
        ImGui.TableSetupColumn("Y");
        ImGui.TableSetupColumn("Scale");
        ImGui.TableSetupColumn("ElementFlags");
        ImGui.TableSetupColumn("Width");
        ImGui.TableSetupColumn("Height");
        ImGui.TableSetupColumn("ByteValue1");
        ImGui.TableSetupColumn("ByteValue2");
        ImGui.TableSetupColumn("ByteValue3");
        ImGui.TableSetupColumn("Alpha");
        ImGui.TableSetupColumn("HasValue");
        ImGui.TableSetupColumn("IsOpen");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var perHudLayout = configEntries.Length / 4;

        for (var i = perHudLayout * hudLayoutIndex; i < perHudLayout * (hudLayoutIndex + 1); i++)
        {
            DrawRow(i, configEntries.GetPointer(i));
        }
    }

    private void DrawRow(int i, AddonConfigEntry* configEntry)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(i.ToString());

        ImGui.TableNextColumn();
        var hash = configEntry->AddonNameHash;
        ImGui.TextUnformatted(_addonNames.TryGetValue(hash, out var name) ? name : hash.ToString("X"));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->X.ToString("0.###", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->Y.ToString("0.###", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->Scale.ToString("0.0", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("0x" + configEntry->ElementFlags.ToString("X"));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->Width.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->Height.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("0x" + configEntry->ByteValue1.ToString("X"));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("0x" + configEntry->ByteValue2.ToString("X"));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("0x" + configEntry->ByteValue3.ToString("X"));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->Alpha.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->HasValue.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(configEntry->IsOpen.ToString());
    }
}
