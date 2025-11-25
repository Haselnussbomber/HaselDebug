using System.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PermissionsTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;
    private bool _initialized;
    private Dictionary<int, string> _conditionNames;
    private int _hideSetting;

    private void Initialize()
    {
        _conditionNames = typeof(Conditions)
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(fi => fi.FieldType == typeof(bool) && fi.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Select(fi => (fi.GetFieldOffset(), fi.Name))
            .DistinctBy(t => t.Item1)
            .ToDictionary();
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        ImGui.SetNextItemWidth(150);
        ImGui.Combo("##HideSetting", ref _hideSetting, ["Show all", "Hide matching", "Hide non-matching"]);

        var conditions = ConditionEx.Instance();

        foreach (var row in _excelService.GetSheet<CustomPermission>())
        {
            var hasPermission = conditions->HasPermission(row.RowId);

            if (_hideSetting == 1 && hasPermission)
                continue;
            else if (_hideSetting == 2 && !hasPermission)
                continue;

            using var color = ImRaii.PushColor(ImGuiCol.Text, hasPermission ? Color.Green : Color.Red);
            using var node = ImRaii.TreeNode(GetPermissionName(row.RowId), ImGuiTreeNodeFlags.SpanAvailWidth);
            color.Dispose();
            if (!node) continue;

            using var table = ImRaii.Table($"PermissionTable#{row.RowId}", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
            if (!table) continue;

            ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Allowed", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Current", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Matches", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableHeadersRow();

            for (var i = 0; i < row.Conditions.Count; i++)
            {
                var allowed = row.Conditions[i];
                var current = conditions->Flags[i];
                var matches = allowed || !current;

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Index
                ImGui.Text($"{i}");

                ImGui.TableNextColumn(); // Name
                if (_conditionNames.TryGetValue(i, out var conditionName))
                    ImGui.Text($"{conditionName}");

                ImGui.TableNextColumn(); // Allowed
                using (ImRaii.PushColor(ImGuiCol.Text, allowed ? Color.Green : Color.Red))
                    ImGui.Text($"{allowed}");

                ImGui.TableNextColumn(); // Current
                using (ImRaii.PushColor(ImGuiCol.Text, current ? Color.Green : Color.Red))
                    ImGui.Text($"{current}");

                ImGui.TableNextColumn(); // Matches
                using (ImRaii.PushColor(ImGuiCol.Text, matches ? Color.Green : Color.Red))
                    ImGui.Text($"{matches}");
            }
        }
    }

    private string GetPermissionName(uint rowId)
    {
        var name = $"Permission #{rowId}";

        switch (rowId)
        {
            case 150: name += " - Idle Camera"; break;
            case 178: name += " - Group Pose"; break;
        }

        return name;
    }
}

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = Conditions.StructSize)]
public unsafe partial struct ConditionEx
{
    public static ConditionEx* Instance() => (ConditionEx*)Conditions.Instance();

    [FieldOffset(0), FixedSizeArray] internal FixedSizeArray104<bool> _flags;

    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 75 ?? 8B FB")]
    public partial bool HasPermission(uint permissionId, int excludedCondition1 = 0, int excludedCondition2 = 0);
}

[Sheet("Permission")]
public readonly unsafe struct CustomPermission(ExcelPage page, uint offset, uint row) : IExcelRow<CustomPermission>
{
    public uint RowId => row;

    public readonly Collection<bool> Conditions => new(page, offset, offset, &ConditionCtor, 104);
    private static bool ConditionCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadBool(offset + i);

    static CustomPermission IExcelRow<CustomPermission>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
