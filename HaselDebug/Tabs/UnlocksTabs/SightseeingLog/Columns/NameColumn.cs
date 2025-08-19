using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<AdventureEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly MapService _mapService;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly ITextureProvider _textureProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(AdventureEntry entry)
        => entry.Row.Name.ToString();

    public override unsafe void DrawColumn(AdventureEntry entry)
    {
        _debugRenderer.DrawIcon((uint)entry.Row.IconList);

        if (AgentLobby.Instance()->IsLoggedIn)
        {
            var clicked = ImGui.Selectable(ToName(entry));

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (clicked)
                _mapService.OpenMap(entry.Row.Level.Value);
        }
        else
        {
            ImGui.Text(ToName(entry));
        }

        if (_textureProvider.TryGetFromGameIcon(entry.Row.IconDiscovered, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
        {
            // cool, image preloaded! now the tooltips don't flicker...
        }

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawAdventureTooltip(entry.Index, entry.Row);
        }
    }
}
