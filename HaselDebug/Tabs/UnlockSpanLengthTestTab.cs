using System.Linq;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using Lumina.Excel.Sheets;
using ContentRouletteSheet = Lumina.Excel.Sheets.ContentRoulette;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class UnlockSpanLengthTestTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly ISigScanner _sigScanner;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("UnlockSpanLengthTestTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table) return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("Length Has", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Length Should", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        static void AddRow(string name, int lengthHas, int lengthShould)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(name);

            ImGui.TableNextColumn();
            ImGui.Text(lengthHas.ToString());

            ImGui.TableNextColumn();
            ImGui.Text(lengthShould.ToString());

            ImGui.TableNextColumn();
            var match = lengthHas == lengthShould;
            using var colorOk = Color.Green.Push(ImGuiCol.Text, match);
            using var colorMismatch = Color.Red.Push(ImGuiCol.Text, !match);
            ImGui.Text(match ? "OK" : "MISMATCH");
        }

        var playerState = PlayerState.Instance();
        var uiState = UIState.Instance();

        AddRow("PlayerState.UnlockedMountsBitmask", playerState->UnlockedMountsBitmask.Length, (_excelService.GetSheet<Mount>().Where(row => row.ModelChara.RowId != 0).Max(row => row.Order) + 7) >> 3);
        AddRow("PlayerState.UnlockedOrnamentsBitmask", playerState->UnlockedOrnamentsBitmask.Length, (_excelService.GetRowCount<Ornament>() + 7) >> 3);
        AddRow("PlayerState.UnlockedGlassesStylesBitmask", playerState->UnlockedGlassesStylesBitmask.Length, (_excelService.GetRowCount<GlassesStyle>() + 7) >> 3);
        AddRow("PlayerState.UnlockedFishingSpotBitmask", playerState->UnlockedFishingSpotBitmask.Length, (_excelService.GetSheet<FishingSpot>().Max(row => row.Order) + 7) >> 3);
        AddRow("PlayerState.CaughtFishBitmask", playerState->CaughtFishBitmask.Length, (_excelService.GetSheet<FishParameter>().Count(row => row.IsInLog) + 7) >> 3);
        AddRow("PlayerState.UnlockedSpearfishingNotebookBitmask", playerState->UnlockedSpearfishingNotebookBitmask.Length, (_excelService.GetRowCount<SpearfishingNotebook>() + 7) >> 3);
        AddRow("PlayerState.CaughtSpearfishBitmask", playerState->CaughtSpearfishBitmask.Length, (int)(_excelService.GetSheet<SpearfishingItem>().Where(row => row.RowId < 30000).Max(row => row.RowId) - 20000 + 7) >> 3);
        AddRow("PlayerState.ContentRouletteCompletion", playerState->ContentRouletteCompletion.Length, _excelService.GetSheet<ContentRouletteSheet>().Max(row => row.Unknown17)); // Not a bit array. TODO: CompletionArrayIndex
        AddRow("PlayerState.UnlockedSecretRecipeBooksBitmask", playerState->UnlockedSecretRecipeBooksBitmask.Length, (_excelService.GetRowCount<SecretRecipeBook>() + 7) >> 3);
        AddRow("PlayerState.CompletedAdventureBitmask", playerState->CompletedAdventureBitmask.Length, (_excelService.GetRowCount<Adventure>() + 7) >> 3);
        AddRow("PlayerState.UnlockedAetherCurrentsBitmask", playerState->UnlockedAetherCurrentsBitmask.Length, (_excelService.GetRowCount<AetherCurrent>() + 7) >> 3);
        AddRow("PlayerState.UnlockedAetherCurrentCompFlgSetBitmask", playerState->UnlockedAetherCurrentCompFlgSetBitmask.Length, _excelService.GetRowCount<AetherCurrentCompFlgSet>() >> 3);
        AddRow("PlayerState.UnlockedMinerFolkloreTomeBitmask", playerState->UnlockedMinerFolkloreTomeBitmask.Length, (int)(_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 16 && row.Quest.RowId < 74).Max(row => row.Quest.RowId) + 7) >> 3);
        AddRow("PlayerState.UnlockedBotanistFolkloreTomeBitmask", playerState->UnlockedBotanistFolkloreTomeBitmask.Length, (int)(_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 17 && row.Quest.RowId < 74).Max(row => row.Quest.RowId) + 7) >> 3);
        AddRow("PlayerState.UnlockedFishingFolkloreTomeBitmask", playerState->UnlockedFishingFolkloreTomeBitmask.Length, (int)(_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 18 && row.Quest.RowId < 74).Max(row => row.Quest.RowId) + 7) >> 3);
        AddRow("PlayerState.UnlockedOrchestrionRollBitmask", playerState->UnlockedOrchestrionRollBitmask.Length, (_excelService.GetRowCount<Orchestrion>() + 7) >> 3);
        // AddRow("PlayerState.UnlockedFramersKitsBitmask", playerState->UnlockedFramersKitsBitmask.Length, unknown);
        AddRow("UIState.UnlockedAetherytesBitmask", uiState->UnlockedAetherytesBitmask.Length, (_excelService.GetRowCount<Aetheryte>() + 7) >> 3);
        AddRow("UIState.UnlockedHowtoBitmask", uiState->UnlockedHowtoBitmask.Length, (_excelService.GetRowCount<HowTo>() + 7) >> 3);
        AddRow("UIState.UnlockedCompanionsBitmask", uiState->UnlockedCompanionsBitmask.Length, (_excelService.GetRowCount<Companion>() + 7) >> 3);
        AddRow("UIState.ChocoboTaxiStandsBitmask", uiState->UnlockedChocoboTaxiStandsBitmask.Length, (_excelService.GetRowCount<ChocoboTaxiStand>() + 7) >> 3);
        AddRow("UIState.CutsceneSeenBitmask", uiState->CutsceneSeenBitmask.Length, (_excelService.GetSheet<CutsceneWorkIndex>().Max(row => row.WorkIndex) + 7) >> 3);
        AddRow("UIState.UnlockedTripleTriadCardsBitmask", uiState->UnlockedTripleTriadCardsBitmask.Length, (_excelService.GetRowCount<TripleTriadCard>() + 7) >> 3);
    }
}
