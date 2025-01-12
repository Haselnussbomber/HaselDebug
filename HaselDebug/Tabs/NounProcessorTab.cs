using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

// TODO: display current SheetHashes and compare
// https://github.com/NotAdam/Lumina/blob/master/src/Lumina/Data/Files/Excel/ExcelHeaderFile.cs#L59

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public class NounProcessorTab : DebugTab
{
    private readonly Dictionary<Type, uint> _sheets = new() {
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

    private readonly NounProcessor _nounProcessor;
    private readonly IDataManager _dataManager;
    private readonly DebugRenderer _debugRenderer;
    private readonly ClientLanguage[] _languages;
    private readonly string[] _sheetNames;
    private readonly string[] _languageNames;

    private int _selectedSheetNameIndex = 0;
    private int _selectedLanguageIndex = 0;
    private int _rowId = 1;
    private int _amount = 1;
    private static readonly string[] GermanCases = ["Nominative", "Genitive", "Dative", "Accusative"];

    public NounProcessorTab(NounProcessor nounProcessor, IDataManager dataManager, IClientState clientState, DebugRenderer debugRenderer)
    {
        _nounProcessor = nounProcessor;
        _dataManager = dataManager;
        _debugRenderer = debugRenderer;

        _languages = Enum.GetValues<ClientLanguage>();
        _languageNames = Enum.GetNames<ClientLanguage>();
        _selectedLanguageIndex = (int)clientState.ClientLanguage;
        _sheetNames = _sheets.Select(kv => kv.Key.Name).Where(name => name != "Attributive").ToArray();
    }

    public override void Draw()
    {
        var sheetName = _sheetNames.ElementAt(_selectedSheetNameIndex);
        var language = _languages[_selectedLanguageIndex];

        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("###SelectedSheetName", ref _selectedSheetNameIndex, _sheetNames, _sheetNames.Length))
        {
            _rowId = 1;
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(120);
        if (ImGui.Combo("###SelectedLanguage", ref _selectedLanguageIndex, _languageNames, _languageNames.Length))
        {
            language = _languages[_selectedLanguageIndex];
            _rowId = 1;
        }

        ImGui.SetNextItemWidth(120);
        var sheet = _dataManager.Excel.GetSheet<RawRow>(Language.English, sheetName);
        var minRowId = (int)sheet.FirstOrDefault().RowId;
        var maxRowId = (int)sheet.LastOrDefault().RowId;
        if (ImGui.InputInt("RowId###RowId", ref _rowId, 1, 10, ImGuiInputTextFlags.AutoSelectAll))
        {
            if (_rowId < minRowId)
                _rowId = minRowId;

            if (_rowId >= maxRowId)
                _rowId = maxRowId;
        }
        ImGui.SameLine();
        ImGui.TextUnformatted($"(Range: {minRowId} - {maxRowId})");

        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("Amount###Amount", ref _amount, 1, 10, ImGuiInputTextFlags.AutoSelectAll))
        {
            if (_amount <= 0)
                _amount = 1;
        }

        var numCases = language == ClientLanguage.German ? 4 : 1;
        using var table = ImRaii.Table("TextDecoderTable", 1 + numCases, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("ArticleType", ImGuiTableColumnFlags.WidthFixed, 150);
        for (var i = 0; i < numCases; i++)
            ImGui.TableSetupColumn(language == ClientLanguage.German ? GermanCases[i] : "Text");
        ImGui.TableSetupScrollFreeze(6, 1);
        ImGui.TableHeadersRow();

        var articleTypeEnumType = language switch
        {
            ClientLanguage.Japanese => typeof(JapaneseArticleType),
            ClientLanguage.German => typeof(GermanArticleType),
            ClientLanguage.French => typeof(FrenchArticleType),
            _ => typeof(EnglishArticleType)
        };

        foreach (var articleType in Enum.GetValues(articleTypeEnumType))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableHeader(articleType.ToString());

            for (var _case = 0; _case < numCases; _case++)
            {
                ImGui.TableNextColumn();

                try
                {
                    _debugRenderer.DrawCopyableText(_nounProcessor.ProcessRow(sheetName, (uint)_rowId, language.ToLumina(), _amount, (int)articleType, _case).ExtractText());
                }
                catch (Exception ex)
                {
                    _debugRenderer.DrawCopyableText(ex.ToString());
                }
            }
        }
    }
}
