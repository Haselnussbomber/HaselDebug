using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AtkHandlerCallsTab : DebugTab, IDisposable
{
    private record CallEntry(DateTime Time, uint handlerIndex, Pointer<AtkValue> Values, uint ValueCount);

    private readonly DebugRenderer _debugRenderer;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly List<CallEntry> _calls = [];
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
        {
            FreeAtkValues(new Span<AtkValue>(entry.Values, (int)entry.ValueCount));
        }

        _calls.Clear();
    }

    private AtkValue* CopyAtkValues(Span<AtkValue> values)
    {
        var ptr = (AtkValue*)IMemorySpace.GetDefaultSpace()->Malloc((ulong)sizeof(AtkValue) * (ulong)values.Length, 0x8);
        var valuesCopy = new Span<AtkValue>(ptr, values.Length);
        var valueCountCopy = 0;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values.GetPointer(i);
            var valueCopy = valuesCopy.GetPointer(i);

            if (value->Type == ValueType.Int && i < values.Length - 1 && values.GetPointer(i + 1)->Type == ValueType.AtkValues)
                valueCountCopy = value->Int;
            else if (value->Type != ValueType.AtkValues)
                valueCountCopy = 0;

            valueCopy->Ctor();

            if (value->Type == ValueType.String)
            {
                var str = new ReadOnlySeStringSpan(value->String.Value);
                var strPtr = (byte*)IMemorySpace.GetDefaultSpace()->Malloc((ulong)str.ByteLength + 1, 0x8);
                Marshal.Copy(str.Data.ToArray(), 0, (nint)strPtr, str.ByteLength);
                strPtr[str.ByteLength] = 0;
                valueCopy->SetString(strPtr);
            }
            else if (value->Type == ValueType.AtkValues && valueCountCopy > 0)
            {
                valueCopy->ChangeType(ValueType.AtkValues);
                valueCopy->AtkValues = CopyAtkValues(new Span<AtkValue>(value->AtkValues, valueCountCopy));
            }
            else
            {
                valueCopy->Copy(value);
            }
        }

        return ptr;
    }

    private void FreeAtkValues(Span<AtkValue> values)
    {
        var valueCountCopy = 0;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values.GetPointer(i);

            if (value->Type == ValueType.Int && i < values.Length - 1 && values[i + 1].Type == ValueType.AtkValues)
                valueCountCopy = value->Int;
            else if (value->Type != ValueType.AtkValues)
                valueCountCopy = 0;

            if (value->Type == ValueType.String)
            {
                IMemorySpace.Free(value->String, (ulong)new ReadOnlySeStringSpan(value->String.Value).ByteLength + 1);
                value->ChangeType(ValueType.Undefined);
                value->String = null;
            }
            else if (value->Type == ValueType.AtkValues && valueCountCopy > 0)
            {
                FreeAtkValues(new Span<AtkValue>(value->AtkValues, valueCountCopy));
            }

            value->Dtor();
        }

        IMemorySpace.Free(values.GetPointer(0), (ulong)sizeof(AtkValue) * (ulong)values.Length);
    }

    private AtkValue* CallHandlerDetour(AtkExternalInterface* thisPtr, AtkValue* returnValue, uint handlerIndex, uint valueCount, AtkValue* values)
    {
        if (valueCount > 0)
        {
            _calls.Add(new CallEntry(DateTime.Now, handlerIndex, CopyAtkValues(new Span<AtkValue>(values, (int)valueCount)), valueCount));
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

        if (ImGui.Checkbox("Enabled", ref _enabled))
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

        if (ImGui.Button("Clear"))
        {
            foreach (var entry in _calls)
            {
                FreeAtkValues(new Span<AtkValue>(entry.Values, (int)entry.ValueCount));
            }

            _calls.Clear();
        }

        using var table = ImRaii.Table("CallTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
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
            ImGui.Text(record.handlerIndex switch
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
                _ => record.handlerIndex.ToString()
            });

            ImGui.TableNextColumn();
            _debugRenderer.DrawAtkValues(record.Values, (ushort)record.ValueCount, new() { AddressPath = new((nint)record.Values.Value) });
        }
    }
}
