using System.Text;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class InstalledPluginsTab : DebugTab
{
    private readonly InstalledPluginsTable _table;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        _table.LoadRows();
        DrawHeader();
        ImGui.Separator();
        _table.Draw();
        _table.UpdateRows();
    }

    private void DrawHeader()
    {
        if (ImGui.Button("Copy to clipboard"))
            ImGui.SetClipboardText(_table.GetExport());

        ImGui.SameLine();

        var stats = _table.GetStats();
        if (stats.Visible == stats.Total)
        {
            ImGui.Text($"{stats.Total} installed - {stats.Loaded} loaded - {stats.ThirdParty} third-party - {stats.Bad} bad");
        }
        else
        {
            ImGui.Text($"{stats.Visible} shown of {stats.Total} installed - {stats.Loaded} loaded - {stats.ThirdParty} third-party - {stats.Bad} bad");
        }
    }
}

[Flags]
public enum BadPluginFlags
{
    None = 0,
    Outdated = 1 << 0,
    Decommissioned = 1 << 1,
    Banned = 1 << 2,
}

[RegisterSingleton, AutoConstruct]
public partial class InstalledPluginsTable : Table<IExposedPlugin>
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly InternalNameColumn _internalNameColumn;
    private readonly NameColumn _nameColumn;
    private readonly IsLoadedColumn _isLoadedColumn;
    private readonly IsDevColumn _isDevColumn;
    private readonly IsThirdPartyColumn _isThirdPartyColumn;
    private readonly IsBadColumn _isBadColumn;
    private readonly VersionColumn _versionColumn;
    private readonly MainUiColumn _mainUiColumn;
    private readonly ConfigUiColumn _configUiColumn;
    private readonly SourceColumn _sourceColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            _internalNameColumn,
            _nameColumn,
            _isLoadedColumn,
            _isDevColumn,
            _isThirdPartyColumn,
            _isBadColumn,
            _versionColumn,
            _mainUiColumn,
            _configUiColumn,
            _sourceColumn,
        ];
    }

    public override float CalculateLineHeight() => ImGui.GetFrameHeightWithSpacing(); // because of the buttons

    public override void LoadRows() => Rows = [.. _pluginInterface.InstalledPlugins];

    private List<IExposedPlugin> _displayRows = [];

    public void UpdateRows()
        => _displayRows = FilteredRows?.ToList() ?? [.. GetVisibleRows()];

    public IEnumerable<IExposedPlugin> GetVisibleRows()
        => Rows.Where(row => Columns.All(column => column.ShouldShow(row)));

    public InstalledPluginsStats GetStats()
    {
        var visible = GetVisibleRows().ToList();

        return new InstalledPluginsStats(
            Total: Rows.Count,
            Visible: visible.Count,
            Loaded: visible.Count(plugin => plugin.IsLoaded),
            ThirdParty: visible.Count(plugin => plugin.IsThirdParty),
            Bad: visible.Count(plugin => GetBadFlags(plugin) != BadPluginFlags.None));
    }

    public string GetExport()
    {
        var rows = _displayRows.Count > 0 ? _displayRows : [.. GetVisibleRows().OrderBy(plugin => plugin.InternalName)];

        var sb = new StringBuilder();
        sb.AppendLine("InternalName,Name,IsLoaded,IsDev,IsThirdParty,IsBad,Version,HasMainUi,HasConfigUi,Source");

        foreach (var row in rows)
        {
            sb.Append(row.InternalName);
            sb.Append(',');
            sb.Append(row.Name);
            sb.Append(',');
            sb.Append(row.IsLoaded ? "Yes" : "No");
            sb.Append(',');
            sb.Append(row.IsDev ? "Yes" : "No");
            sb.Append(',');
            sb.Append(row.IsThirdParty ? "Yes" : "No");
            sb.Append(',');
            sb.Append(ToBadFlagsString(row));
            sb.Append(',');
            sb.Append(row.Version.ToString());
            sb.Append(',');
            sb.Append(row.HasMainUi ? "Yes" : "No");
            sb.Append(',');
            sb.Append(row.HasConfigUi ? "Yes" : "No");
            sb.Append(',');
            sb.AppendLine(row.Manifest.InstalledFromUrl ?? string.Empty);
        }

        return sb.ToString();
    }

    public static BadPluginFlags GetBadFlags(IExposedPlugin plugin)
    {
        var flags = BadPluginFlags.None;

        if (plugin.IsOutdated)
            flags |= BadPluginFlags.Outdated;

        if (plugin.IsDecommissioned)
            flags |= BadPluginFlags.Decommissioned;

        if (plugin.IsBanned)
            flags |= BadPluginFlags.Banned;

        return flags;
    }

    public static string ToBadFlagsString(IExposedPlugin plugin)
    {
        var flags = GetBadFlags(plugin);
        if (flags == BadPluginFlags.None)
            return string.Empty;

        return string.Join(", ", Enum.GetValues<BadPluginFlags>()
            .Where(flag => flag != BadPluginFlags.None && flags.HasFlag(flag))
            .Select(flag => flag.ToString()));
    }

    [RegisterTransient]
    public class InternalNameColumn : ColumnString<IExposedPlugin>
    {
        public InternalNameColumn()
        {
            AutoLabel = false;
            Label = "InternalName";
            SetFixedWidth(140);
        }

        public override string ToName(IExposedPlugin row)
            => row.InternalName;

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtils.DrawCopyableText(ToName(row));
        }
    }

    [RegisterTransient]
    public class NameColumn : ColumnString<IExposedPlugin>
    {
        public NameColumn()
        {
            AutoLabel = false;
            Label = "Name";
            SetStretchWidth();
        }

        public override string ToName(IExposedPlugin row)
            => row.Name;

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtils.DrawCopyableText(ToName(row));
        }
    }

    [RegisterTransient]
    public class IsLoadedColumn : ColumnYesNo<IExposedPlugin>
    {
        public IsLoadedColumn()
        {
            AutoLabel = false;
            Label = "IsLoaded";
            SetFixedWidth(75);
        }

        public override bool ToBool(IExposedPlugin row)
            => row.IsLoaded;

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            base.DrawColumn(row);
        }
    }

    [RegisterTransient]
    public class IsDevColumn : ColumnYesNo<IExposedPlugin>
    {
        public IsDevColumn()
        {
            AutoLabel = false;
            Label = "IsDev";
            SetFixedWidth(60);
        }

        public override bool ToBool(IExposedPlugin row)
            => row.IsDev;

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            base.DrawColumn(row);
        }
    }

    [RegisterTransient]
    public class IsThirdPartyColumn : ColumnYesNo<IExposedPlugin>
    {
        public IsThirdPartyColumn()
        {
            AutoLabel = false;
            Label = "IsThirdParty";
            SetFixedWidth(95);
        }

        public override bool ToBool(IExposedPlugin row)
            => row.IsThirdParty;

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            base.DrawColumn(row);
        }
    }

    [RegisterTransient]
    public class IsBadColumn : ColumnFlags<BadPluginFlags, IExposedPlugin>
    {
        private BadPluginFlags _filterValue;

        public IsBadColumn()
        {
            AutoLabel = false;
            Label = "IsBad";
            SetFixedWidth(100);
            AllFlags = Enum.GetValues<BadPluginFlags>().Aggregate((a, b) => a | b) & ~BadPluginFlags.None;
            _filterValue = AllFlags;
        }

        public override BadPluginFlags FilterValue => _filterValue;

        public override bool ShouldShow(IExposedPlugin row)
        {
            var value = GetBadFlags(row);

            if (value == BadPluginFlags.None)
                return _filterValue == AllFlags;

            return Enum.GetValues<BadPluginFlags>().Any(flag => flag != BadPluginFlags.None && value.HasFlag(flag) && _filterValue.HasFlag(flag));
        }

        public override void DrawColumn(IExposedPlugin row)
        {
            var text = ToBadFlagsString(row);
            if (string.IsNullOrEmpty(text))
                return;

            ImGui.AlignTextToFramePadding();

            using (ImRaii.PushColor(ImGuiCol.Text, Color.Red.ToUInt()))
                ImGuiUtils.DrawCopyableText(text);
        }

        public override int Compare(IExposedPlugin lhs, IExposedPlugin rhs)
            => GetBadFlags(lhs).CompareTo(GetBadFlags(rhs));

        public override void SetValue(BadPluginFlags value, bool enable)
        {
            if (enable)
                _filterValue |= value;
            else
                _filterValue &= ~value;
        }
    }

    [RegisterTransient]
    public class VersionColumn : ColumnString<IExposedPlugin>
    {
        public VersionColumn()
        {
            AutoLabel = false;
            Label = "Version";
            SetFixedWidth(80);
        }

        public override string ToName(IExposedPlugin row)
            => row.Version.ToString();

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtils.DrawCopyableText(ToName(row));
        }
    }

    [RegisterTransient]
    public class MainUiColumn : Column<IExposedPlugin>
    {
        public MainUiColumn()
        {
            AutoLabel = false;
            Label = "MainUi";
            Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort;
            Width = 70;
        }

        public override void DrawColumn(IExposedPlugin row)
        {
            using (ImRaii.Disabled(!row.HasMainUi))
            {
                if (ImGui.Button("Main UI"))
                    row.OpenMainUi();
            }
        }
    }

    [RegisterTransient]
    public class ConfigUiColumn : Column<IExposedPlugin>
    {
        public ConfigUiColumn()
        {
            AutoLabel = false;
            Label = "ConfigUi";
            Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort;
            Width = 70;
        }

        public override void DrawColumn(IExposedPlugin row)
        {
            using (ImRaii.Disabled(!row.HasConfigUi))
            {
                if (ImGui.Button("Config UI"))
                    row.OpenConfigUi();
            }
        }
    }

    [RegisterTransient]
    public class SourceColumn : ColumnString<IExposedPlugin>
    {
        public SourceColumn()
        {
            AutoLabel = false;
            Label = "Source";
            SetStretchWidth();
        }

        public override string ToName(IExposedPlugin row)
            => row.Manifest.InstalledFromUrl ?? string.Empty;

        public override void DrawColumn(IExposedPlugin row)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtils.DrawCopyableText(ToName(row));
        }
    }
}

public readonly record struct InstalledPluginsStats(int Total, int Visible, int Loaded, int ThirdParty, int Bad);
