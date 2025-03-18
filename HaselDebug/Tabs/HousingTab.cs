using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class HousingTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly LanguageProvider _languageProvider;

    public override void Draw()
    {
        var housingManager = HousingManager.Instance();
        if (housingManager == null)
        {
            ImGui.TextUnformatted("HousingManager unavailable");
            return;
        }

        HouseId houseId = 0;

        switch (housingManager->GetCurrentHousingTerritoryType())
        {
            case HousingTerritoryType.Outdoor:
                houseId = housingManager->OutdoorTerritory->HouseId;
                break;
            case HousingTerritoryType.Indoor:
                houseId = housingManager->IndoorTerritory->HouseId;
                break;
            case HousingTerritoryType.Workshop:
                houseId = housingManager->WorkshopTerritory->HouseId;
                break;
        }

        if (houseId != 0)
        {
            ImGui.TextUnformatted($"Current HouseId ({housingManager->GetCurrentHousingTerritoryType()})");
            ImGui.SameLine();
            _debugRenderer.DrawCopyableText($"{(long)houseId}");
            ImGui.SameLine();
            _debugRenderer.DrawCopyableText($"0x{(long)houseId:X}");
            ImGui.SameLine();
            _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new(0) });
            _debugRenderer.DrawCopyableText($"IsApartment: {houseId.IsApartment}");
            if (houseId.IsApartment)
            {
                _debugRenderer.DrawCopyableText($"Division: {houseId.ApartmentDivision}");
                _debugRenderer.DrawCopyableText($"RoomNumber: {houseId.RoomNumber}");
            }
            else
            {
                _debugRenderer.DrawCopyableText($"PlotIndex: {houseId.PlotIndex}");
                _debugRenderer.DrawCopyableText($"WardIndex: {houseId.WardIndex}");
                _debugRenderer.DrawCopyableText($"RoomNumber: {houseId.RoomNumber}");
            }
        }

        using (var node = ImRaii.TreeNode("Owned HouseIds", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                using var table = ImRaii.Table("OwnedHouseIdsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                if (table)
                {
                    ImGui.TableSetupColumn("EstateType", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("HouseId (long)", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("HouseId (struct)", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("FreeCompanyEstate");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.FreeCompanyEstate);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 0]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("PersonalChambers");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.PersonalChambers);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 1]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("PersonalEstate");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.PersonalEstate);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 2]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("Unknown3");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.Unknown3);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 3]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("SharedEstate 0");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.SharedEstate, 0);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 4]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("SharedEstate 1");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.SharedEstate, 1);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 5]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("ApartmentBuilding");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.ApartmentBuilding);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 6]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("ApartmentRoom");
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.ApartmentRoom);
                    _debugRenderer.DrawCopyableText($"{(long)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 7]) });
                }
            }
        }

        var territoryTypeId = HousingManager.GetOriginalHouseTerritoryTypeId();
        ImGui.TextUnformatted($"OriginalHouseTerritoryTypeId:");
        ImGui.SameLine();
        _debugRenderer.DrawExdRow(typeof(TerritoryType), territoryTypeId, 0, new NodeOptions()
        {
            Language = _languageProvider.ClientLanguage
        });
        ImGui.Separator();

        _debugRenderer.DrawPointerType(housingManager, typeof(HousingManager), new NodeOptions() { DefaultOpen = true });
    }
}
