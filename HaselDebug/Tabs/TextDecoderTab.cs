using System.Globalization;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Extensions.Dalamud;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public class TextDecoderTab : DebugTab
{
    private readonly string[] SheetNames = [
        "Aetheryte",
        "BNpcName",
        "BeastTribe",
        "DeepDungeonEquipment",
        "DeepDungeonItem",
        "DeepDungeonMagicStone",
        "ENpcResident",
        "EObjName",
        "EurekaAetherItem",
        "EventItem",
        "GCRankGridaniaFemaleText",
        "GCRankGridaniaMaleText",
        "GCRankLimsaFemaleText",
        "GCRankLimsaMaleText",
        "GCRankUldahFemaleText",
        "GCRankUldahMaleText",
        "GatheringPointName",
        "Glasses",
        "GlassesStyle",
        "HousingPreset",
        "Item",
        "MJIName",
        "Mount",
        "Ornament",
        "TripleTriadCard"
    ];

    private readonly TextDecoder TextDecoder;
    private readonly IDataManager DataManager;
    private readonly ClientLanguage[] Languages;
    private readonly string[] LanguageNames;

    private int SelectedSheetNameIndex = 0;
    private int SelectedLanguageIndex = 0;
    private CultureInfo CultureInfo;
    private int RowId = 1;
    private int Amount = 1;
    private bool EnableTitleCase = false;

    public TextDecoderTab(TextDecoder textDecoder, IDataManager dataManager, IClientState clientState)
    {
        TextDecoder = textDecoder;
        DataManager = dataManager;

        Languages = Enum.GetValues<ClientLanguage>();
        LanguageNames = Enum.GetNames<ClientLanguage>();
        SelectedLanguageIndex = (int)clientState.ClientLanguage;
        CultureInfo = CultureInfo.GetCultureInfo(clientState.ClientLanguage.ToCode());
    }

    public override void Draw()
    {
        var sheetName = SheetNames[SelectedSheetNameIndex];
        var language = Languages[SelectedLanguageIndex];

        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("###SelectedSheetName", ref SelectedSheetNameIndex, SheetNames, SheetNames.Length))
        {
            RowId = 1;
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(120);
        if (ImGui.Combo("###SelectedLanguage", ref SelectedLanguageIndex, LanguageNames, LanguageNames.Length))
        {
            language = Languages[SelectedLanguageIndex];
            RowId = 1;
            CultureInfo = CultureInfo.GetCultureInfo(language.ToCode());
        }

        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("RowId###RowId", ref RowId, 1, 10, ImGuiInputTextFlags.AutoSelectAll))
        {
            if (RowId < 0)
                RowId = 1;

            if (RowId >= (DataManager.Excel.GetSheetRaw(sheetName)?.RowCount ?? 1))
                RowId = 1;
        }

        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("Amount###Amount", ref Amount, 1, 10, ImGuiInputTextFlags.AutoSelectAll))
        {
            if (Amount <= 0)
                Amount = 1;
        }

        ImGui.Checkbox("Title Case###TitleCase", ref EnableTitleCase);

        using var table = ImRaii.Table("TextDecoderTable", 6, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Person", ImGuiTableColumnFlags.WidthFixed, 60);
        for (var i = 1; i < 6; i++)
            ImGui.TableSetupColumn($"Case {i}");
        ImGui.TableSetupScrollFreeze(6, 1);
        ImGui.TableHeadersRow();

        for (var person = 1; person < 6; person++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableHeader($"Person {person}");

            for (var @case = 1; @case < 6; @case++)
            {
                ImGui.TableNextColumn();
                var text = TextDecoder.ProcessNoun(language, sheetName, person, RowId, Amount, @case).ExtractText();

                if (EnableTitleCase)
                    text = CultureInfo.TextInfo.ToTitleCase(text);

                ImGui.TextUnformatted(text);
            }
        }
    }
}
