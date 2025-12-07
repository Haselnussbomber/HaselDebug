using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class PointerInspectorTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ILogger<PointerInspectorTab> _logger;

    private readonly Dictionary<nint, Type> _virtualTableMappings = [];
    private readonly Dictionary<OffsetInfo, Type> _offsetMappings = [];
    private Task _loadTask;

    private string _addressInput = string.Empty;
    private string _addressSize = string.Empty;

    private uint _memorySize;
    private nint _memoryAddress;

    private record OffsetInfo(nint ActualAddress, int Offset);

    public override void Draw()
    {
        _loadTask ??= Task.Run(PopulateVirtualTableMappings);

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

    private unsafe void ParsePointer(IntPtr address, uint size)
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

        if (numberString.StartsWith("0x") && T.TryParse(numberString[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsedHex))
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

    private void PopulateVirtualTableMappings()
    {
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
}
