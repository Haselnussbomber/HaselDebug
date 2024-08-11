using System.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text.ReadOnly;
using ObjectKind = FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind;

namespace HaselDebug.Tabs;

public unsafe class ObjectTableTab(
    DebugRenderer DebugRenderer,
    SeStringEvaluatorService SeStringEvaluator,
    ImGuiContextMenuService ImGuiContextMenuService,
    IGameGui GameGui,
    ExcelService ExcelService) : DebugTab
{
    public override bool DrawInChild => false;
    public override void Draw()
    {
        using var table = ImRaii.Table("ObjectTable", 5, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("ObjectKind", ImGuiTableColumnFlags.WidthFixed, 90);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("EventHandler", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        foreach (GameObject* gameObject in GameObjectManager.Instance()->Objects.GameObjectIdSorted)
        {
            if (gameObject == null) continue;

            var objectKind = gameObject->GetObjectKind();
            var objectName = new ReadOnlySeStringSpan(gameObject->GetName()).ExtractText();

            var titleOverride = objectName;
            if (objectKind == ObjectKind.EventNpc)
            {
                var resident = ExcelService.GetRow<ENpcResident>(gameObject->BaseId);
                if (resident != null && resident.Title.RawData.Length > 0)
                {
                    var title = SeStringEvaluator.EvaluateFromAddon(37, new SeStringContext() { LocalParameters = [resident.Title] }).ExtractText();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        if (!string.IsNullOrEmpty(titleOverride))
                            titleOverride += " ";
                        titleOverride += title;
                    }
                }
            }

            if (string.IsNullOrEmpty(titleOverride))
                titleOverride = "Unnamed Object";

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Address
            DebugRenderer.DrawAddress(gameObject);

            ImGui.TableNextColumn(); // ObjectKind
            ImGui.TextUnformatted(objectKind.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawPointerType(
                gameObject,
                objectKind switch
                {
                    ObjectKind.Pc or ObjectKind.EventNpc or ObjectKind.BattleNpc => typeof(BattleChara),
                    ObjectKind.Ornament => typeof(FFXIVClientStructs.FFXIV.Client.Game.Character.Ornament),
                    ObjectKind.GatheringPoint => typeof(GatheringPointObject),
                    ObjectKind.HousingEventObject => typeof(HousingObject),
                    _ => typeof(GameObject),
                },
                new NodeOptions()
                {
                    AddressPath = new AddressPath((nint)gameObject),
                    TitleOverride = new(Encoding.UTF8.GetBytes(titleOverride)),
                    OnHovered = () =>
                    {
                        if (GameGui.WorldToScreen(gameObject->Position, out var screenPos))
                        {
                            ImGui.GetForegroundDrawList().AddLine(ImGui.GetMousePos(), screenPos, Colors.Orange);
                            ImGui.GetForegroundDrawList().AddCircleFilled(screenPos, 3f, Colors.Orange);
                        }
                    },
                    DrawContextMenu = (NodeOptions nodeOptions) =>
                    {
                        ImGuiContextMenuService.Draw($"{nodeOptions.AddressPath}ContextMenu", builder =>
                        {
                            builder.Add(new ImGuiContextMenuEntry()
                            {
                                Visible = ((byte)gameObject->TargetableStatus & 1 << 7) != 0,
                                Label = "Disable Draw",
                                ClickCallback = () => gameObject->DisableDraw()
                            });
                            builder.Add(new ImGuiContextMenuEntry()
                            {
                                Visible = ((byte)gameObject->TargetableStatus & 1 << 7) == 0,
                                Label = "Enable Draw",
                                ClickCallback = () => gameObject->EnableDraw()
                            });
                        });
                    }
                });

            ImGui.TableNextColumn(); // EventHandler

            if (gameObject->EventHandler != null)
            {
                switch (gameObject->EventHandler->Info.EventId.ContentId)
                {
                    case EventHandlerType.Adventure:
                        ImGui.TextUnformatted($"Adventure#{gameObject->EventHandler->Info.EventId.Id}");

                        var adventureName = ExcelService.GetRow<Adventure>(gameObject->EventHandler->Info.EventId.Id)?.Name.ExtractText();
                        if (!string.IsNullOrWhiteSpace(adventureName))
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.TextUnformatted($"({adventureName})");
                        }
                        break;

                    case EventHandlerType.Quest:
                        ImGui.TextUnformatted($"Quest#{gameObject->EventHandler->Info.EventId.EntryId + 0x10000u}");

                        var questName = ExcelService.GetRow<Quest>(gameObject->EventHandler->Info.EventId.EntryId + 0x10000u)?.Name.ExtractText();
                        if (!string.IsNullOrWhiteSpace(questName))
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.TextUnformatted($"({questName})");
                        }
                        break;

                    case EventHandlerType.CustomTalk:
                        ImGui.TextUnformatted($"CustomTalk#{gameObject->EventHandler->Info.EventId.Id}");

                        var customTalkName = ExcelService.GetRow<CustomTalk>(gameObject->EventHandler->Info.EventId.Id)?.Name.ExtractText();
                        if (!string.IsNullOrWhiteSpace(customTalkName))
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.TextUnformatted($"({customTalkName})");
                        }
                        break;

                    default:
                        if (string.IsNullOrEmpty(Enum.GetName(gameObject->EventHandler->Info.EventId.ContentId)))
                            ImGui.TextUnformatted($"0x{(ushort)gameObject->EventHandler->Info.EventId.ContentId:X4}");
                        else
                            ImGui.TextUnformatted($"{gameObject->EventHandler->Info.EventId.ContentId}");
                        break;
                }
            }

            i++;
            if (i >= GameObjectManager.Instance()->Objects.GameObjectIdSortedCount) break;
        }
    }
}
