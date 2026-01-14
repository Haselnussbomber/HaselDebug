using HaselDebug.Services;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class AddonInspectorWindow : SimpleWindow
{
    private readonly AtkDebugRenderer _atkDebugRenderer;

    public ushort AddonId { get; internal set; }

    public string AddonName
    {
        get;
        set { field = value; WindowName = value; }
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

    public override void Draw()
    {
        _atkDebugRenderer.DrawAddon(new DrawAddonParams()
        {
            AddonId = AddonId,
            AddonName = AddonName,
            Border = false
        });
    }
}
