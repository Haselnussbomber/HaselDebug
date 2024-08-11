using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    private (uint Id, HashSet<(string, uint, uint, string)> Unlocks)[] UnlockLinks = [];

    public void DrawUnlockLinks()
    {
        using var tab = ImRaii.TabItem("Unlock Links");
        if (!tab) return;

        using var table = ImRaii.Table("UnlockLinksTable", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Unlocks", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var uiState = UIState.Instance();
        foreach (var (id, entries) in UnlockLinks)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(id.ToString());

            ImGui.TableNextColumn(); // Unlocked
            var isUnlocked = UIState.Instance()->IsUnlockLinkUnlocked(id);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Colors.Green : Colors.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Unlocks

            using var innertable = ImRaii.Table($"InnerTable{id}", 2, ImGuiTableFlags.NoSavedSettings, new Vector2(400, -1));
            if (!innertable) return;

            ImGui.TableSetupColumn("Sheet", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);

            foreach (var (sheetName, rowId, iconId, name) in entries)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Sheet
                ImGui.TextUnformatted($"{sheetName}#{rowId}");

                ImGui.TableNextColumn(); // Name
                DebugRenderer.DrawIcon(iconId);
                ImGui.TextUnformatted(name);
            }
        }
    }

    private void UpdateUnlockLinks()
    {
        var dict = new Dictionary<uint, HashSet<(string, uint, uint, string)>>();

        foreach (var row in ExcelService.GetSheet<GeneralAction>())
        {
            if (row.UnlockLink > 0)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(("GeneralAction", row.RowId, (uint)row.Icon, row.Name));
            }
        }

        foreach (var row in ExcelService.GetSheet<Lumina.Excel.GeneratedSheets.Action>())
        {
            if (row.UnlockLink is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(("Action", row.RowId, row.Icon, row.Name));
            }
        }

        foreach (var row in ExcelService.GetSheet<Emote>())
        {
            if (row.UnlockLink is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(("Emote", row.RowId, row.Icon, row.Name));
            }
        }

        UnlockLinks = dict
            .OrderBy(kv => kv.Key)
            .Select(kv => (Id: kv.Key, Unlocks: kv.Value))
            .ToArray();
    }
}
