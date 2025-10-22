using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

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
            ImGui.Text("HousingManager unavailable"u8);
            return;
        }

        var houseId = housingManager->GetCurrentHouseId();
        if (houseId != 0)
        {
            ImGui.Text($"Current HouseId ({housingManager->GetCurrentHousingTerritoryType()})");
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText($"0x{(ulong)houseId:X}");
            ImGui.SameLine();
            _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new(0) });
            ImGuiUtils.DrawCopyableText($"IsApartment: {houseId.IsApartment}");
            if (houseId.IsApartment)
            {
                ImGuiUtils.DrawCopyableText($"Division: {houseId.ApartmentDivision}");
                ImGuiUtils.DrawCopyableText($"RoomNumber: {houseId.RoomNumber}");
            }
            else
            {
                if (houseId.PlotIndex < 60)
                    ImGuiUtils.DrawCopyableText($"PlotIndex: {houseId.PlotIndex}");
                ImGuiUtils.DrawCopyableText($"WardIndex: {houseId.WardIndex}");
                ImGuiUtils.DrawCopyableText($"RoomNumber: {houseId.RoomNumber}");
            }
        }

        using (var node = ImRaii.TreeNode("Owned HouseIds", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                using var table = ImRaii.Table("OwnedHouseIdsTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                if (table)
                {
                    ImGui.TableSetupColumn("EstateType"u8, ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("HouseId (long)"u8, ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("HouseId (struct)"u8, ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("FreeCompanyEstate"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.FreeCompanyEstate);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 0]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("PersonalChambers"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.PersonalChambers);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 1]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("PersonalEstate"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.PersonalEstate);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 2]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Unknown3"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.Unknown3);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 3]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("SharedEstate 0"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.SharedEstate, 0);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 4]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("SharedEstate 1"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.SharedEstate, 1);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 5]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("ApartmentBuilding"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.ApartmentBuilding);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 6]) });

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("ApartmentRoom"u8);
                    ImGui.TableNextColumn();
                    houseId = HousingManager.GetOwnedHouseId(EstateType.ApartmentRoom);
                    ImGuiUtils.DrawCopyableText($"{(ulong)houseId}");
                    ImGui.TableNextColumn();
                    _debugRenderer.DrawPointerType(&houseId, typeof(HouseId), new() { AddressPath = new([1, 7]) });
                }
            }
        }

        var territoryTypeId = HousingManager.GetOriginalHouseTerritoryTypeId();
        ImGui.Text($"OriginalHouseTerritoryTypeId:");
        ImGui.SameLine();
        _debugRenderer.DrawExdRow(typeof(TerritoryType), territoryTypeId, 0, new NodeOptions()
        {
            Language = _languageProvider.ClientLanguage
        });
        ImGui.Separator();
    }
}
