using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class LobbyTab : DebugTab
{
    public override void Draw()
    {
        var currentCharacter = CharaSelectCharacterList.GetCurrentCharacter();
        if (currentCharacter == null)
            return;

        if (ImGui.Button("DisableDraw"))
        {
            currentCharacter->GameObject.DisableDraw();
        }
        ImGui.SameLine();
        if (ImGui.Button("EnableDraw"))
        {
            currentCharacter->GameObject.EnableDraw();
        }
    }
}
