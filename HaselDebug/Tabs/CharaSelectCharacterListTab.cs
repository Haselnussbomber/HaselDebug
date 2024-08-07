using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class CharaSelectCharacterListTab : DebugTab
{
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
            DebugUtils.DrawPointerType(obj, typeof(BattleChara), new NodeOptions());
        }
    }
}
