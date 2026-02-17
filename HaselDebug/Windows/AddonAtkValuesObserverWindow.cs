using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class AddonAtkValuesObserverWindow : SimpleWindow
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly DebugRenderer _debugRenderer;
    private readonly List<AtkValuesCopy> _refreshValues = [];

    private record RefreshEntry(DateTime Time, Pointer<AtkValue> Values, uint ValueCount);

    public string AddonName
    {
        get;
        set { field = value; WindowName = value + " - AtkValues Observer"; }
    }

    public override void Dispose()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, AddonName, OnEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, AddonName, OnEvent);
        base.Dispose();
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

        _addonLifecycle.RegisterListener(AddonEvent.PreSetup, AddonName, OnEvent);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, AddonName, OnEvent);
    }

    public override void OnClose()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, AddonName, OnEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, AddonName, OnEvent);
        base.OnClose();
    }

    private void OnEvent(AddonEvent type, AddonArgs args)
    {
        if (args is AddonSetupArgs addonSetupArgs)
            _refreshValues.Add(new AtkValuesCopy(addonSetupArgs.GetAtkValues()) { AdditionalText = "OnSetup" });

        if (args is AddonRefreshArgs addonRefreshArgs)
            _refreshValues.Add(new AtkValuesCopy(addonRefreshArgs.GetAtkValues()) { AdditionalText = "OnRefresh" });
    }

    public override unsafe void Draw()
    {
        using (ImRaii.Disabled(_refreshValues.Count == 0))
        {
            if (ImGui.Button("Clear"u8))
            {
                foreach (var entry in _refreshValues)
                    entry.Dispose();

                _refreshValues.Clear();
            }
        }

        using var table = ImRaii.Table($"{AddonName}RefreshValuesTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Values"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _refreshValues.Count - 1; i >= 0; i--)
        {
            var record = _refreshValues[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(record.Time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGui.Text(record.AdditionalText);

            ImGui.TableNextColumn();
            _debugRenderer.DrawAtkValues(record.Ptr, (ushort)record.ValueCount, new() { AddressPath = new((nint)record.Ptr.Value) });
        }
    }
}
