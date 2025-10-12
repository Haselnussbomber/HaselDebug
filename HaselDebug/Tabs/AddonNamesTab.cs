using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class AddonNamesTab(AddonNameTable table) : DebugTab
{
    public override void Draw()
    {
        table.Draw();
    }
}

public record AddonNameEntry(int Index, string Name);

[RegisterSingleton, AutoConstruct]
public partial class AddonNameTable : Table<AddonNameEntry>, IDisposable
{
    private readonly IndexColumn _indexColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            _indexColumn,
            _nameColumn,
        ];
    }

    public override unsafe void LoadRows()
    {
        Rows = RaptureAtkModule.Instance()->AddonNames.Select((name, index) => new AddonNameEntry(index, name.ToString())).ToList();
    }

    [RegisterTransient]
    public class IndexColumn : ColumnNumber<AddonNameEntry>
    {
        public IndexColumn()
        {
            LabelKey = "IndexColumn.Label";
            SetFixedWidth(60);
        }

        public override int ToValue(AddonNameEntry row)
        {
            return row.Index;
        }

        public override void DrawColumn(AddonNameEntry row)
        {
            ImGuiUtilsEx.DrawCopyableText(ToName(row));
        }
    }

    [RegisterTransient]
    public class NameColumn : ColumnString<AddonNameEntry>
    {
        public NameColumn()
        {
            LabelKey = "NameColumn.Label";
        }

        public override string ToName(AddonNameEntry row)
        {
            return row.Name;
        }

        public override unsafe void DrawColumn(AddonNameEntry row)
        {
            ImGuiUtilsEx.DrawCopyableText(ToName(row));

            if (ImGui.IsItemClicked())
            {
                var values = stackalloc AtkValue[3];
                values[0].SetManagedString("Test");
                values[1].SetUInt(0);
                RaptureAtkModule.Instance()->OpenAddon((uint)row.Index, 2, values, null, 0, 0, 0);
            }
        }
    }
}
