using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Iced.Intel;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PointerInspectorTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ISigScanner _sigScanner;
    private readonly ILogger<PointerInspectorTab> _logger;

    private readonly Dictionary<nint, Type> _virtualTableMappings = [];
    private readonly Dictionary<OffsetInfo, Type> _offsetMappings = [];
    private Task _loadTask;

    private string _addressInput = string.Empty;
    private string _addressSize = string.Empty;

    private uint _memorySize;
    private nint _memoryAddress;
    private nint _freeMemoryAddress;

    private record OffsetInfo(nint ActualAddress, int Offset);

    public override void Draw()
    {
        _loadTask ??= Task.Run(Load);

        if (!_loadTask.IsCompleted)
        {
            ImGui.Text("Loading...");
            return;
        }

        if (_loadTask.IsFaulted)
        {
            ImGuiUtilsEx.DrawAlertError("TaskError", _loadTask.Exception?.ToString() ?? "Error loading data :(");
            return;
        }

        DrawSearchBox();

        if (_memoryAddress == 0)
            return;

        ImGui.Separator();
        DrawResults();
    }

    private void DrawSearchBox()
    {
        using var table = ImRaii.Table("SearchTable", 3, ImGuiTableFlags.SizingStretchProp);
        if (!table) return;

        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed, 100);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Struct Address");

        ImGui.TableNextColumn();
        ImGui.Text("Struct Size");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##MemoryAddress", ref _addressInput, 20, ImGuiInputTextFlags.AutoSelectAll))
        {
            _memoryAddress = ParseNumericString<nint>(_addressInput);
            FindSize();
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##MemorySize", ref _addressSize, 10, ImGuiInputTextFlags.AutoSelectAll))
            _memorySize = ParseNumericString<uint>(_addressSize);

        ImGui.TableNextColumn();
        if (ImGui.Button("Process", new Vector2(-1, ImGui.GetFrameHeight())))
        {
            if (MemoryUtils.IsPointerValid(_memoryAddress) && _memorySize > 0)
                ParsePointer(_memoryAddress, _memorySize);
        }

        if (_memoryAddress == 0)
            return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(10.0f);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text($"Struct Address: 0x{_memoryAddress:X}\nStruct Size: 0x{_memorySize:X}");
    }

    private void ParsePointer(nint address, uint size)
    {
        _offsetMappings.Clear();

        foreach (var offsetIndex in Enumerable.Range(0, (int)size / 8))
        {
            var offsetAddress = address + offsetIndex * 8;
            if (!MemoryUtils.IsPointerValid(offsetAddress))
                continue;

            var objectPointer = *(nint*)offsetAddress;
            if (!MemoryUtils.IsPointerValid(objectPointer))
                continue;

            var virtualTablePointer = *(nint*)objectPointer;
            if (!MemoryUtils.IsPointerValid(virtualTablePointer))
                continue;

            if (_virtualTableMappings.TryGetValue(virtualTablePointer, out var tableName))
                _offsetMappings.TryAdd(new OffsetInfo(*(nint*)offsetAddress, offsetIndex * 8), tableName);
        }
    }

    private static T? ParseNumericString<T>(string numberString) where T : INumberBase<T>
    {
        if (T.TryParse(numberString, null, out var parsedNumber))
            return parsedNumber;

        if (T.TryParse(numberString.StartsWith("0x") ? numberString[2..] : numberString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsedHex))
            return parsedHex;

        return default;
    }

    private void DrawResults()
    {
        if (!MemoryUtils.IsPointerValid(_memoryAddress))
        {
            ImGui.Text("Invalid pointer.");
            return;
        }

        using var table = ImRaii.Table("OffsetTypeMapping", 2);
        if (!table) return;

        ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 65.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);

        foreach (var ((address, offset), type) in _offsetMappings)
        {
            using var id = ImRaii.PushId(offset.ToString());

            ImGui.TableNextColumn();
            ImGui.Text($"+0x{offset,-6:X}");

            ImGui.TableNextColumn();
            _debugRenderer.DrawPointerType(address, type, new NodeOptions());
        }
    }

    private void Load()
    {
        _freeMemoryAddress = _sigScanner.ScanText("E8 ?? ?? ?? ?? 4D 89 AE") - _sigScanner.Module.BaseAddress;

        foreach (var type in typeof(AtkUnitBase).Assembly.GetTypes())
        {
            if (!type.GetCustomAttributes<VirtualTableAttribute>(false).Any())
                continue;

            var addressesType = type.GetNestedType("Addresses", BindingFlags.Public | BindingFlags.Static);
            if (addressesType == null)
                continue;

            var staticVirtualTableType = addressesType.GetField("StaticVirtualTable", BindingFlags.Public | BindingFlags.Static);
            if (staticVirtualTableType == null)
                continue;

            var address = (Address?)staticVirtualTableType.GetValue(null);
            if (address == null)
                continue;

            if (_virtualTableMappings.TryAdd(address.Value, type))
                _logger.LogDebug("Mapped: {address:X} to {typeName}", address.Value, type.Name);
        }
    }

    private void FindSize()
    {
        if (!MemoryUtils.IsPointerValid(_memoryAddress))
        {
            _logger.LogWarning("FindSize failed at _memoryAddress ({_memoryAddress:X})", _memoryAddress);
            return;
        }

        var vtblPtr = *(nint*)_memoryAddress;
        if (!MemoryUtils.IsPointerValid(vtblPtr))
        {
            _logger.LogWarning("FindSize failed at vtblPtr ({vtblPtr:X})", vtblPtr);
            return;
        }

        var vf0Ptr = *(nint*)vtblPtr;
        if (!MemoryUtils.IsPointerValid(vf0Ptr))
        {
            _logger.LogWarning("FindSize failed at vf0Ptr ({vf0Ptr:X})", vf0Ptr);
            return;
        }

        _logger.LogDebug("Decoding vf0 @ {addr:X}", vf0Ptr);

        var codeReader = new NativeCodeReader(vf0Ptr);
        var decoder = Decoder.Create(64, codeReader);
        decoder.IP = (ulong)(vf0Ptr - _sigScanner.Module.BaseAddress);

        var vtblAddress = nint.Zero;
        var rax = nint.Zero;
        
        var list = new List<Instruction>();

        while (codeReader.CanReadByte)
        {
            decoder.Decode(out var instr);

            if (instr.IsInvalid || instr.FlowControl == FlowControl.Return)
                break;

            list.Add(instr);
        }

        list.Reverse();

        var isFreeMemoryCall = false;
        var remainingInstructions = 0;
        foreach (var instr in list.Take(20))
        {
            _logger.LogTrace("Instruction @ 0x{addr:X}: {instr}", instr.IP, instr.ToString());

            if (instr.Code == Code.Call_rel32_64 && instr.NearBranch32 == _freeMemoryAddress)
            {
                _logger.LogDebug("Found FreeMemory call @ 0x{addr:X}", instr.IP);
                isFreeMemoryCall = true;
                continue;
            }

            if (!isFreeMemoryCall)
                continue;

            if (--remainingInstructions == 0)
                break;

            if (instr.Code == Code.Mov_r32_imm32
                && instr.Op0Kind == OpKind.Register
                && instr.Op0Register == Register.EDX
                && instr.Op1Kind == OpKind.Immediate32)
            {
                _memorySize = instr.Immediate32;
                _addressSize = "0x" + _memorySize.ToString("X");
                _logger.LogDebug("Found struct size {size} @ 0x{addr:X}", _addressSize, instr.IP);
                return;
            }
        }

        _logger.LogDebug("Struct size not found");
    }
}
