using System.Globalization;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AddonConfigTab : DebugTab
{
    private readonly ILogger<AddonConfigTab> _logger;
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;

    private readonly Dictionary<uint, string> _addonNames = [];
    private bool _isInitialized;

    private void Initialize()
    {
        void AddName(string name)
        {
            if (name.Length >= 32)
                return;

            _addonNames.TryAdd(UIGlobals.ComputeAddonNameHash(name), name);
        }

        foreach (ref var addon in HudLayoutAddon.GetSpan())
        {
            AddName(addon.AddonName);
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
        if (addonConfig == null || addonConfig->ActiveDataSet == null)
            return;

        using var tabbar = ImRaii.TabBar("AddonConfigTabBar");
        if (!tabbar)
            return;

        using (var tab = ImRaii.TabItem("Global Configs"))
        {
            if (tab)
            {
                DrawTable(addonConfig->ActiveDataSet->ConfigEntries);
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
                            DrawHudLayoutTable(addonConfig->ActiveDataSet->HudLayoutConfigEntries, i);
                        }
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("HudLayout Addons"))
        {
            if (tab)
            {
                DrawHudLayoutAddonsTab();
            }
        }

    }

    private void DrawTable(Span<AddonConfigEntry> configEntries)
    {
        using var table = ImRaii.Table("AddonConfigTable"u8, 14, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Addon"u8, ImGuiTableColumnFlags.WidthFixed, 200);
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
        using var table = ImRaii.Table("AddonConfigTable"u8, 14, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Addon"u8, ImGuiTableColumnFlags.WidthFixed, 200);
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
        ImGuiUtils.DrawCopyableText(i.ToString());

        ImGui.TableNextColumn();
        var hash = configEntry->AddonNameHash;
        ImGuiUtils.DrawCopyableText(_addonNames.TryGetValue(hash, out var name) ? name : hash.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->X.ToString("0.###", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->Y.ToString("0.###", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->Scale.ToString("0.0", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText("0x" + configEntry->ElementFlags.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->Width.ToString());

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->Height.ToString());

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText("0x" + configEntry->ByteValue1.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText("0x" + configEntry->ByteValue2.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText("0x" + configEntry->ByteValue3.ToString("X"));

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->Alpha.ToString());

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->HasValue.ToString());

        ImGui.TableNextColumn();
        ImGuiUtils.DrawCopyableText(configEntry->IsOpen.ToString());
    }

    public void DrawHudLayoutAddonsTab()
    {
        var span = HudLayoutAddon.GetSpan();

        using var table = ImRaii.Table("DrawHudLayoutAddonsTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Addon Name"u8);
        ImGui.TableSetupColumn("Addon Name Hash"u8, ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("Hud RowId and Display Name"u8);
        ImGui.TableSetupColumn("Flags"u8, ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < span.Length; i++)
        {
            var entry = span[i];

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGuiUtils.DrawCopyableText(i.ToString());

            ImGui.TableNextColumn(); // AddonName
            ImGuiUtils.DrawCopyableText(entry.AddonName.ToString());

            ImGui.TableNextColumn(); // Hash
            ImGuiUtils.DrawCopyableText($"0x{UIGlobals.ComputeAddonNameHash(entry.AddonName):X8}");

            ImGui.TableNextColumn(); // HudRowId

            if (_excelService.TryGetRow<Hud>(entry.HudRowId, out var hudRow))
                ImGuiUtils.DrawCopyableText($"[Hud#{entry.HudRowId}] {hudRow.Unknown0}");
            else
                ImGuiUtils.DrawCopyableText($"[Hud#{entry.HudRowId}]");

            ImGui.TableNextColumn(); // Flags
            ImGuiUtils.DrawCopyableText($"0x{entry.Flags:X2}");
        }
    }
}
