using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public class AetherytesTab(IAetheryteList AetheryteList, TextService TextService, ExcelService ExcelService, TextureService TextureService) : DebugTab
{
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

        foreach (var aetheryte in AetheryteList)
        {
            var gameData = aetheryte.AetheryteData.GameData;
            if (gameData == null || gameData.Invisible || !gameData.IsAetheryte)
                continue;

            var territory = gameData.Territory.Value;
            if (territory == null)
                continue;

            var regionName = territory.Map.Value?.PlaceNameRegion.Value?.Name.ToString();
            var mapName = territory.Map.Value?.PlaceName.Value?.Name.ToString();
            var aetheryteName = gameData.PlaceName.Value?.Name.ToString();

            var regionType = GetRegion(territory);

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"#{aetheryte.AetheryteId}");

            ImGui.TableNextColumn();
            TextureService.DrawPart("Teleport", 16, GetPartId(GetTimelineId(regionType, territory.RowId)), 40 / 2f);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(GetRegionName(regionType));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(territory.ExVersion.Value?.Name.ExtractText() ?? string.Empty);

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
    private static AetheryteRegion GetRegion(TerritoryType? territoryType)
    {
        if (territoryType == null)
            return AetheryteRegion.Others;

        if (territoryType.TerritoryIntendedUse == 13)
            return AetheryteRegion.HousingArea;

        return territoryType.PlaceNameRegion.Row switch
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
            AetheryteRegion.LaNoscea => ExcelService.GetRow<PlaceName>(22)!.Name.ExtractText(), // La Noscea
            AetheryteRegion.TheBlackShroud => ExcelService.GetRow<PlaceName>(23)!.Name.ExtractText(), // The Black Shroud
            AetheryteRegion.Thanalan => ExcelService.GetRow<PlaceName>(24)!.Name.ExtractText(), // Thanalan
            AetheryteRegion.Coerthas or AetheryteRegion.Dravania or AetheryteRegion.AbalathiasSpine => TextService.GetAddonText(8486), // Ishgard and Surrounding Areas
            AetheryteRegion.GyrAbania => TextService.GetAddonText(8488), // Gyr Abania
            AetheryteRegion.Hingashi or AetheryteRegion.Othard => TextService.GetAddonText(8489), // Othard
            AetheryteRegion.Norvrandt => TextService.GetAddonText(8497), // Norvrandt
            AetheryteRegion.Ilsabard => TextService.GetAddonText(8498), // Ilsabard
            AetheryteRegion.YokTural or AetheryteRegion.XakTural => TextService.GetAddonText(8559), // Tural
            _ => TextService.GetAddonText(8484), // Others
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
