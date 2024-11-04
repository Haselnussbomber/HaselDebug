using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Extensions.Dalamud;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

// TODO: display current SheetHashes and compare
// https://github.com/NotAdam/Lumina/blob/master/src/Lumina/Data/Files/Excel/ExcelHeaderFile.cs#L59

public class TextDecoderTab : DebugTab
{
    private readonly Dictionary<Type, uint> Sheets = new() {
        { typeof(Attributive), 0xECF11B18 },
        { typeof(Aetheryte), 0xB41A2B3C },
        { typeof(BNpcName), 0x77A72DA0 },
        { typeof(BeastTribe), 0x2FAF7B33 },
        { typeof(DeepDungeonEquipment), 0xC638F2BF },
        { typeof(DeepDungeonItem), 0x878768C6 },
        { typeof(DeepDungeonMagicStone), 0xC638F2BF },
        { typeof(DeepDungeonDemiclone), 0xC638F2BF },
        { typeof(ENpcResident), 0xF74FA88C },
        { typeof(EObjName), 0x77A72DA0 },
        { typeof(EurekaAetherItem), 0x45C06AE0 },
        { typeof(EventItem), 0x2A1D4FB2 },
        { typeof(GCRankGridaniaFemaleText), 0xD573CBA6 },
        { typeof(GCRankGridaniaMaleText), 0xD573CBA6 },
        { typeof(GCRankLimsaFemaleText), 0xD573CBA6 },
        { typeof(GCRankLimsaMaleText), 0xD573CBA6 },
        { typeof(GCRankUldahFemaleText), 0xD573CBA6 },
        { typeof(GCRankUldahMaleText), 0xD573CBA6 },
        { typeof(GatheringPointName), 0x77A72DA0 },
        { typeof(Glasses), 0x2FAAC2C1 },
        { typeof(GlassesStyle), 0xC138BB6E },
        { typeof(HousingPreset), 0x9184AF18 },
        { typeof(Item), 0xE9A33C9D },
        { typeof(MJIName), 0x77A72DA0 },
        { typeof(Mount), 0x304B5115 },
        { typeof(Ornament), 0x3D312C8F },
        { typeof(TripleTriadCard), 0x45C06AE0 },
    };

    private readonly TextDecoder TextDecoder;
    private readonly IDataManager DataManager;
    private readonly ClientLanguage[] Languages;
    private readonly string[] SheetNames;
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
        SheetNames = Sheets.Select(kv => kv.Key.Name).Where(name => name != "Attributive").ToArray();
    }

    public override void Draw()
    {
        var sheetName = SheetNames.ElementAt(SelectedSheetNameIndex);
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
        var sheet = DataManager.Excel.GetSheet<RawRow>(Language.English, sheetName);
        var minRowId = (int)sheet.FirstOrDefault().RowId;
        var maxRowId = (int)sheet.LastOrDefault().RowId;
        if (ImGui.InputInt("RowId###RowId", ref RowId, 1, 10, ImGuiInputTextFlags.AutoSelectAll))
        {
            if (RowId < minRowId)
                RowId = minRowId;

            if (RowId >= maxRowId)
                RowId = maxRowId;
        }
        ImGui.SameLine();
        ImGui.TextUnformatted($"(Range: {minRowId} - {maxRowId})");

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
