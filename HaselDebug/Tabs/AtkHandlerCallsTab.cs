using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AtkHandlerCallsTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly List<AtkValuesCopy> _calls = [];
    private Hook<AtkExternalInterface.Delegates.CallHandler>? _callHandlerDetour;
    private bool _enabled = false;
    private bool _isInitialized;

    private void Initialize()
    {
        _callHandlerDetour = _gameInteropProvider.HookFromSignature<AtkExternalInterface.Delegates.CallHandler>(
            "40 53 48 83 EC ?? 48 8B 81 ?? ?? ?? ?? 48 8B DA 48 8B 91 ?? ?? ?? ?? 48 2B C2 45 8B D0",
            CallHandlerDetour);
    }

    public void Dispose()
    {
        _callHandlerDetour?.Dispose();

        foreach (var entry in _calls)
            entry.Dispose();

        _calls.Clear();
    }

    private AtkValue* CallHandlerDetour(AtkExternalInterface* thisPtr, AtkValue* returnValue, uint handlerIndex, uint valueCount, AtkValue* values)
    {
        if (valueCount > 0)
        {
            var additionalText = handlerIndex switch
            {
                1 => "UnregisterAddonCallback",
                2 => "AddonAgentCallback",
                3 => "AddonEventCallback",
                4 => "AddonEventCallback2",
                5 => "SubscribeAtkArrayData",
                6 => "UnsubscribeAtkArrayData",
                11 => "SetCursor",
                14 => "OpenMapWithMapLink",
                17 => "SaveAddonConfig",
                21 => "PlaySoundEffect",
                22 => "ExecuteHotbarSlot",
                23 => "SetHotbarSlot",
                24 => "ItemMove",
                25 => "LootRoll",
                26 => "SellStack",
                27 => "SetBattleMode",
                28 => "OpenInventory",
                29 => "OpenItemContextMenu",
                31 => "NameplateHover",
                32 => "ShowDetailAddon",
                33 => "FormatText",
                41 => "ExecuteMainCommand",
                42 => "IsMainCommandUnlocked",
                44 => "NowLoading",
                45 => "ItemDiscard",
                49 => "WorldToScreenPoint",
                50 => "ScdResource",
                52 => "CloseTryOn",
                53 => "OpenContextMenuForAddon",
                56 => "GlassesDrop",
                _ => handlerIndex.ToString()
            };

            _calls.Add(new AtkValuesCopy(new Span<AtkValue>(values, (int)valueCount)) { AdditionalText = additionalText });
        }

        return _callHandlerDetour!.Original(thisPtr, returnValue, handlerIndex, valueCount, values);
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        if (_callHandlerDetour == null)
        {
            ImGui.Text("Hook not created"u8);
            return;
        }

        if (ImGui.Checkbox("Enabled"u8, ref _enabled))
        {
            if (_enabled && !_callHandlerDetour.IsEnabled)
            {
                _callHandlerDetour.Enable();
            }
            else if (!_enabled && _callHandlerDetour.IsEnabled)
            {
                _callHandlerDetour.Disable();
            }
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(_calls.Count == 0))
        {
            if (ImGui.Button("Clear"u8))
            {
                foreach (var entry in _calls)
                    entry.Dispose();

                _calls.Clear();
            }
        }

        using var table = ImRaii.Table("CallTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Handler"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Values"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _calls.Count - 1; i >= 0; i--)
        {
            var record = _calls[i];

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
