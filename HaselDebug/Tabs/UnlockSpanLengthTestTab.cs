using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class UnlockSpanLengthTestTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly ISigScanner _sigScanner;
    private readonly List<BitArrayRecord> _bitArrays = [];
    private bool _initialized;

    public override bool DrawInChild => false;

    public record struct BitArrayRecord(string Name, BitArray BitArray, int BitCount)
    {
        public int LengthShould = (BitCount + 7) / 8;
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        using var table = ImRaii.Table("UnlockSpanLengthTestTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table) return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("Length Has", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Length Should", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Bit Count Has", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Bit Count Should", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("PopCount", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        foreach (var entry in _bitArrays)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.Name);

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.BitArray.ByteLength.ToString());

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.LengthShould.ToString());

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.BitArray.BitCount.ToString());

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.BitCount.ToString());

            ImGui.TableNextColumn();
            ImGuiUtilsEx.DrawCopyableText(entry.BitArray.PopCount.ToString());

            ImGui.TableNextColumn();
            var match = entry.BitArray.ByteLength == entry.LengthShould && entry.BitArray.BitCount == entry.BitCount;
            using var colorOk = Color.Green.Push(ImGuiCol.Text, match);
            using var colorMismatch = Color.Red.Push(ImGuiCol.Text, !match);
            ImGui.Text(match ? "OK" : "MISMATCH");
        }
    }

    private void Initialize()
    {
        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedMounts",
            PlayerState.Instance()->UnlockedMountsBitArray,
            _excelService.GetSheet<Mount>().Where(row => row.ModelChara.RowId != 0).Max(row => row.Order)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedOrnaments",
            PlayerState.Instance()->UnlockedOrnamentsBitArray,
            _excelService.GetRowCount<Ornament>()));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedGlassesStyles",
            PlayerState.Instance()->UnlockedGlassesStylesBitArray,
            _excelService.GetRowCount<GlassesStyle>()));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedFishingSpots",
            PlayerState.Instance()->UnlockedFishingSpotsBitArray,
            _excelService.GetSheet<FishingSpot>().Max(row => row.Order)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.CaughtFish",
            PlayerState.Instance()->CaughtFishBitArray,
            (int)_excelService.GetSheet<FishParameter>().Last(row => row.IsInLog).RowId));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedSpearfishingNotebooks",
            PlayerState.Instance()->UnlockedSpearfishingNotebooksBitArray,
            _excelService.GetRowCount<SpearfishingNotebook>()));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.CaughtSpearfish",
            PlayerState.Instance()->CaughtSpearfishBitArray,
            (int)(_excelService.GetSheet<SpearfishingItem>().Where(row => row.RowId < 30000).Max(row => row.RowId) - 20000)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedSecretRecipeBooks",
            PlayerState.Instance()->UnlockedSecretRecipeBooksBitArray,
            _excelService.GetRowCount<SecretRecipeBook>()));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.CompletedAdventures",
            PlayerState.Instance()->CompletedAdventuresBitArray,
            _excelService.GetRowCount<Adventure>()));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedAetherCurrents",
            PlayerState.Instance()->UnlockedAetherCurrentsBitArray,
            _excelService.GetRowCount<AetherCurrent>()));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedAetherCurrentCompFlgSets",
            PlayerState.Instance()->UnlockedAetherCurrentCompFlgSetsBitArray,
            _excelService.GetSheet<AetherCurrentCompFlgSet>().Count(row => row.Territory.IsValid)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedMinerFolkloreTomes",
            PlayerState.Instance()->UnlockedMinerFolkloreTomesBitArray,
            (int)_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 16 && row.Quest.RowId < 74).Max(row => row.Quest.RowId)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedBotanistFolkloreTomes",
            PlayerState.Instance()->UnlockedBotanistFolkloreTomesBitArray,
            (int)_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 17 && row.Quest.RowId < 74).Max(row => row.Quest.RowId)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedFishingFolkloreTomes",
            PlayerState.Instance()->UnlockedFishingFolkloreTomesBitArray,
            (int)_excelService.GetSheet<GatheringSubCategory>().Where(row => row.ClassJob.RowId == 18 && row.Quest.RowId < 74).Max(row => row.Quest.RowId)));

        _bitArrays.Add(new BitArrayRecord(
            "PlayerState.UnlockedOrchestrionRolls",
            PlayerState.Instance()->UnlockedOrchestrionRollsBitArray,
            _excelService.GetRowCount<Orchestrion>()));

        // _bitfields.Add(new BitfieldRecord(
        //    "PlayerState.UnlockedFramersKits",
        //    PlayerState->UnlockedFramersKitsBitArray,
        //    unknown));

        _bitArrays.Add(new BitArrayRecord(
            "UIState.UnlockedAetherytes",
            UIState.Instance()->UnlockedAetherytesBitArray,
            _excelService.GetRowCount<Aetheryte>()));

        _bitArrays.Add(new BitArrayRecord(
            "UIState.UnlockedHowTos",
            UIState.Instance()->UnlockedHowTosBitArray,
            _excelService.GetRowCount<HowTo>()));

        _bitArrays.Add(new BitArrayRecord(
            "UIState.UnlockedCompanions",
            UIState.Instance()->UnlockedCompanionsBitArray,
            _excelService.GetRowCount<Companion>()));

        _bitArrays.Add(new BitArrayRecord(
            "UIState.UnlockedChocoboTaxiStands",
            UIState.Instance()->UnlockedChocoboTaxiStandsBitArray,
            _excelService.GetRowCount<ChocoboTaxiStand>()));

        _bitArrays.Add(new BitArrayRecord(
            "UIState.SeenCutscenes",
            UIState.Instance()->SeenCutscenesBitArray,
            _excelService.GetSheet<CutsceneWorkIndex>().Max(row => row.WorkIndex)));

        _bitArrays.Add(new BitArrayRecord(
            "UIState.UnlockedTripleTriadCards",
            UIState.Instance()->UnlockedTripleTriadCardsBitArray,
            _excelService.GetRowCount<TripleTriadCard>()));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.CompletedQuests",
            QuestManager.Instance()->CompletedQuestsBitArray,
            _excelService.GetRowCount<Quest>()));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.UnlockedMapMarkers",
            QuestManager.Instance()->UnlockedMapMarkersBitArray,
            _excelService.GetSubrowSheet<MapMarker>().Count));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.QuestRepeatFlags",
            QuestManager.Instance()->QuestRepeatFlagsBitArray,
            _excelService.GetRowCount<QuestRepeatFlag>()));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.CompletedLegacyQuestsBitArray",
            QuestManager.Instance()->CompletedLegacyQuestsBitArray,
            _excelService.GetRowCount<LegacyQuest>()));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.SeenGatheringNotebookDivisionLevelRanges",
            QuestManager.Instance()->SeenGatheringNotebookDivisionLevelRangesBitArray,
            _excelService.GetRowCount<NotebookDivision>()));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.GatheredGatheringItems",
            QuestManager.Instance()->GatheredGatheringItemsBitArray,
            _excelService.GetRowCount<GatheringItem>()));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.SeenCraftingNotebookDivisionLevelRanges",
            QuestManager.Instance()->SeenCraftingNotebookDivisionLevelRangesBitArray,
            _excelService.GetSheet<NotebookDivision>().Where(row => row.CraftOpeningLevel is not 0 or 255).Count() * 8));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.CompletedRecipes",
            QuestManager.Instance()->CompletedRecipesBitArray,
            (int)_excelService.GetSheet<Recipe>().Where(row => row.RowId < 30000).Max(row => row.RowId)));

        _bitArrays.Add(new BitArrayRecord(
            "QuestManager.CompletedLeveQuests",
            QuestManager.Instance()->CompletedLeveQuestsBitArray,
            _excelService.GetRowCount<Leve>()));
    }
}
