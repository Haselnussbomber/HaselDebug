/*
using System;
using System.Collections.Generic;
using System.Linq;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using Dalamud;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public unsafe class PlayerStateTab : IDebugWindowTab
{
    public string Title => "PlayerState";

    public unsafe void Draw()
    {
        var sheetLanguage = ClientLanguage.English;

        var ItemSheet = GetSheet<Item>(sheetLanguage)!;
        var PlaceNameSheet = GetSheet<PlaceName>(sheetLanguage)!;

        var playerState = PlayerState.Instance();
        using var tabs = ImRaii.TabBar("PlayerStateTabs");
        using (var tab = ImRaii.TabItem("PlayerState"))
        {
            if (tab)
            {
                ImGui.TextUnformatted($"OwnedMountsBitmask size: {(GetSheet<Mount>(sheetLanguage)!.Max(row => row.Order) + 7) >> 3}");
                ImGui.TextUnformatted($"UnlockedOrnamentsBitmask size: {(GetSheet<Ornament>(sheetLanguage)!.Count() + 7) >> 3}");
                ImGui.TextUnformatted($"CaughtFishBitmask size: {(GetSheet<FishParameter>(sheetLanguage)!.Count(row => row.IsInLog) + 7) >> 3}");
                ImGui.TextUnformatted($"CaughtSpearfishBitmask size: {(GetSheet<SpearfishingItem>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.TextUnformatted($"UnlockedSpearfishingNotebookBitmask size: {(GetSheet<SpearfishingNotebook>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.TextUnformatted($"UnlockedAdventureBitmask size: {(GetSheet<Adventure>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.TextUnformatted($"UnlockedAdventureExPhaseBitmask size: {(GetSheet<AdventureExPhase>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.TextUnformatted($"UnlockedVVDRouteDataBitmask2 size: {(GetSheet<VVDRouteData>(sheetLanguage)!.RowCount + 7) >> 3}");

                ImGui.Separator();

                var playerstateflags = new List<string>();
                foreach (PlayerStateFlag flag in Enum.GetValues(typeof(PlayerStateFlag)))
                {
                    if (playerState->IsPlayerStateFlagSet(flag))
                        playerstateflags.Add(Enum.GetName(typeof(PlayerStateFlag), flag) ?? $"{flag}");
                }
                ImGui.TextUnformatted($"PlayerStateFlags: {string.Join(", ", playerstateflags)}");

                ImGui.Separator();

                Debug.DrawPointerType((nint)playerState, typeof(PlayerState));
            }
        }

        using (var tab = ImRaii.TabItem("VVD"))
        {
            if (tab)
            {
                var VVDRouteDataSheet = GetSheet<VVDRouteData>(sheetLanguage)!.Where(row => row.Unknown0 != 0);
                var VVDNotebookSeriesSheet = GetSheet<VVDNotebookSeries>(sheetLanguage)!;
                var VVDNotebookContentsSheet = GetSheet<VVDNotebookContents>(sheetLanguage)!;

                using var table = ImRaii.Table("VVDRouteData", 5);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Series", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Record", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Number", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("UnlockedFn", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var vvdRouteDataSheet in VVDRouteDataSheet)
                {
                    var series = VVDNotebookSeriesSheet.GetRow(vvdRouteDataSheet.RowId)!;
                    var contents = VVDNotebookContentsSheet.GetRow(series.Contents[vvdRouteDataSheet.SubRowId].Row)!;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{vvdRouteDataSheet.RowId}-{vvdRouteDataSheet.SubRowId}");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(series.Name);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(contents.Name);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(vvdRouteDataSheet.Unknown0.ToString());

                    var unlocked = playerState->IsVVDRouteComplete(vvdRouteDataSheet.Unknown0 - 1u);

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.TextUnformatted(unlocked ? "Yes" : "No");
                }
            }
        }

        using (var tab = ImRaii.TabItem("Mounts"))
        {
            if (tab)
            {
                ImGui.TextUnformatted($"{playerState->NumOwnedMounts} owned mounts");

                var MountSheet = GetSheet<Mount>(sheetLanguage)!;
                ImGui.TextUnformatted($"Num Mounts: {MountSheet.Count()}");
                ImGui.TextUnformatted($"Nax MountSheet.Order: {MountSheet.Max(row => row.Order)}");

                using var table = ImRaii.Table("OwnedMounts", 3);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("UnlockedFn", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var mount in MountSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mount.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mount.Singular);

                    if (mount.Order != 0)
                    {
                        var unlocked = playerState->IsMountUnlocked(mount.RowId);

                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                            ImGui.TextUnformatted(unlocked ? "Yes" : "No");
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("CaughtFish"))
        {
            if (tab)
            {
                ImGui.TextUnformatted($"{playerState->NumFishCaught} caught fish");

                var FishParameterSheet = GetSheet<FishParameter>(sheetLanguage)!.Where(row => row.IsInLog);
                ImGui.TextUnformatted($"Num FishParameters (IsInLog = true): {FishParameterSheet.Count()}");

                using var table = ImRaii.Table("CaughtFishTable", 4);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Caught", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var fishParameter in FishParameterSheet)
                {
                    if (fishParameter.Item == 0)
                        continue;

                    var item = ItemSheet.GetRow((uint)fishParameter.Item);
                    if (item == null)
                        continue;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(fishParameter.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(item.Name);

                    if (fishParameter.IsInLog)
                    {
                        var fishId = fishParameter.RowId;
                        var offset = fishId / 8;
                        var bit = (byte)fishId % 8;
                        var caught = ((playerState->CaughtFishBitmask[offset] >> bit) & 1) == 1;

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(offset.ToString());

                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, caught ? 0xFF00FF00 : 0xFF0000FF))
                            ImGui.TextUnformatted(caught ? "Yes" : "No");
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("CaughtSpearFish"))
        {
            if (tab)
            {
                ImGui.TextUnformatted($"{playerState->NumSpearfishCaught} caught spearfish");

                var spearfishingItemSheet = GetSheet<SpearfishingItem>(sheetLanguage)!;
                ImGui.TextUnformatted($"Num SpearfishingItems: {spearfishingItemSheet.RowCount}");

                using var table = ImRaii.Table("CaughtSpearFishTable", 4);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Caught", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var spearfishingItem in spearfishingItemSheet)
                {
                    if (spearfishingItem.RowId < 20000 || spearfishingItem.Item.Row == 0)
                        continue;

                    var item = ItemSheet.GetRow(spearfishingItem.Item.Row);
                    if (item == null)
                        continue;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(spearfishingItem.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(item.Name);

                    if (spearfishingItem.RowId < 30000)
                    {
                        var spearFishId = spearfishingItem.RowId - 20000;
                        var offset = spearFishId / 8;
                        var bit = (byte)spearFishId % 8;
                        var caught = ((playerState->CaughtSpearfishBitmask[offset] >> bit) & 1) == 1;

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(offset.ToString());

                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, caught ? 0xFF00FF00 : 0xFF0000FF))
                            ImGui.TextUnformatted(caught ? "Yes" : "No");
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("SpearfishingNotebook"))
        {
            if (tab)
            {
                var spearfishingNotebookSheet = GetSheet<SpearfishingNotebook>(sheetLanguage)!;
                ImGui.TextUnformatted($"Num SpearfishingNotebooks: {spearfishingNotebookSheet.RowCount}");

                using var table = ImRaii.Table("SpearfishingNotebookTable", 5);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Territory", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("PlaceName", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var spearfishingNotebook in spearfishingNotebookSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(spearfishingNotebook.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(PlaceNameSheet.GetRow(spearfishingNotebook.TerritoryType.Value?.PlaceName.Row ?? 0)?.Name ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(PlaceNameSheet.GetRow(spearfishingNotebook.PlaceName.Row)?.Name ?? "");

                    var id = spearfishingNotebook.RowId;
                    var offset = id / 8;
                    var bit = (byte)id % 8;
                    var unlocked = ((playerState->UnlockedSpearfishingNotebookBitmask[offset] >> bit) & 1) == 1;

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(offset.ToString());

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.TextUnformatted(unlocked ? "Yes" : "No");
                }
            }
        }

        using (var tab = ImRaii.TabItem("Adventure"))
        {
            if (tab)
            {
                var itemSheet = GetSheet<Item>(sheetLanguage)!;
                var adventureSheet = GetSheet<Adventure>(sheetLanguage)!;
                ImGui.TextUnformatted($"Num Adventures: {adventureSheet.RowCount}");

                using var table = ImRaii.Table("AdventureTable", 4);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("UnlockedFn", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var adventure in adventureSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(adventure.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(adventure.Name);

                    var id = adventure.RowId - 0x210000;
                    var unlocked = playerState->IsAdventureComplete(id);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted((id / 8).ToString());

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.TextUnformatted(unlocked ? "Yes" : "No");
                }
            }
        }

        using (var tab = ImRaii.TabItem("AdventureExPhase"))
        {
            if (tab)
            {
                var itemSheet = GetSheet<Item>(sheetLanguage)!;
                var adventureExPhaseSheet = GetSheet<AdventureExPhase>(sheetLanguage)!;
                ImGui.TextUnformatted($"Num AdventureExPhases: {adventureExPhaseSheet.RowCount}");

                using var table = ImRaii.Table("AdventureExPhaseTable", 5);

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Expansion", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("UnlockedFn", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var adventureExPhase in adventureExPhaseSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(adventureExPhase.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(adventureExPhase.Expansion.Value?.Name ?? "");

                    var id = adventureExPhase.RowId;
                    var offset = id / 8;
                    var bit = (byte)id % 8;
                    var unlocked = ((playerState->UnlockedAdventureExPhaseBitmask[offset] >> bit) & 1) == 1;

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(offset.ToString());

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.TextUnformatted(unlocked ? "Yes" : "No");

                    ImGui.TableNextColumn();
                    unlocked = playerState->IsAdventureExPhaseComplete(adventureExPhase.RowId);
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.TextUnformatted(unlocked ? "Yes" : "No");
                }
            }
        }
    }
}
*/
