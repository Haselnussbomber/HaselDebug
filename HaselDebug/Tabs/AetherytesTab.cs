using Dalamud.Game.Text;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

using TerritoryIntendedUseEnum = FFXIVClientStructs.FFXIV.Client.Enums.TerritoryIntendedUse;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class AetherytesTab : DebugTab
{
    private readonly IAetheryteList _aetheryteList;
    private readonly TextService _textService;
    private readonly UldService _uldService;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("AetheryteListTable"u8, 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Region Category"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Expansion Category"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Region Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Map Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Aetheryte Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Gil Cost"u8, ImGuiTableColumnFlags.WidthFixed, 80);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var aetheryte in _aetheryteList)
        {
            if (!aetheryte.AetheryteData.IsValid)
                continue;

            var gameData = aetheryte.AetheryteData.Value;
            if (gameData.Invisible || !gameData.Territory.IsValid)
                continue;

            var territory = gameData.Territory.Value;
            if (!territory.Map.IsValid)
                continue;

            var regionName = territory.Map.Value.PlaceNameRegion.Value.Name.ToString();
            var mapName = territory.Map.Value.PlaceName.Value.Name.ToString();
            var aetheryteName = gameData.PlaceName.Value.Name.ToString();

            var regionType = GetRegion(territory);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // ID
            ImGui.Text($"#{aetheryte.AetheryteId}");

            ImGui.TableNextColumn(); // Icon
            _uldService.DrawPart("Teleport", 16, GetPartId(GetTimelineId(regionType, territory.RowId)), 40 / 2f);

            ImGui.TableNextColumn(); // Region Category
            ImGui.Text(GetRegionName(regionType));

            ImGui.TableNextColumn(); // Expansion Category
            ImGui.Text(territory.ExVersion.Value.Name.ToString());

            ImGui.TableNextColumn(); // Region Name
            ImGui.Text(regionName);

            ImGui.TableNextColumn(); // Map Name
            ImGui.Text(mapName);

            ImGui.TableNextColumn(); // Aetheryte Name
            ImGui.Text(aetheryteName);

            ImGui.TableNextColumn(); // Gil Cost
            ImGui.Text($"{aetheryte.GilCost}{SeIconChar.Gil.ToIconString()}");
        }
    }

    // "48 83 EC 28 0F B7 4A 08"
    // int GetRegion(Client::UI::Agent::AgentTeleport* thisPtr, Client::Game::UI::TeleportInfo* teleportInfo)
    private static AetheryteRegion GetRegion(TerritoryType territoryType)
    {
        if (territoryType.TerritoryIntendedUse.RowId == (uint)TerritoryIntendedUseEnum.HousingOutdoor)
            return AetheryteRegion.HousingArea;

        return territoryType.PlaceNameRegion.RowId switch
        {
            22u => AetheryteRegion.LaNoscea,
            23u => AetheryteRegion.TheBlackShroud,
            24u => AetheryteRegion.Thanalan,
            25u => AetheryteRegion.Coerthas,
            497u => AetheryteRegion.Dravania,
            498u => AetheryteRegion.AbalathiasSpine,
            26u => AetheryteRegion.Hingashi,
            2400u => AetheryteRegion.MorDhona,
            2402u => AetheryteRegion.GyrAbania,
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

    private string GetRegionName(AetheryteRegion region)
    {
        return region switch
        {
            AetheryteRegion.LaNoscea => _textService.GetPlaceName(22), // La Noscea
            AetheryteRegion.TheBlackShroud => _textService.GetPlaceName(23), // The Black Shroud
            AetheryteRegion.Thanalan => _textService.GetPlaceName(24), // Thanalan
            AetheryteRegion.Coerthas => _textService.GetPlaceName(25), // Coerthas
            AetheryteRegion.Dravania => _textService.GetPlaceName(497), // Abalathia's Spine
            AetheryteRegion.AbalathiasSpine => _textService.GetPlaceName(498), // Dravania
            AetheryteRegion.MorDhona => _textService.GetPlaceName(2400), // Gyr Abania
            AetheryteRegion.GyrAbania => _textService.GetPlaceName(2402), // Hingashi
            AetheryteRegion.Hingashi => _textService.GetPlaceName(26), // Mor Dhona
            AetheryteRegion.Othard => _textService.GetPlaceName(2401), // Othard
            AetheryteRegion.HousingArea => _textService.GetAddonText(8495), // Residential Areas
            AetheryteRegion.Norvrandt => _textService.GetPlaceName(2950), // Norvrandt
            AetheryteRegion.Ilsabard => _textService.GetPlaceName(3703), // Ilsabard
            AetheryteRegion.TheNorthernEmpty => _textService.GetPlaceName(3702), // The Northern Empty
            AetheryteRegion.TheSeaOfStars => _textService.GetPlaceName(3704), // The Sea of Stars
            AetheryteRegion.TheWorldUnsundered => _textService.GetPlaceName(3705), // The World Unsundered
            AetheryteRegion.YokTural => _textService.GetPlaceName(4500), // Yok Tural
            AetheryteRegion.XakTural => _textService.GetPlaceName(4501), // Xak Tural
            AetheryteRegion.UnlostWorld => _textService.GetPlaceName(4502), // Unlost World
            _ => string.Empty
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

    private enum AetheryteRegion
    {
        LaNoscea = 0,
        TheBlackShroud = 1,
        Thanalan = 2,
        Coerthas = 3,
        Dravania = 4,
        AbalathiasSpine = 5,
        MorDhona = 6,
        GyrAbania = 7,
        Hingashi = 8,
        Othard = 9,
        HousingArea = 10,
        Norvrandt = 11,
        Ilsabard = 12,
        TheNorthernEmpty = 13,
        TheSeaOfStars = 14,
        TheWorldUnsundered = 15,
        YokTural = 16,
        XakTural = 17,
        UnlostWorld = 18,
        Others = 19,
    }

    private enum AetheryteExpansionCategory
    {
        LaNoscea = 0,
        TheBlackShroud = 1,
        Thanalan = 2,
        ARealmReborn = 3,
        Heavensward = 4,
        Stormblood = 5,
        Shadowbringers = 6,
        Endwalker = 7,
        Dawntrail = 8,
        Others = 9,
    }
}
