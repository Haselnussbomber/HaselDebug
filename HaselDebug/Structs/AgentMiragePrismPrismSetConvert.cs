using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using InteropGenerator.Runtime.Attributes;

namespace HaselDebug.Structs;

[GenerateInterop]
[Agent(AgentId.MiragePrismPrismSetConvert)]
[StructLayout(LayoutKind.Explicit, Size = 0x30)]
public unsafe partial struct AgentMiragePrismPrismSetConvert
{
    public static AgentMiragePrismPrismSetConvert* Instance() => (AgentMiragePrismPrismSetConvert*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MiragePrismPrismSetConvert);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 43 28 C6 40 02 0B")]
    public partial void Open(uint itemId, InventoryType inventoryType, int slot, int openerAddonId, bool enableStoring);

    // OpenPreview in data.yml
    public void Open(uint itemId) => Open(itemId, (InventoryType)9999, 0, 0, false);
}
