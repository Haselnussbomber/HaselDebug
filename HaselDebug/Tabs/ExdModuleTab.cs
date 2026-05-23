using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Data.Files.Excel;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ExdModuleTab : DebugTab
{
    private readonly ILogger<ExdModuleTab> _logger;
    private readonly DebugRenderer _debugRenderer;
    private readonly IDataManager _dataManager;
    private ExcelListFile _excelListFile;

    [AutoPostConstruct]
    private void Initialize()
    {
        _excelListFile = _dataManager.GetFile<ExcelListFile>("exd/root.exl")!;
    }

    public override void Draw()
    {
        using var table = ImRaii.Table("ExdModuleTable"u8, 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Sheet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (name, index) in _excelListFile.ExdMap.OrderBy(kv => kv.Value))
        {
            if (index == -1) continue;

            var sheet = Framework.Instance()->ExdModule->GetSheetByIndex((uint)index);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.Text(index.ToString());

            ImGui.TableNextColumn(); // Sheet
            _debugRenderer.DrawPointerType(sheet, new NodeOptions() { Title = name });
        }
    }
}
