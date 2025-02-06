using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;

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

[RegisterSingleton]
public class AddonNameTable : Table<AddonNameEntry>, IDisposable
{
    public AddonNameTable(LanguageProvider languageProvider, DebugRenderer debugRenderer) : base("AddonNameTable", languageProvider)
    {
        Columns = [
            new IndexColumn(debugRenderer) {
                Label = "Index",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new NameColumn(debugRenderer) {
                Label = "Name",
            },
        ];
    }

    public override unsafe void LoadRows()
    {
        Rows = RaptureAtkModule.Instance()->AddonNames.Select((name, index) => new AddonNameEntry(index, name.ToString())).ToList();
    }

    private class IndexColumn(DebugRenderer debugRenderer) : ColumnNumber<AddonNameEntry>
    {
        public override int ToValue(AddonNameEntry row)
        {
            return row.Index;
        }

        public override void DrawColumn(AddonNameEntry row)
        {
            debugRenderer.DrawCopyableText(ToName(row));
        }
    }

    private class NameColumn(DebugRenderer debugRenderer) : ColumnString<AddonNameEntry>
    {
        public override string ToName(AddonNameEntry row)
        {
            return row.Name;
        }

        public override unsafe void DrawColumn(AddonNameEntry row)
        {
            debugRenderer.DrawCopyableText(ToName(row));

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
