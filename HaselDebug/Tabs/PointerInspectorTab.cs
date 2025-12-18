using System.Globalization;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Services.Data;
using HaselDebug.Utils;
using Iced.Intel;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PointerInspectorTab : DebugTab
{
    private readonly DataYmlService _dataYml;
    private readonly TypeService _typeService;
    private readonly DebugRenderer _debugRenderer;
    private readonly ISigScanner _sigScanner;
    private readonly ILogger<PointerInspectorTab> _logger;

    private readonly List<OffsetInfo> _offsetMappings = [];
    private OffsetInfo? _currentStructInfo;
    private OrderedDictionary<int, (string, Type?)>? _currentStructFields;

    private nint? _freeMemoryAddress;

    private string _addressInput = string.Empty;
    private string _addressSize = string.Empty;

    private nint _memoryAddress;
    private uint _memorySize;

    private record OffsetInfo
    {
        public nint Address { get; set; }
        public nint ResolvedAddress { get; set; }
        public int Offset { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public Type? Type { get; set; }
    }

    public override void Draw()
    {
        _freeMemoryAddress ??= _sigScanner.ScanText("E8 ?? ?? ?? ?? 48 89 5D ?? 48 8B 74 24") - _sigScanner.Module.BaseAddress;

        DrawSearchBox();

        if (_memoryAddress == 0)
            return;

        ImGui.Separator();
        DrawResults();
    }

    private void DrawSearchBox()
    {
        using var table = ImRaii.Table("SearchTable", 2, ImGuiTableFlags.SizingStretchProp);
        if (!table) return;

        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthStretch, 1);

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
            ParsePointer();
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##MemorySize", ref _addressSize, 10, ImGuiInputTextFlags.AutoSelectAll))
        {
            _memorySize = ParseNumericString<uint>(_addressSize);
            ParsePointer();
        }

        if (_memoryAddress == 0)
            return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (_currentStructInfo != null)
        {
            ImGui.Text("Struct:");
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText(_currentStructInfo.ClassName, new() { TextColor = DebugRenderer.ColorTreeNode });
        }

        ImGui.Text("Struct Address:");
        ImGui.SameLine();
        _debugRenderer.DrawAddress(_memoryAddress);

        ImGui.Text("Struct Size:");
        ImGui.SameLine();
        ImGuiUtils.DrawCopyableText($"0x{_memorySize:X}");
    }

    private void ParsePointer()
    {
        _offsetMappings.Clear();
        _currentStructInfo = null;
        _currentStructFields = null;

        if (!MemoryUtils.IsPointerValid(_memoryAddress) || _memorySize == 0)
            return;

        var vtablePtr = *(nint*)_memoryAddress;
        if (MemoryUtils.IsPointerValid(vtablePtr))
        {
            foreach (var (name, cl) in _dataYml.Data.Classes)
            {
                if (cl == null || cl.VirtualTables == null || cl.VirtualTables.Count == 0)
                    continue;

                if (cl.VirtualTables.First().Address != vtablePtr - _sigScanner.Module.BaseAddress)
                    continue;

                _logger.LogDebug("Found struct {name} vtbl at {add:X}", name, vtablePtr);

                var csType = GetCSTypeByName(name);

                _currentStructInfo = new OffsetInfo()
                {
                    Address = _memoryAddress,
                    ResolvedAddress = vtablePtr,
                    ClassName = name,
                    Type = csType,
                };

                if (csType != null)
                {
                    _currentStructFields = [];
                    AtkDebugRenderer.LoadTypeMapping(_currentStructFields, "", 0, csType);
                }

                break;
            }
        }

        foreach (var offsetIndex in Enumerable.Range(0, (int)_memorySize / 8))
        {
            var offsetAddress = _memoryAddress + offsetIndex * 8;
            if (!MemoryUtils.IsPointerValid(offsetAddress))
                continue;

            var objectPointer = *(nint*)offsetAddress;
            if (!MemoryUtils.IsPointerValid(objectPointer))
                continue;

            var virtualTablePointer = *(nint*)objectPointer;
            if (!MemoryUtils.IsPointerValid(virtualTablePointer))
                continue;

            var foundInFields = false;
            var foundInDataYml = false;

            var offsetInfo = new OffsetInfo()
            {
                Address = offsetAddress,
                ResolvedAddress = objectPointer,
                Offset = offsetIndex * 8,
            };

            if (_currentStructFields != null && _currentStructFields.TryGetValue(offsetIndex * 8, out var fieldInfo) && fieldInfo.Item2 != null)
            {
                if (!fieldInfo.Item2.IsPointer)
                    continue;

                offsetInfo.FieldName = fieldInfo.Item1;
                offsetInfo.Type = fieldInfo.Item2;
                foundInFields = true;
            }

            if (!foundInFields)
            {
                foreach (var (name, cl) in _dataYml.Data.Classes)
                {
                    if (cl == null || cl.VirtualTables == null || cl.VirtualTables.Count == 0)
                        continue;

                    if (cl.VirtualTables.First().Address != virtualTablePointer - _sigScanner.Module.BaseAddress)
                        continue;

                    _logger.LogDebug("Found {name} vtbl at {add:X}", name, virtualTablePointer);
                    offsetInfo.ClassName = name;
                    offsetInfo.Type = GetCSTypeByName(name)?.MakePointerType();
                    foundInDataYml = true;
                    break;
                }
            }

            if (foundInFields || foundInDataYml)
                _offsetMappings.Add(offsetInfo);
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

        foreach (var info in _offsetMappings)
        {
            using var id = ImRaii.PushId(info.Offset.ToString());

            ImGuiUtils.DrawCopyableText($"[0x{info.Offset:X}]", new()
            {
                CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"0x{info.Offset:X}" : $"{info.Address + info.Offset:X}",
                TextColor = Color.Grey3
            });

            ImGui.SameLine();

            if (info.Type != null)
            {
                ImGuiUtils.DrawCopyableText(info.Type.ReadableTypeName(), new()
                {
                    CopyText = info.Type.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)),
                    TextColor = DebugRenderer.ColorType
                });
                ImGui.SameLine();
            }
            else if (!string.IsNullOrEmpty(info.ClassName))
            {
                ImGuiUtils.DrawCopyableText(info.ClassName + "*", new() { TextColor = DebugRenderer.ColorType });
                ImGui.SameLine();
            }

            if (!string.IsNullOrEmpty(info.FieldName))
            {
                ImGui.TextColored(DebugRenderer.ColorFieldName, info.FieldName);
                ImGui.SameLine();
            }

            if (info.Type != null)
            {
                _debugRenderer.DrawPointerType(info.Address, info.Type, new NodeOptions());
            }
            else
            {
                _debugRenderer.DrawAddress(info.ResolvedAddress);
            }
        }
    }

    private Type? GetCSTypeByName(string name)
    {
        if (_typeService.CSTypes == null)
            return null;

        if (!_typeService.CSTypes.TryGetValue("FFXIVClientStructs.FFXIV." + name.Replace("::", "."), out var type))
            return null;

        return type;
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
