using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class AetherytesTab : DebugTab
{
    private readonly IAetheryteList _aetheryteList;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly TextureService _textureService;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("AetheryteListTable", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Region Category", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Expansion Category", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Region Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Map Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Aetheryte Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Gil Cost", ImGuiTableColumnFlags.WidthFixed, 80);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var aetheryte in _aetheryteList)
        {
            if (!aetheryte.AetheryteData.IsValid)
                continue;

            var gameData = aetheryte.AetheryteData.Value;
            if (gameData.Invisible || !gameData.IsAetheryte || !gameData.Territory.IsValid)
                continue;

            var territory = gameData.Territory.Value;
            if (!territory.Map.IsValid)
                continue;

            var regionName = territory.Map.Value.PlaceNameRegion.Value.Name.ToString();
            var mapName = territory.Map.Value.PlaceName.Value.Name.ToString();
            var aetheryteName = gameData.PlaceName.Value.Name.ToString();

            var regionType = GetRegion(territory);

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"#{aetheryte.AetheryteId}");

            ImGui.TableNextColumn();
            _textureService.DrawPart("Teleport", 16, GetPartId(GetTimelineId(regionType, territory.RowId)), 40 / 2f);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(GetRegionName(regionType));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(territory.ExVersion.Value.Name.ExtractText());

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(regionName);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(mapName);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(aetheryteName);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{aetheryte.GilCost}{SeIconChar.Gil.ToIconString()}");
        }
    }

    // "48 83 EC 28 0F B7 4A 08"
    // int GetRegion(Client::UI::Agent::AgentTeleport* thisPtr, Client::Game::UI::TeleportInfo* teleportInfo)
    private static AetheryteRegion GetRegion(TerritoryType territoryType)
    {
        //if (territoryType == null)
        //    return AetheryteRegion.Others;

        if (territoryType.TerritoryIntendedUse.RowId == 13)
            return AetheryteRegion.HousingArea;

        return territoryType.PlaceNameRegion.RowId switch
        {
            22u => AetheryteRegion.LaNoscea,
            23u => AetheryteRegion.TheBlackShroud,
            24u => AetheryteRegion.Thanalan,
            25u => AetheryteRegion.Coerthas,
            498u => AetheryteRegion.Dravania,
            497u => AetheryteRegion.AbalathiasSpine,
            26u => AetheryteRegion.MorDhona,
            2400u => AetheryteRegion.GyrAbania,
            2402u => AetheryteRegion.Hingashi,
            2401u => AetheryteRegion.Othard,
            2950u => AetheryteRegion.Norvrandt,
            3703u => AetheryteRegion.Ilsabard,
            3702u => AetheryteRegion.TheNorthernEmpty,
            3704u => AetheryteRegion.TheSeaOfStars,
            3705u => AetheryteRegion.TheWorldUnsundered,
            4500u => AetheryteRegion.YokTural,
            4501u => AetheryteRegion.XakTural,
            4502u => AetheryteRegion.UnlostWorld,
            _ => AetheryteRegion.Others,
        };
    }

    // ids are hardcoded in the opener functions
    private string GetRegionName(AetheryteRegion region)
    {
        return region switch
        {
            AetheryteRegion.LaNoscea => _excelService.TryGetRow<PlaceName>(22, out var placeName) ? placeName.Name.ExtractText() : string.Empty, // La Noscea
            AetheryteRegion.TheBlackShroud => _excelService.TryGetRow<PlaceName>(23, out var placeName) ? placeName.Name.ExtractText() : string.Empty, // The Black Shroud
            AetheryteRegion.Thanalan => _excelService.TryGetRow<PlaceName>(24, out var placeName) ? placeName.Name.ExtractText() : string.Empty, // Thanalan
            AetheryteRegion.Coerthas or AetheryteRegion.Dravania or AetheryteRegion.AbalathiasSpine => _textService.GetAddonText(8486), // Ishgard and Surrounding Areas
            AetheryteRegion.GyrAbania => _textService.GetAddonText(8488), // Gyr Abania
            AetheryteRegion.Hingashi or AetheryteRegion.Othard => _textService.GetAddonText(8489), // Othard
            AetheryteRegion.Norvrandt => _textService.GetAddonText(8497), // Norvrandt
            AetheryteRegion.Ilsabard => _textService.GetAddonText(8498), // Ilsabard
            AetheryteRegion.YokTural or AetheryteRegion.XakTural => _textService.GetAddonText(8559), // Tural
            _ => _textService.GetAddonText(8484), // Others
        };
    }

    // "E8 ?? ?? ?? ?? 49 8D 4E F8 8B D8"
    // int GetTimelineId(Client::UI::Agent::AgentTeleport* thisPtr, int region, int territoryTypeId)
    private static uint GetTimelineId(AetheryteRegion region, uint territoryTypeId)
    {
        return territoryTypeId switch
        {
            819 => 8, // The Crystarium
            820 => 9, // Eulmore
            958 => 11, // Garlemald
            1186 or 1191 => 14, // Solution Nine

            _ => region switch
            {
                AetheryteRegion.LaNoscea => 0,
                AetheryteRegion.TheBlackShroud => 1,
                AetheryteRegion.Thanalan => 2,
                AetheryteRegion.Coerthas => 3,
                AetheryteRegion.HousingArea => 5,
                AetheryteRegion.GyrAbania => 6,
                AetheryteRegion.Hingashi => 7,
                AetheryteRegion.Ilsabard => 10,
                AetheryteRegion.TheNorthernEmpty => 12,
                AetheryteRegion.YokTural or AetheryteRegion.XakTural => 13,
                _ => 4, // Others
            },
        };
    }

    // Found via manually inspecting the Teleport uld using VFXEditor.
    // Timeline 18, Frames 1.
    // The part index is the Value of the "TextColor" entry.
    private static uint GetPartId(uint timelineId)
    {
        return timelineId switch
        {
            3 => 4,
            4 => 3,
            _ => timelineId,
        };
    }

    private static AetheryteExpansionCategory GetExpansionCategory(AetheryteRegion region, uint territoryTypeId)
    {
        return region switch
        {
            AetheryteRegion.LaNoscea => AetheryteExpansionCategory.LaNoscea,
            AetheryteRegion.TheBlackShroud => AetheryteExpansionCategory.TheBlackShroud,
            AetheryteRegion.Thanalan => AetheryteExpansionCategory.Thanalan,
            // dunno
            _ => AetheryteExpansionCategory.Others
        };
    }

    private enum AetheryteRegion
    {
        LaNoscea = 0,
        TheBlackShroud,
        Thanalan,
        Coerthas,
        Dravania,
        AbalathiasSpine,
        MorDhona,
        GyrAbania,
        Hingashi,
        Othard,
        HousingArea,
        Norvrandt,
        Ilsabard,
        TheNorthernEmpty,
        TheSeaOfStars,
        TheWorldUnsundered,
        YokTural,
        XakTural,
        UnlostWorld,
        Others
    }

    private enum AetheryteExpansionCategory
    {
        LaNoscea = 0,
        TheBlackShroud,
        Thanalan,
        ARealmReborn,
        Heavensward,
        Stormblood,
        Shadowbringers,
        Endwalker,
        Dawntrail,
        Others
    }
}
