using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility.Table;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Sheets;
using ImGuiNET;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class OutfitsTab : DebugTab, ISubTab<UnlocksTab>, IDisposable
{
    private const float IconSize = 32;

    private readonly LanguageProvider _languageProvider;

    private readonly List<uint> _prismBoxItemIds = [];
    private bool _prismBoxBackedUp = false;
    private DateTime _prismBoxLastCheck = DateTime.MinValue;
    private readonly OutfitsTable _table;

    public OutfitsTab(LanguageProvider languageProvider, OutfitsTable table)
    {
        _languageProvider = languageProvider;

        _table = table;

        var tab = new Table<CustomMirageStoreSetItem>("", [], [new ColumnString<CustomMirageStoreSetItem>()]);

        _languageProvider.LanguageChanged += OnLanguageChanged;
    }

    public void Dispose()
    {
        _table.Dispose();
        _languageProvider.LanguageChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        _table.IsSortDirty = true;
    }

    public override string Title => "Outfits";

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

        var numCollectedSets = _table.Rows.Count(row => _prismBoxItemIds.Contains(row.RowId));
        ImGui.TextUnformatted($"{numCollectedSets} out of {_table.Rows.Count} filtered sets collected");

        _table.Draw();
    }
}
