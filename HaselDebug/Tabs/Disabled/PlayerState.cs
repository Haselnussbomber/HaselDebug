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
                ImGui.Text($"OwnedMountsBitmask size: {(GetSheet<Mount>(sheetLanguage)!.Max(row => row.Order) + 7) >> 3}");
                ImGui.Text($"UnlockedOrnamentsBitmask size: {(GetSheet<Ornament>(sheetLanguage)!.Count() + 7) >> 3}");
                ImGui.Text($"CaughtFishBitmask size: {(GetSheet<FishParameter>(sheetLanguage)!.Count(row => row.IsInLog) + 7) >> 3}");
                ImGui.Text($"CaughtSpearfishBitmask size: {(GetSheet<SpearfishingItem>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.Text($"UnlockedSpearfishingNotebookBitmask size: {(GetSheet<SpearfishingNotebook>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.Text($"UnlockedAdventureBitmask size: {(GetSheet<Adventure>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.Text($"UnlockedAdventureExPhaseBitmask size: {(GetSheet<AdventureExPhase>(sheetLanguage)!.RowCount + 7) >> 3}");
                ImGui.Text($"UnlockedVVDRouteDataBitmask2 size: {(GetSheet<VVDRouteData>(sheetLanguage)!.RowCount + 7) >> 3}");

                ImGui.Separator();

                var playerstateflags = new List<string>();
                foreach (PlayerStateFlag flag in Enum.GetValues(typeof(PlayerStateFlag)))
                {
                    if (playerState->IsPlayerStateFlagSet(flag))
                        playerstateflags.Add(Enum.GetName(typeof(PlayerStateFlag), flag) ?? $"{flag}");
                }
                ImGui.Text($"PlayerStateFlags: {string.Join(", ", playerstateflags)}");

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

                using var table = ImRaii.Table("VVDRouteData"u8, 5);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Series"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Record"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Number"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("UnlockedFn"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var vvdRouteDataSheet in VVDRouteDataSheet)
                {
                    var series = VVDNotebookSeriesSheet.GetRow(vvdRouteDataSheet.RowId)!;
                    var contents = VVDNotebookContentsSheet.GetRow(series.Contents[vvdRouteDataSheet.SubRowId].Row)!;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{vvdRouteDataSheet.RowId}-{vvdRouteDataSheet.SubRowId}");

                    ImGui.TableNextColumn();
                    ImGui.Text(series.Name);

                    ImGui.TableNextColumn();
                    ImGui.Text(contents.Name);

                    ImGui.TableNextColumn();
                    ImGui.Text(vvdRouteDataSheet.Unknown0.ToString());

                    var unlocked = playerState->IsVVDRouteComplete(vvdRouteDataSheet.Unknown0 - 1u);

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.Text(unlocked ? "Yes" : "No");
                }
            }
        }

        using (var tab = ImRaii.TabItem("Mounts"))
        {
            if (tab)
            {
                ImGui.Text($"{playerState->NumOwnedMounts} owned mounts");

                var MountSheet = GetSheet<Mount>(sheetLanguage)!;
                ImGui.Text($"Num Mounts: {MountSheet.Count()}");
                ImGui.Text($"Nax MountSheet.Order: {MountSheet.Max(row => row.Order)}");

                using var table = ImRaii.Table("OwnedMounts"u8, 3);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("UnlockedFn"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var mount in MountSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(mount.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(mount.Singular);

                    if (mount.Order != 0)
                    {
                        var unlocked = playerState->IsMountUnlocked(mount.RowId);

                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                            ImGui.Text(unlocked ? "Yes" : "No");
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("CaughtFish"))
        {
            if (tab)
            {
                ImGui.Text($"{playerState->NumFishCaught} caught fish");

                var FishParameterSheet = GetSheet<FishParameter>(sheetLanguage)!.Where(row => row.IsInLog);
                ImGui.Text($"Num FishParameters (IsInLog = true): {FishParameterSheet.Count()}");

                using var table = ImRaii.Table("CaughtFishTable"u8, 4);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Caught"u8, ImGuiTableColumnFlags.WidthFixed, 75);
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
                    ImGui.Text(fishParameter.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(item.Name);

                    if (fishParameter.IsInLog)
                    {
                        var fishId = fishParameter.RowId;
                        var offset = fishId / 8;
                        var bit = (byte)fishId % 8;
                        var caught = ((playerState->CaughtFishBitmask[offset] >> bit) & 1) == 1;

                        ImGui.TableNextColumn();
                        ImGui.Text(offset.ToString());

                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, caught ? 0xFF00FF00 : 0xFF0000FF))
                            ImGui.Text(caught ? "Yes" : "No");
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("CaughtSpearFish"))
        {
            if (tab)
            {
                ImGui.Text($"{playerState->NumSpearfishCaught} caught spearfish");

                var spearfishingItemSheet = GetSheet<SpearfishingItem>(sheetLanguage)!;
                ImGui.Text($"Num SpearfishingItems: {spearfishingItemSheet.RowCount}");

                using var table = ImRaii.Table("CaughtSpearFishTable"u8, 4);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Caught"u8, ImGuiTableColumnFlags.WidthFixed, 75);
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
                    ImGui.Text(spearfishingItem.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(item.Name);

                    if (spearfishingItem.RowId < 30000)
                    {
                        var spearFishId = spearfishingItem.RowId - 20000;
                        var offset = spearFishId / 8;
                        var bit = (byte)spearFishId % 8;
                        var caught = ((playerState->CaughtSpearfishBitmask[offset] >> bit) & 1) == 1;

                        ImGui.TableNextColumn();
                        ImGui.Text(offset.ToString());

                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, caught ? 0xFF00FF00 : 0xFF0000FF))
                            ImGui.Text(caught ? "Yes" : "No");
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("SpearfishingNotebook"))
        {
            if (tab)
            {
                var spearfishingNotebookSheet = GetSheet<SpearfishingNotebook>(sheetLanguage)!;
                ImGui.Text($"Num SpearfishingNotebooks: {spearfishingNotebookSheet.RowCount}");

                using var table = ImRaii.Table("SpearfishingNotebookTable"u8, 5);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Territory"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("PlaceName"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Unlocked"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var spearfishingNotebook in spearfishingNotebookSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(spearfishingNotebook.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(PlaceNameSheet.GetRow(spearfishingNotebook.TerritoryType.Value?.PlaceName.Row ?? 0)?.Name ?? "");

                    ImGui.TableNextColumn();
                    ImGui.Text(PlaceNameSheet.GetRow(spearfishingNotebook.PlaceName.Row)?.Name ?? "");

                    var id = spearfishingNotebook.RowId;
                    var offset = id / 8;
                    var bit = (byte)id % 8;
                    var unlocked = ((playerState->UnlockedSpearfishingNotebookBitmask[offset] >> bit) & 1) == 1;

                    ImGui.TableNextColumn();
                    ImGui.Text(offset.ToString());

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.Text(unlocked ? "Yes" : "No");
                }
            }
        }

        using (var tab = ImRaii.TabItem("Adventure"))
        {
            if (tab)
            {
                var itemSheet = GetSheet<Item>(sheetLanguage)!;
                var adventureSheet = GetSheet<Adventure>(sheetLanguage)!;
                ImGui.Text($"Num Adventures: {adventureSheet.RowCount}");

                using var table = ImRaii.Table("AdventureTable"u8, 4);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("UnlockedFn"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var adventure in adventureSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(adventure.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(adventure.Name);

                    var id = adventure.RowId - 0x210000;
                    var unlocked = playerState->IsAdventureComplete(id);

                    ImGui.TableNextColumn();
                    ImGui.Text((id / 8).ToString());

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.Text(unlocked ? "Yes" : "No");
                }
            }
        }

        using (var tab = ImRaii.TabItem("AdventureExPhase"))
        {
            if (tab)
            {
                var itemSheet = GetSheet<Item>(sheetLanguage)!;
                var adventureExPhaseSheet = GetSheet<AdventureExPhase>(sheetLanguage)!;
                ImGui.Text($"Num AdventureExPhases: {adventureExPhaseSheet.RowCount}");

                using var table = ImRaii.Table("AdventureExPhaseTable"u8, 5);

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Expansion"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Offset"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Unlocked"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("UnlockedFn"u8, ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();

                foreach (var adventureExPhase in adventureExPhaseSheet)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(adventureExPhase.RowId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(adventureExPhase.Expansion.Value?.Name ?? "");

                    var id = adventureExPhase.RowId;
                    var offset = id / 8;
                    var bit = (byte)id % 8;
                    var unlocked = ((playerState->UnlockedAdventureExPhaseBitmask[offset] >> bit) & 1) == 1;

                    ImGui.TableNextColumn();
                    ImGui.Text(offset.ToString());

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.Text(unlocked ? "Yes" : "No");

                    ImGui.TableNextColumn();
                    unlocked = playerState->IsAdventureExPhaseComplete(adventureExPhase.RowId);
                    using (ImRaii.PushColor(ImGuiCol.Text, unlocked ? 0xFF00FF00 : 0xFF0000FF))
                        ImGui.Text(unlocked ? "Yes" : "No");
                }
            }
        }
    }
}
*/
