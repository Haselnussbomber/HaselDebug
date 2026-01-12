using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Services;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class EntityInspectorWindow : SimpleWindow
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly ISeStringEvaluator _seStringEvaluator;

    public uint EntityId { get; set; }

    public override void OnOpen()
    {
        base.OnOpen();

        Size = new Vector2(800, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(250, 250),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Appearing;

        Flags |= ImGuiWindowFlags.NoSavedSettings;

        RespectCloseHotkey = true;
        DisableWindowSounds = true;
    }

    public override unsafe void Draw()
    {
        var gameObject = GameObjectManager.Instance()->Objects.GetObjectByEntityId(EntityId);
        if (gameObject == null)
        {
            ImGui.Text($"GameObject with EntityId {EntityId:X} not found");
            return;
        }

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

        _debugRenderer.DrawPointerType(gameObject, typeof(GameObject), new()
        {
            DefaultOpen = true,
            Title = title,
        });
    }
}
