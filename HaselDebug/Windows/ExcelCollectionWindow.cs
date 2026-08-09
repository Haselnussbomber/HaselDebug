using HaselDebug.Services;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class ExcelCollectionWindow : SimpleWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Type _columnType;
    private readonly object _columnValue;
    private readonly uint _rowId;
    private DebugRenderer _debugRenderer;

    [AutoPostConstruct]
    private void Initialize(string windowName)
    {
        _debugRenderer = _serviceProvider.GetRequiredService<DebugRenderer>();
        WindowNameKey = string.Empty;
        WindowName = windowName;
    }

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

    public override bool DrawConditions()
    {
        return true;
    }

    public override void Draw()
    {
        _debugRenderer.DrawExcelColumn(_columnType.Name, _columnType, _columnValue, _rowId, 0, new() { DefaultOpen = true });
    }
}
