using Dalamud.Game.Text.SeStringHandling;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using static HaselCommon.Utils.GfdFile;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GaijiFontdataTab : DebugTab
{
    private readonly GaijiFontdataTable _table;

    public override void Draw()
    {
        _table.Draw();
    }
}

[RegisterSingleton, AutoConstruct]
public partial class GaijiFontdataTable : Table<GfdEntry>, IDisposable
{
    private readonly GfdService _gfdService;
    private readonly IdColumn _idColumn;
    private readonly IconColumn _iconColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            _idColumn,
            _iconColumn,
            _nameColumn,
            new PropertyColumn("Top", entry => entry.Top),
            new PropertyColumn("Left", entry => entry.Left),
            new PropertyColumn("Width", entry => entry.Width),
            new PropertyColumn("Height", entry => entry.Height),
            new PropertyColumn("Unk0A", entry => entry.Unk0A),
            new PropertyColumn("Redirect", entry => entry.Redirect),
            new PropertyColumn("Unk0E", entry => entry.Unk0E),
        ];
    }

    public override float CalculateLineHeight()
    {
        return 20 + ImGui.GetStyle().ItemSpacing.Y;
    }

    public override unsafe void LoadRows()
    {
        Rows = _gfdService.Entries.ToArray().ToList();
    }

    [RegisterTransient]
    public class IdColumn : ColumnNumber<GfdEntry>
    {
        public IdColumn()
        {
            Label = "Id";
            Flags = ImGuiTableColumnFlags.WidthFixed;
            Width = 60;
        }

        public override int ToValue(GfdEntry row)
        {
            return row.Id;
        }

        public override void DrawColumn(GfdEntry row)
        {
            ImGuiUtilsEx.DrawCopyableText(ToName(row));
        }
    }

    [RegisterTransient, AutoConstruct]
    public partial class IconColumn : Column<GfdEntry>
    {
        private readonly GfdService _gfdService;

        [AutoPostConstruct]
        public void Initialize()
        {
            Label = "Icon";
            Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort;
            Width = 64;
        }

        public override void DrawColumn(GfdEntry row)
        {
            if (row.Height == 0)
            {
                ImGui.Dummy(new Vector2(20));
                return;
            }

            _gfdService.Draw(row.Id, 20);

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                _gfdService.Draw(row.Id, row.Height * 2);
            }
        }
    }

    [RegisterTransient]
    public class NameColumn : ColumnString<GfdEntry>
    {
        public NameColumn()
        {
            Label = "BitmapFontIcon Name";
            Flags = ImGuiTableColumnFlags.WidthStretch;
        }

        public override string ToName(GfdEntry row)
        {
            return Enum.GetName((BitmapFontIcon)row.Id) ?? string.Empty;
        }

        public override void DrawColumn(GfdEntry row)
        {
            ImGuiUtilsEx.DrawCopyableText(ToName(row));
        }
    }

    public class PropertyColumn : ColumnNumber<GfdEntry>
    {
        private readonly Func<GfdEntry, int> _getValue;

        public PropertyColumn(string label, Func<GfdEntry, int> getValue)
        {
            Label = label;
            _getValue = getValue;
            Flags = ImGuiTableColumnFlags.WidthFixed;
            Width = 70;
        }

        public override int ToValue(GfdEntry row)
        {
            return _getValue(row);
        }

        public override void DrawColumn(GfdEntry row)
        {
            ImGuiUtilsEx.DrawCopyableText(ToName(row));
        }
    }
}
