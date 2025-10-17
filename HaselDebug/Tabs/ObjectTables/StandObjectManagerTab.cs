using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class StandObjectManagerTab : DebugTab, IDebugTab, IObjectTableTab
{
    private readonly ObjectTableRenderer _objectTableRenderer;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("StandObjectManagerTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("StandObjectManagerTabBar");
        if (!tabs) return;

        DrawEventNpcsTable();
        DrawEventObjsTable();
    }

    public void DrawEventNpcsTable()
    {
        using var tab = ImRaii.TabItem("EventNpcs");
        if (!tab) return;

        _objectTableRenderer.Draw("StandObjectManager_EventNpcs", [..
            StandObjectManager.Instance()->Characters.ToArray()
                .Select((entry, i) => (Index: i, Entry: entry))
                .Where(tuple => tuple.Entry.Character != null && tuple.Entry.ObjectKind != ObjectKind.None)
                .Select(tuple => (tuple.Index, (Pointer<GameObject>)(GameObject*)tuple.Entry.Character))]);
    }

    public void DrawEventObjsTable()
    {
        using var tab = ImRaii.TabItem("EventObjs");
        if (!tab) return;

        _objectTableRenderer.Draw("StandObjectManager_EventObjs", [..
            StandObjectManager.Instance()->EventObjects.ToArray()
                .Select((ptr, i) => (Index: i, Pointer: ptr))
                .Where(tuple => tuple.Pointer.Value != null)
                .Select(tuple => (tuple.Index, (Pointer<GameObject>)(GameObject*)tuple.Pointer.Value))]);
    }
}
