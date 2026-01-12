using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ObjectTableRenderer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly WindowManager _windowManager;
    private readonly LanguageProvider _languageProvider;

    public void Draw(string key, Span<(int Index, Pointer<GameObject> GameObjectPtr)> entries)
    {
        using var table = ImRaii.Table(key, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.Hideable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Address"u8, ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("EntityId"u8, ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        //ImGui.TableSetupColumn("EventHandler"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < entries.Length; i++)
        {
            var gameObject = entries[i].GameObjectPtr.Value;
            if (gameObject == null) continue;

            var objectKind = gameObject->GetObjectKind();
            var objectName = gameObject->GetName().AsReadOnlySeStringSpan().ToString();
            var entityId = gameObject->EntityId;

            var title = $"[{objectKind}] {objectName}";

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
            ImGuiUtils.DrawCopyableText(entityId.ToString("X"));

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawPointerType(gameObject, typeof(GameObject), new NodeOptions()
            {
                AddressPath = new AddressPath((nint)gameObject),
                Title = title,
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () =>
                        {
                            var windowName = $"Entity #{entityId:X}";
                            var window = _windowManager.CreateOrOpen(windowName, () => _serviceProvider.CreateInstance<EntityInspectorWindow>());
                            window.WindowNameKey = string.Empty;
                            window.WindowName = windowName;
                            window.EntityId = entityId;
                        }
                    });
                    builder.AddCopyName(objectName);
                    builder.AddCopyAddress((nint)gameObject);
                }
            });
        }
    }
}
