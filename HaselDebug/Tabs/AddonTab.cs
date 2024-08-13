using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ExdSheets;
using ExdSheets.Sheets;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AddonTab : DebugTab
{
    private readonly TextService TextService;
    private readonly DebugRenderer DebugRenderer;
    private readonly Module ExdModule;
    private readonly Addon[] Rows;
    private Addon[]? FilteredRows;
    private CancellationTokenSource? FilterCTS;
    private string SearchTerm = string.Empty;

    public AddonTab(TextService textService, DebugRenderer debugRenderer, Module exdModule)
    {
        TextService = textService;
        DebugRenderer = debugRenderer;
        ExdModule = exdModule;

        Rows = ExdModule.GetSheet<Addon>().ToArray();
    }

    public override bool DrawInChild => false;
    public override void Draw()
    {
        using var hostChild = ImRaii.Child("Host", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        ImGui.SetNextItemWidth(-1);
        var searchTermChanged = ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref SearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        if (searchTermChanged)
        {
            FilterCTS?.Cancel();
            FilterCTS = new();
            Task.Run(() => FilterList(FilterCTS.Token));
        }

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        using var table = ImRaii.Table("AddonTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        /*
        var sheet = ExdModule.GetSheet<Addon>();

        var imGuiListClipperPtr = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        imGuiListClipperPtr.Begin(sheet.Count, ImGui.GetTextLineHeightWithSpacing());

        while (imGuiListClipperPtr.Step())
        {
            foreach (var row in sheet.Skip(imGuiListClipperPtr.DisplayStart).Take(imGuiListClipperPtr.DisplayEnd - imGuiListClipperPtr.DisplayStart))
            {
                DrawRow(row);
            }
        }

        imGuiListClipperPtr.End();
        imGuiListClipperPtr.Destroy();
        */

        ImGuiClip.ClippedDraw(FilteredRows ?? Rows, DrawRow, ImGui.GetTextLineHeightWithSpacing());
    }

    private void FilterList(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            FilteredRows = null;
            return;
        }

        var list = new List<Addon>();

        for (var i = 0; i < Rows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = Rows[i];
            if (row.RowId.ToString().Contains(SearchTerm)
             || row.Text.ToString().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
             || row.Text.ExtractText().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                list.Add(row);
            }
        }

        FilteredRows = list.ToArray();
    }

    private void DrawRow(Addon row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Text
        DebugRenderer.DrawSeStringSelectable(row.Text.AsSpan(), new NodeOptions()
        {
            AddressPath = new AddressPath((nint)row.RowId),
            RenderSeString = false,
            Title = $"Addon#{row.RowId}"
        });
    }
}
