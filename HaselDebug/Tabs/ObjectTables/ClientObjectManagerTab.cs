using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ClientObjectManagerTab : DebugTab, IDebugTab, IObjectTableTab
{
    private readonly ObjectTableRenderer _objectTableRenderer;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        _objectTableRenderer.Draw("ClientObjectManagerTable", [..
            ClientObjectManager.Instance()->BattleCharas.ToArray()
                .Select((entry, i) => (Index: i, Entry: entry))
                .Where(tuple => tuple.Entry.BattleChara != null && tuple.Entry.ObjectKind != ObjectKind.None)
                .Select(tuple => (tuple.Index, (Pointer<GameObject>)(GameObject*)tuple.Entry.BattleChara))]);
    }
}
