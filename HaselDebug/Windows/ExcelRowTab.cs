using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class ExcelRowTab : SimpleWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Type _rowType;
    private readonly uint _rowId;
    private readonly ushort _subrowId;
    private readonly ClientLanguage _language;
    private DebugRenderer _debugRenderer;
    private bool _isSubrow;

    [AutoPostConstruct]
    private void Initialize(string windowName)
    {
        _debugRenderer = _serviceProvider.GetRequiredService<DebugRenderer>();
        WindowNameKey = string.Empty;
        WindowName = windowName;
        _isSubrow = _rowType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExcelSubrow<>));
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
        if (_isSubrow)
            _debugRenderer.DrawExdSubrow(_rowType, _rowId, _subrowId, 0, new NodeOptions() { DefaultOpen = true, Language = _language });
        else
            _debugRenderer.DrawExdRow(_rowType, _rowId, 0, new NodeOptions() { DefaultOpen = true, Language = _language });
    }
}
