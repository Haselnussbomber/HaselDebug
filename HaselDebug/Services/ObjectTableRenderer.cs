using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Utils;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ObjectTableRenderer
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly WindowManager _windowManager;
    private readonly LanguageProvider _languageProvider;

    public void Draw(string key, Span<(int Index, Pointer<GameObject> GameObjectPtr)> entries)
    {
        using var table = ImRaii.Table(key, 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.Hideable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Address"u8, ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("EntityId"u8, ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("ObjectId"u8, ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("ObjectKind"u8, ImGuiTableColumnFlags.WidthFixed, 90);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("EventHandler"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < entries.Length; i++)
        {
            var gameObject = entries[i].GameObjectPtr.Value;
            if (gameObject == null) continue;

            var objectKind = gameObject->GetObjectKind();
            var objectName = new ReadOnlySeStringSpan(gameObject->GetName().AsSpan()).ToString();

            var title = objectName;
            if (objectKind == ObjectKind.EventNpc && _excelService.TryGetRow<ENpcResident>(gameObject->BaseId, out var resident) && !resident.Title.IsEmpty)
            {
                var evaluated = _seStringEvaluator.EvaluateFromAddon(37, [resident.Title]).ToString();
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
            ImGui.Text(entries[i].Index.ToString());

            ImGui.TableNextColumn(); // Address
            _debugRenderer.DrawAddress(gameObject);

            ImGui.TableNextColumn(); // EntityId
            ImGuiUtilsEx.DrawCopyableText(gameObject->EntityId.ToString("X"));

            ImGui.TableNextColumn(); // ObjectId
            ImGuiUtilsEx.DrawCopyableText(gameObject->GetGameObjectId().Id.ToString("X"));

            ImGui.TableNextColumn(); // ObjectKind
            ImGuiUtilsEx.DrawCopyableText(objectKind.ToString());

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawPointerType(
            gameObject,
                typeof(GameObject),
                new Utils.NodeOptions()
                {
                    AddressPath = new Utils.AddressPath((nint)gameObject),
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
                        ImGui.Text($"Adventure#{gameObject->EventHandler->Info.EventId.Id}");

                        if (_excelService.TryGetRow<Adventure>(gameObject->EventHandler->Info.EventId.Id, out var adventure) && !adventure.Name.IsEmpty)
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.Text($"({adventure.Name})");
                        }
                        break;

                    case EventHandlerContent.Quest:
                        ImGui.Text($"Quest#{gameObject->EventHandler->Info.EventId.EntryId + 0x10000u}");

                        if (_excelService.TryGetRow<Quest>(gameObject->EventHandler->Info.EventId.EntryId + 0x10000u, out var quest) && !quest.Name.IsEmpty)
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.Text($"({quest.Name})");
                        }
                        break;

                    case EventHandlerContent.CustomTalk:
                        ImGui.Text($"CustomTalk#{gameObject->EventHandler->Info.EventId.Id}");

                        if (_excelService.TryGetRow<CustomTalk>(gameObject->EventHandler->Info.EventId.Id, out var customTalk) && !customTalk.Name.IsEmpty)
                        {
                            ImGuiUtils.SameLineSpace();
                            ImGui.Text($"({customTalk.Name})");
                        }
                        break;

                    default:
                        if (string.IsNullOrEmpty(Enum.GetName(gameObject->EventHandler->Info.EventId.ContentId)))
                            ImGui.Text($"0x{(ushort)gameObject->EventHandler->Info.EventId.ContentId:X4}");
                        else
                            ImGui.Text($"{gameObject->EventHandler->Info.EventId.ContentId}");
                        break;
                }
            }
        }
    }
}
