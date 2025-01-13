using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.FashionAccessories;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class FashionAccessoriesTab(FashionAccessoriesTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Fashion Accessories";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
