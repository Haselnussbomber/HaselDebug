using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImAnimSharp;

namespace HaselDebug.Tabs;

[RegisterSingleton, AutoConstruct]
public partial class ImAnimService : IDisposable
{
    private readonly ILogger<ImAnimService> _logger;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ImGuiService _imGuiService;
    private bool _initialized;
    private bool _isDisposed;

    [AutoPostConstruct]
    private void Initialize()
    {
        _imGuiService.PreDraw += OnPreDraw;
        _imGuiService.PostDraw += ImAnim.EndFrame;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogDebug("Dispose");

        _isDisposed = true;

        _imGuiService.PreDraw -= OnPreDraw;
        _imGuiService.PostDraw -= ImAnim.EndFrame;
        ImAnim.Shutdown();
    }

    private void OnPreDraw()
    {
        if (_isDisposed)
            return;

        if (!_initialized)
        {
            ImAnim.Initialize();
            ImAnim.SetConfigDirectory(_pluginInterface.GetPluginConfigDirectory());

            _initialized = true;
        }

        ImAnim.BeginFrame();
    }
}

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ImAnimTestTab : DebugTab
{
    private readonly ILogger<ImAnimTestTab> _logger;
    private readonly ImAnimService _imAnimService;
    private bool _showDemoWindow;
    private bool _initialized;
    private int _callbackBeginCount;
    private int _callbackUpdateCount;
    private int _callbackCompleteCount;

    private ImAnim.ClipCallback _beginCallback;
    private ImAnim.ClipCallback _updateCallback;
    private ImAnim.ClipCallback _completeCallback;

    public override void Draw()
    {
        _logger.LogDebug("Draw");

        if (!_initialized)
        {
            _beginCallback = OnBegin;
            _updateCallback = OnUpdate;
            _completeCallback = OnComplete;

            var spring = new ImAnimSpringParams
            {
                Mass = 1.0f,
                Stiffness = 180.0f,
                Damping = 22.0f, // Higher damping to prevent excessive scale overshoot
                InitialVelocity = 0.0f,
            };

            ImAnimClip.Begin("bounce"u8)
                .KeyVec2("offset"u8, 0.0f, new Vector2(0, -50), ImAnimEaseType.Linear)
                .KeyFloat("scale"u8, 0.0f, 0.6f, ImAnimEaseType.Linear)
                .KeyFloat("alpha"u8, 0.0f, 0.3f, ImAnimEaseType.Linear)
                .KeyVec2("offset"u8, 0.3f, new Vector2(0, 10), ImAnimEaseType.OutQuad)
                .KeyFloat("alpha"u8, 0.3f, 1.0f, ImAnimEaseType.OutQuad)
                .KeyVec2("offset"u8, 0.5f, new Vector2(0, -15), ImAnimEaseType.OutQuad)
                .KeyVec2("offset"u8, 0.7f, new Vector2(0, 5), ImAnimEaseType.OutQuad)
                .KeyVec2("offset"u8, 0.9f, new Vector2(0, 0), ImAnimEaseType.OutBounce)
                .KeyFloatSpring("scale"u8, 0.3f, 1.0f, spring)
                .End();

            ImAnimClip.Begin("clip-with-callbacks"u8)
                .KeyFloat("scale"u8, 0.0f, 0.5f, ImAnimEaseType.OutCubic)
                .KeyFloat("scale"u8, 0.5f, 1.2f, ImAnimEaseType.OutBack)
                .KeyFloat("scale"u8, 1.0f, 1.0f, ImAnimEaseType.InOutSine)
                .OnBegin(_beginCallback)
                .OnUpdate(_updateCallback)
                .OnComplete(_completeCallback)
                .End();

            _initialized = true;
        }

        // var inst = ImAnim.Play("bounce"u8, "widget_1"u8);
        // ImGui.Text($"scale: {(inst.GetFloat("scale"u8, out var scale) ? scale : 0)}");
        // ImGui.Text($"alpha: {(inst.GetFloat("alpha"u8, out var alpha) ? alpha : 0)}");

        ImAnim.ProfilerBegin("test"u8);

        ImGui.Text("Hello world.");
        ImGui.Text($"Profiler: {(ImAnim.ProfilerIsEnabled() ? "enabled" : "disabled")}");
        ImGui.Checkbox("Show Demo Window"u8, ref _showDemoWindow);
        if (_showDemoWindow) ImAnim.DemoWindow();

        ImAnim.ProfilerEnd();

        /*
        ImAnimPath.Begin("what"u8, new Vector2(5, 5))
            .LineTo(new Vector2(10, 10))
            .LineTo(new Vector2(10, 5))
            .Close();
        */

        Draw2();
    }

    private void OnBegin(uint instId, void* userData)
    {
        _callbackBeginCount++;
        // _logger.LogDebug("Animation {instId} started!", instId);
    }

    private void OnUpdate(uint instId, void* userData)
    {
        _callbackUpdateCount++;
        // _logger.LogDebug("Animation {instId} updated!", instId);
    }

    private void OnComplete(uint instId, void* userData)
    {
        _callbackCompleteCount++;
        // _logger.LogDebug("Animation {instId} completed!", instId);
    }

    public unsafe void Draw2()
    {
        if (ImGui.Button("Play Bounce"))
        {
            ImAnim.Play("bounce"u8, "bounce-inst"u8);
        }

        ImGui.SameLine();

        var inst = ImAnim.GetInstance("bounce-inst"u8);
        var offset = Vector2.Zero;
        var scale = 1.0f;
        var alpha = 1.0f;

        if (inst.Valid())
        {
            inst.GetVec2("offset"u8, out offset);
            inst.GetFloat("scale"u8, out scale);
            inst.GetFloat("alpha"u8, out alpha);
        }

        // Clamp scale to valid range for SetWindowFontScale
        if (scale < 0.1f) scale = 0.1f;
        if (scale > 10.0f) scale = 10.0f;

        var cur = ImGui.GetCursorPos();
        ImGui.SetCursorPos(cur + offset);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);
        ImGui.SetWindowFontScale(scale);
        ImGui.Text("Bouncing!");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleVar();

        ImGui.Separator();

        if (ImGui.Button("Play with Callbacks"))
        {
            ImAnim.Play("clip-with-callbacks"u8, "callback-inst"u8);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Counters"))
        {
            _callbackBeginCount = 0;
            _callbackUpdateCount = 0;
            _callbackCompleteCount = 0;
        }

        inst = ImAnim.GetInstance("callback-inst"u8);
        scale = 1.0f;

        if (inst.Valid())
        {
            inst.GetFloat("scale"u8, out scale);
        }

        if (scale < 0.1f) scale = 0.1f;
        if (scale > 10.0f) scale = 10.0f;

        ImGui.SameLine();
        ImGui.SetWindowFontScale(scale);
        ImGui.Text("Scaling");
        ImGui.SetWindowFontScale(1.0f);

        ImGui.Text($"on_begin called:    {_callbackBeginCount} times");
        ImGui.Text($"on_update called:   {_callbackUpdateCount} times");
        ImGui.Text($"on_complete called: {_callbackCompleteCount} times");

        ImGui.Separator();

        var vp_size = ImGui.GetWindowViewport().Size;

        var display_size = new Vector2(MathF.Min(vp_size.X * 0.3f, 400.0f), 60);
        var origin = ImGui.GetCursorScreenPos();

        var draw_list = ImGui.GetWindowDrawList();
        draw_list.AddRectFilled(origin, new Vector2(origin.X + display_size.X, origin.Y + display_size.Y), ImGui.ColorConvertFloat4ToU32(new Vector4(50 / 255f, 40 / 255f, 40 / 255f, 255 / 255f)));
        draw_list.AddRect(origin, new Vector2(origin.X + display_size.X, origin.Y + display_size.Y), ImGui.ColorConvertFloat4ToU32(new Vector4(120 / 255f, 80 / 255f, 80 / 255f, 255 / 255f)));

        var id = ImGui.GetID("anchor_viewport");
        // var pos = ImAnim.TweenVec4Rel(id, 0, new Vector4(0.5f, 0.5f, 0.5f, 0.5f), new Vector4(0, 0, 0, 0), 0.5f, ImAnim.EasePreset(ImAnimEaseType.OutCubic), ImAnimPolicy.Crossfade, ImAnimAnchorSpace.Viewport, ImGui.GetIO().DeltaTime);
        // var pos = ImAnim.TweenVec4Resolved(id, 0, (_) => new Vector4(0.5f, 0.5f, 0.5f, 0.5f), null, 0.5f, ImAnim.EasePreset(ImAnimEaseType.OutCubic), ImAnimPolicy.Crossfade, ImGui.GetIO().DeltaTime);
        var pos = ImAnim.TweenVec4Resolved(id, 0, () => new Vector4(0.5f, 0.5f, 0.5f, 0.5f), 0.5f, ImAnim.EasePreset(ImAnimEaseType.OutCubic), ImAnimPolicy.Crossfade, ImGui.GetIO().DeltaTime);

        // Scale position to display size
        var scale_x = display_size.X / vp_size.X;
        var scale_y = display_size.Y / vp_size.Y;
        var draw_x = Math.Clamp(pos.X * scale_x, 10.0f, display_size.X - 10.0f);
        var draw_y = Math.Clamp(pos.Y * scale_y, 10.0f, display_size.Y - 10.0f);
        draw_list.AddCircleFilled(new Vector2(origin.X + draw_x, origin.Y + draw_y), 8.0f, ImGui.ColorConvertFloat4ToU32(new Vector4(255 / 255f, 100 / 255f, 100 / 255f, 255 / 255f)));
        draw_list.AddText(new Vector2(origin.X + 5, origin.Y + 5), ImGui.ColorConvertFloat4ToU32(new Vector4(255 / 255f, 180 / 255f, 180 / 255f, 255 / 255f)), "Viewport Size (scaled preview)");

        ImGui.Dummy(display_size);
        ImGui.Text($"Actual viewport size: ({vp_size.X}, {vp_size.X}), Center pos: ({pos.X}, {pos.Y})");
    }
}
