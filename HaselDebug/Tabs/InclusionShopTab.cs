using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InclusionShopTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        if (!TryGetAddon<AddonInclusionShop>("InclusionShop", out var addon))
        {
            ImGui.Text("No InclusionShop open!"u8);
            return;
        }

        _debugRenderer.DrawPointerType(addon->AtkValues, typeof(AddonInclusionShop.InclusionShopAtkValues), new NodeOptions());
    }
}
