using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using ObjectKind = FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ObjectTableTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly SeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("ObjectTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("ObjectKind", ImGuiTableColumnFlags.WidthFixed, 90);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("EventHandler", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        foreach (GameObject* gameObject in GameObjectManager.Instance()->Objects.IndexSorted)
        {
            if (gameObject == null) continue;

            var objectKind = gameObject->GetObjectKind();
            var objectName = new ReadOnlySeStringSpan(gameObject->GetName().AsSpan()).ExtractText();

            var title = objectName;
            if (objectKind == ObjectKind.EventNpc && _excelService.TryGetRow<ENpcResident>(gameObject->BaseId, out var resident) && !resident.Title.IsEmpty)
            {
                var evaluated = _seStringEvaluator.EvaluateFromAddon(37, [resident.Title]).ExtractText();
                if (!string.IsNullOrWhiteSpace(evaluated))
                {
                    if (!string.IsNullOrEmpty(evaluated))
                        title += " ";
                    title += evaluated;
                }
            }

            if (string.IsNullOrEmpty(title))
                title = "Unnamed Object";

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Address
            _debugRenderer.DrawAddress(gameObject);

            ImGui.TableNextColumn(); // ObjectKind
            ImGui.TextUnformatted(objectKind.ToString());

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawPointerType(
                gameObject,
                typeof(GameObject),
                new NodeOptions()
                {
                    AddressPath = new AddressPath((nint)gameObject),
                    Title = title,
                    DrawContextMenu = (nodeOptions, builder) =>
                    {
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = ((byte)gameObject->TargetableStatus & 1 << 7) != 0,
                            Label = _textService.Translate("ContextMenu.GameObject.DisableDraw"),
                            ClickCallback = () => gameObject->DisableDraw()
                        });
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = ((byte)gameObject->TargetableStatus & 1 << 7) == 0,
                            Label = _textService.Translate("ContextMenu.GameObject.EnableDraw"),
                            ClickCallback = () => gameObject->EnableDraw()
                        });
                    }
                });

            ImGui.TableNextColumn(); // EventHandler

            if (gameObject->EventHandler != null)
            {
                switch (gameObject->EventHandler->Info.EventId.ContentId)
                {
                    case EventHandlerContent.Adventure:
                        ImGui.TextUnformatted($"Adventure#{gameObject->EventHandler->Info.EventId.Id}");

                        if (_excelService.TryGetRow<Adventure>(gameObject->EventHandler->Info.EventId.Id, out var adventure) && !adventure.Name.IsEmpty)
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.TextUnformatted($"({adventure.Name})");
                        }
                        break;

                    case EventHandlerContent.Quest:
                        ImGui.TextUnformatted($"Quest#{gameObject->EventHandler->Info.EventId.EntryId + 0x10000u}");

                        if (_excelService.TryGetRow<Quest>(gameObject->EventHandler->Info.EventId.EntryId + 0x10000u, out var quest) && !quest.Name.IsEmpty)
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.TextUnformatted($"({quest.Name})");
                        }
                        break;

                    case EventHandlerContent.CustomTalk:
                        ImGui.TextUnformatted($"CustomTalk#{gameObject->EventHandler->Info.EventId.Id}");

                        if (_excelService.TryGetRow<CustomTalk>(gameObject->EventHandler->Info.EventId.Id, out var customTalk) && !customTalk.Name.IsEmpty)
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.TextUnformatted($"({customTalk.Name})");
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
