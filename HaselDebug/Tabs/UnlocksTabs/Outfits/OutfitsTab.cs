using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class OutfitsTab(OutfitsTable table, PrismBoxProvider prismBoxProvider) : DebugTab, IUnlockTab
{
    private const float IconSize = 32;
    private readonly List<uint> _prismBoxItemIds = [];
    private bool _prismBoxBackedUp = false;
    private DateTime _prismBoxLastCheck = DateTime.MinValue;

    public override string Title => "Outfits";

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            NeedsExtraData = true,
            HasExtraData = MirageManager.Instance()->PrismBoxLoaded,
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(row => prismBoxProvider.ItemIds.Contains(row.RowId)),
        };
    }

    public override void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn)
        {
            ImGui.TextUnformatted("Not logged in.");

            // in case of logout
            if (_prismBoxBackedUp)
                _prismBoxBackedUp = false;

            return;
        }

        var mirageManager = MirageManager.Instance();
        if (!mirageManager->PrismBoxLoaded)
        {
            if (_prismBoxBackedUp)
            {
                using (Color.Yellow.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted("PrismBox not loaded. Using cache.");
            }
            else
            {
                using (Color.Red.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted("PrismBox not loaded.");
            }
        }
        else
        {
            var hasChanges = false;

            if (DateTime.Now - _prismBoxLastCheck > TimeSpan.FromSeconds(2))
            {
                hasChanges = !CollectionsMarshal.AsSpan(_prismBoxItemIds).SequenceEqual(mirageManager->PrismBoxItemIds);
                _prismBoxLastCheck = DateTime.Now;
            }

            if (!_prismBoxBackedUp || hasChanges)
            {
                _prismBoxItemIds.Clear();
                _prismBoxItemIds.AddRange(mirageManager->PrismBoxItemIds);
                _prismBoxBackedUp = true;
            }
        }

        var numCollectedSets = table.Rows.Count(row => _prismBoxItemIds.Contains(row.RowId));
        ImGui.TextUnformatted($"{numCollectedSets} out of {table.Rows.Count} filtered sets collected");

        table.Draw();
    }
}
