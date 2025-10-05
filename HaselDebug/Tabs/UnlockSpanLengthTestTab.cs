using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class UnlockSpanLengthTestTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly ISigScanner _sigScanner;
    private readonly List<BitfieldRecord> _bitfields = [];
    private bool _initialized;

    public override bool DrawInChild => false;

    public record struct BitfieldRecord(string Name, int LengthHas, int NumEntries)
    {
        public int LengthShould = (NumEntries + 7) / 8;
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        using var table = ImRaii.Table("UnlockSpanLengthTestTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table) return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("Length Has", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Length Should", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        foreach (var entry in _bitfields)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.Name);

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.LengthHas.ToString());

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.LengthShould.ToString());

            ImGui.TableNextColumn();
            var match = entry.LengthHas == entry.LengthShould;
            using var colorOk = Color.Green.Push(ImGuiCol.Text, match);
            using var colorMismatch = Color.Red.Push(ImGuiCol.Text, !match);
            ImGui.Text(match ? "OK" : "MISMATCH");
        }
    }

    private void Initialize()
    {
        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedMountsBitmask",
            PlayerState.Instance()->UnlockedMountsBitmask.Length,
            _excelService.GetSheet<Mount>().Where(row => row.ModelChara.RowId != 0).Max(row => row.Order)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedMountsBitmask NEW",
            PlayerState.Instance()->UnlockedMountsBitmask.Length,
            _excelService.GetSheet<Mount>().Where(row => row.ModelChara.RowId != 0).Max(row => row.Order)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedOrnamentsBitmask",
            PlayerState.Instance()->UnlockedOrnamentsBitmask.Length,
            _excelService.GetRowCount<Ornament>()));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedGlassesStylesBitmask",
            PlayerState.Instance()->UnlockedGlassesStylesBitmask.Length,
            _excelService.GetRowCount<GlassesStyle>()));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedFishingSpotBitmask",
            PlayerState.Instance()->UnlockedFishingSpotBitmask.Length,
            _excelService.GetSheet<FishingSpot>().Max(row => row.Order)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.CaughtFishBitmask",
            PlayerState.Instance()->CaughtFishBitmask.Length,
            _excelService.GetSheet<FishParameter>().Count(row => row.IsInLog)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedSpearfishingNotebookBitmask",
            PlayerState.Instance()->UnlockedSpearfishingNotebookBitmask.Length,
            _excelService.GetRowCount<SpearfishingNotebook>()));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.CaughtSpearfishBitmask",
            PlayerState.Instance()->CaughtSpearfishBitmask.Length,
            (int)(_excelService.GetSheet<SpearfishingItem>().Where(row => row.RowId < 30000).Max(row => row.RowId) - 20000)));

        // _bitfields.Add(new BitfieldRecord(
        //     "PlayerState.ContentRouletteCompletion",
        //     PlayerState.Instance()->ContentRouletteCompletion.Length,
        //     _excelService.GetSheet<ContentRouletteSheet>().Max(row => row.Unknown17))); // Not a bit array. TODO: CompletionArrayIndex

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedSecretRecipeBooksBitmask",
            PlayerState.Instance()->UnlockedSecretRecipeBooksBitmask.Length,
            _excelService.GetRowCount<SecretRecipeBook>()));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.CompletedAdventureBitmask",
            PlayerState.Instance()->CompletedAdventureBitmask.Length,
            _excelService.GetRowCount<Adventure>()));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedAetherCurrentsBitmask",
            PlayerState.Instance()->UnlockedAetherCurrentsBitmask.Length,
            _excelService.GetRowCount<AetherCurrent>()));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedAetherCurrentCompFlgSetBitmask",
            PlayerState.Instance()->UnlockedAetherCurrentCompFlgSetBitmask.Length,
            _excelService.GetSheet<AetherCurrentCompFlgSet>().Count(row => row.Territory.IsValid)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedMinerFolkloreTomeBitmask",
            PlayerState.Instance()->UnlockedMinerFolkloreTomeBitmask.Length,
            (int)_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 16 && row.Quest.RowId < 74).Max(row => row.Quest.RowId)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedBotanistFolkloreTomeBitmask",
            PlayerState.Instance()->UnlockedBotanistFolkloreTomeBitmask.Length,
            (int)_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 17 && row.Quest.RowId < 74).Max(row => row.Quest.RowId)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedFishingFolkloreTomeBitmask",
            PlayerState.Instance()->UnlockedFishingFolkloreTomeBitmask.Length,
            (int)_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 18 && row.Quest.RowId < 74).Max(row => row.Quest.RowId)));

        _bitfields.Add(new BitfieldRecord(
            "PlayerState.UnlockedOrchestrionRollBitmask",
            PlayerState.Instance()->UnlockedOrchestrionRollBitmask.Length,
            _excelService.GetRowCount<Orchestrion>()));

        // _bitfields.Add(new BitfieldRecord(
        //    "PlayerState.UnlockedFramersKitsBitmask",
        //    PlayerState->UnlockedFramersKitsBitmask.Length,
        //    unknown));

        _bitfields.Add(new BitfieldRecord(
            "UIState.UnlockedAetherytesBitmask", UIState.Instance()->UnlockedAetherytesBitmask.Length,
            _excelService.GetRowCount<Aetheryte>()));

        _bitfields.Add(new BitfieldRecord(
            "UIState.UnlockedHowtoBitmask", UIState.Instance()->UnlockedHowtoBitmask.Length,
            _excelService.GetRowCount<HowTo>()));

        _bitfields.Add(new BitfieldRecord(
            "UIState.UnlockedCompanionsBitmask", UIState.Instance()->UnlockedCompanionsBitmask.Length,
            _excelService.GetRowCount<Companion>()));

        _bitfields.Add(new BitfieldRecord(
            "UIState.ChocoboTaxiStandsBitmask", UIState.Instance()->UnlockedChocoboTaxiStandsBitmask.Length,
            _excelService.GetRowCount<ChocoboTaxiStand>()));

        _bitfields.Add(new BitfieldRecord(
            "UIState.CutsceneSeenBitmask", UIState.Instance()->CutsceneSeenBitmask.Length,
            _excelService.GetSheet<CutsceneWorkIndex>().Max(row => row.WorkIndex)));

        _bitfields.Add(new BitfieldRecord(
            "UIState.UnlockedTripleTriadCardsBitmask", UIState.Instance()->UnlockedTripleTriadCardsBitmask.Length,
            _excelService.GetRowCount<TripleTriadCard>()));
    }
}
