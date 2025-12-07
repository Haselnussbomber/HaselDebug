using System.Globalization;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Task = System.Threading.Tasks.Task;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class PointerInspectorTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ILogger<PointerInspectorTab> _logger;

    private readonly Dictionary<nint, Type> _virtualTableMappings = [];
    private bool _isReady;

    private string _addressInput = string.Empty;
    private string _addressSize = string.Empty;

    private uint _memorySize;
    private nint _memoryAddress;

    private record OffsetInfo(nint ActualAddress, int Offset);
    private readonly Dictionary<OffsetInfo, Type> _offsetMappings = [];

    [AutoPostConstruct]
    private void Initialize()
    {
        Task.Run(PopulateVirtualTableMappings);
    }

    public override void Draw()
    {
        if (!_isReady) 
            ImGui.Text("Parsing VirtualTable Addresses, please wait...");
        else
        {
            DrawSearchBox();
            ImGui.Separator();
            DrawResults();
        }
    }

    private void DrawSearchBox()
    {
        using var child = ImRaii.Child("PointerInspectorChild", new Vector2(ImGui.GetContentRegionAvail().X, 105.0f * ImGuiHelpers.GlobalScale));
        if (!child) return;
        
        using var table = ImRaii.Table("SearchTable", 2, ImGuiTableFlags.SizingStretchProp);
        if (!table) return;
        
        ImGui.TableSetupColumn("##first", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("##second", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        
        ImGui.TableNextColumn();
        ImGui.Text("Struct Address");

        ImGui.TableNextColumn();
        ImGui.Text("Struct Size");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X);
        if (ImGui.InputTextWithHint("##Memory Address", "Struct Address", ref _addressInput, 20)) 
            _memoryAddress = ParseNumericString<nint>(_addressInput);

        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X);
        if (ImGui.InputTextWithHint("##Memory Size", "Struct Size", ref _addressSize, 10))
            _memorySize = ParseNumericString<uint>(_addressSize);
        
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(10.0f);
        
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text($"Struct Address: 0x{_memoryAddress:X}\nStruct Size: 0x{_memorySize:X}");
        
        ImGui.TableNextColumn();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 125.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button("Process", ImGuiHelpers.ScaledVector2(125.0f, 24.0f)))
        {
            if (_memoryAddress != nint.Zero && _memoryAddress.IsValid() && _memorySize > 0) 
                ParsePointer(_memoryAddress, _memorySize);
        }
    }

    private unsafe void ParsePointer(IntPtr address, uint size)
    {
        _offsetMappings.Clear();

        foreach (var offsetIndex in Enumerable.Range(0, (int)size / 8))
        {
            var offsetAddress = address + offsetIndex * 8;
            if (!offsetAddress.IsValid()) continue;
            
            var objectPointer = *(nint*)offsetAddress;
            if (objectPointer == nint.Zero || !objectPointer.IsValid()) continue;
            
            var virtualTablePointer = *(nint*)objectPointer;
            if (virtualTablePointer == nint.Zero || !virtualTablePointer.IsValid()) continue;
                
            if (_virtualTableMappings.TryGetValue(virtualTablePointer, out var tableName)) 
                _offsetMappings.TryAdd(new OffsetInfo(*(nint*)offsetAddress, offsetIndex * 8) , tableName);
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
        using var table = ImRaii.Table("OffsetTypeMapping", 2);
        if (!table) return;
        
        ImGui.TableSetupColumn("##offset", ImGuiTableColumnFlags.WidthFixed, 65.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##type", ImGuiTableColumnFlags.WidthStretch);
        
        foreach (var ((address, offset), type) in _offsetMappings)
        {
            using var id = ImRaii.PushId(offset.ToString());
            
            ImGui.TableNextColumn();
            ImGui.Text($"+0x{offset, -6:X}");
            
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

            if (!_virtualTableMappings.TryAdd(address.Value, type))
                _logger.LogWarning("Duplicate found for {typeName}", type.Name);
            else
                _logger.LogDebug("Mapped: {address:X} to {typeName}", address.Value, type.Name);
        }

        _isReady = true;
    }
}
