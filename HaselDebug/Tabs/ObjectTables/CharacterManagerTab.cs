using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharacterManagerTab : DebugTab, IObjectTableTab
{
    private readonly ObjectTableRenderer _objectTableRenderer;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var manager = CharacterManager.Instance();

        _objectTableRenderer.Draw("CharacterManager", [.. manager->BattleCharas.ToArray()
            .Select((entry, i) => (Index: i, Pointer: (Pointer<GameObject>)(GameObject*)entry.Value))
            .Where(tuple => tuple.Pointer.Value != null)]);
    }
}
