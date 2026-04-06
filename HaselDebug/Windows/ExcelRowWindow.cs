using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class ExcelRowWindow : SimpleWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelRowIdentifier _identifier;
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
        if (_identifier.IsSubrowSheet)
        {
            if (_identifier.SubrowId.HasValue)
            {
                _debugRenderer.DrawExdSubrow(_identifier.SheetType, _identifier.RowId, _identifier.SubrowId.Value, 0, new NodeOptions() { DefaultOpen = true, Language = _identifier.Language });
            }
            else
            {
                _debugRenderer.DrawExdSubrows(_identifier.SheetType, _identifier.RowId, 0, new NodeOptions() { DefaultOpen = true, Language = _identifier.Language });
            }
        }
        else
        {
            _debugRenderer.DrawExdRow(_identifier.SheetType, _identifier.RowId, 0, new NodeOptions() { DefaultOpen = true, Language = _identifier.Language });
        }
    }
}
