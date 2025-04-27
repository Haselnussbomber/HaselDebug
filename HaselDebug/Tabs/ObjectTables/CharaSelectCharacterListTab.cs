using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharaSelectCharacterListTab : DebugTab, IObjectTableTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        var list = CharaSelectCharacterList.Instance();
        var clientObjectManager = ClientObjectManager.Instance();
        if (list == null || clientObjectManager == null)
            return;

        foreach (var item in list->CharacterMapping)
        {
            if (item.ClientObjectIndex == -1)
                continue;

            ImGui.Text($"{item.ClientObjectIndex}: {item.ContentId:X}");

            var obj = clientObjectManager->GetObjectByIndex((ushort)item.ClientObjectIndex);
            if (obj == null)
                continue;

            ImGui.SameLine();
            _debugRenderer.DrawPointerType(obj, typeof(BattleChara), new NodeOptions());
        }
    }
}
