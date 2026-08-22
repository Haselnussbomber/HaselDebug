using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ReactionEventObjectManagerTab : DebugTab, IDebugTab, IObjectTableTab
{
    private readonly ObjectTableRenderer _objectTableRenderer;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("ReactionEventObjectManagerTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        _objectTableRenderer.Draw("ReactionEventObjectManager_EventObjs", [..
            ReactionEventObjectManager.Instance()->ReactionEventObjects.ToArray()
                .Select((ptr, i) => (Index: i, Pointer: ptr))
                .Where(tuple => tuple.Pointer.Value != null)
                .Select(tuple => (tuple.Index, (Pointer<GameObject>)(GameObject*)tuple.Pointer.Value))]);
    }
}
