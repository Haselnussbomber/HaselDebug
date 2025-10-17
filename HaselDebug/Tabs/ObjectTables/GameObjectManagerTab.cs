using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GameObjectManagerTab : DebugTab, IObjectTableTab
{
    private readonly ObjectTableRenderer _objectTableRenderer;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("GameObjectManagerTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("GameObjectManagerTabBar");
        if (!tabs) return;

        DrawIndexSortedTable();
        DrawGameObjectIdSortedTable();
        DrawEntityIdSortedTable();
    }

    public void DrawIndexSortedTable()
    {
        using var tab = ImRaii.TabItem("IndexSorted");
        if (!tab) return;

        var manager = GameObjectManager.Instance();

        _objectTableRenderer.Draw("ObjectTable", [.. manager->Objects.IndexSorted.ToArray()
            .Select((entry, i) => (Index: i, Pointer: (Pointer<GameObject>)entry.Value))
            .Where(tuple => tuple.Pointer.Value != null)]);
    }

    public void DrawGameObjectIdSortedTable()
    {
        using var tab = ImRaii.TabItem("GameObjectIdSorted");
        if (!tab) return;

        var manager = GameObjectManager.Instance();

        _objectTableRenderer.Draw("ObjectTable", [.. manager->Objects.GameObjectIdSorted.ToArray()
            .Take(manager->Objects.GameObjectIdSortedCount)
            .Select((entry, i) => (Index: i, Pointer: (Pointer<GameObject>)entry.Value))
            .Where(tuple => tuple.Pointer.Value != null)]);
    }

    public void DrawEntityIdSortedTable()
    {
        using var tab = ImRaii.TabItem("EntityIdSorted");
        if (!tab) return;

        var manager = GameObjectManager.Instance();

        _objectTableRenderer.Draw("ObjectTable", [.. manager->Objects.EntityIdSorted.ToArray()
            .Take(manager->Objects.EntityIdSortedCount)
            .Select((entry, i) => (Index: i, Pointer: (Pointer<GameObject>)entry.Value))
            .Where(tuple => tuple.Pointer.Value != null)]);
    }
}
