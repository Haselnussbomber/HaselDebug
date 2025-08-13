using System.Collections.Generic;
using System.Globalization;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using InteropGenerator.Runtime;
using InteropGenerator.Runtime.Attributes;
using Microsoft.Extensions.Logging;

namespace HaselDebug.Tabs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0)]
public unsafe partial struct AddonConfigFunctions
{
    [MemberFunction("E8 ?? ?? ?? ?? 33 ED 44 8B F8 85 C0 0F 84")]
    public static partial int GetNameCount();

    [MemberFunction("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 8B CF E8")]
    public static partial CStringPointer GetNameByIndex(uint index);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B 43 ?? 41 8B 88"), GenerateStringOverloads]
    public static partial uint GetNameHash(CStringPointer name);
}

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AddonConfigTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ILogger<AddonConfigTab> _logger;
    private readonly Dictionary<uint, string> _addonNames = [];
    private bool _isInitialized;

    private void Initialize()
    {
        void AddName(string name)
        {
            if (name.Length >= 32)
                return;

            _addonNames.TryAdd(AddonConfigFunctions.GetNameHash(name), name);
        }

        for (var i = 0u; i < AddonConfigFunctions.GetNameCount(); i++)
        {
            AddName(AddonConfigFunctions.GetNameByIndex(i));
        }

        foreach (var addonName in RaptureAtkModule.Instance()->AddonNames)
        {
            var addonNameString = addonName.StringPtr.ToString();
            AddName(addonNameString);

            for (var i = 0u; i < 10; i++)
            {
                AddName($"{addonNameString}{i}");
                AddName($"{addonNameString}{i:00}");
            }
        }
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

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
        ImGuiUtilsEx.DrawCopyableText(i.ToString());

        ImGui.TableNextColumn();
        var hash = configEntry->AddonNameHash;
        ImGuiUtilsEx.DrawCopyableText(_addonNames.TryGetValue(hash, out var name) ? name : hash.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->X.ToString("0.###", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->Y.ToString("0.###", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->Scale.ToString("0.0", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText("0x" + configEntry->ElementFlags.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->Width.ToString());

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->Height.ToString());

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText("0x" + configEntry->ByteValue1.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText("0x" + configEntry->ByteValue2.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText("0x" + configEntry->ByteValue3.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->Alpha.ToString());

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->HasValue.ToString());

        ImGui.TableNextColumn();
        ImGuiUtilsEx.DrawCopyableText(configEntry->IsOpen.ToString());
    }
}
