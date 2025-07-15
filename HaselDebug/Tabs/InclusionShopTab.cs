using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using InteropGenerator.Runtime.Attributes;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InclusionShopTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        if (!TryGetAddon<AtkUnitBase>("InclusionShop", out var addon))
        {
            ImGui.TextUnformatted("No shop open!");
            return;
        }

        _debugRenderer.DrawPointerType(addon->AtkValues, typeof(InclusionShopAtkValues), new NodeOptions());
    }
}

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x10 * 2939)]
public partial struct InclusionShopAtkValues
{
    [FieldOffset(0x10 * 296)] public AtkValue PinnedCurrencyIconId;
    [FieldOffset(0x10 * 297)] public AtkValue PinnedCurrencyCount;
    [FieldOffset(0x10 * 298)] public AtkValue ItemCount;
    [FieldOffset(0x10 * 299), FixedSizeArray] internal FixedSizeArray60<InclusionShopItem> _items;
}

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x10 * 18)]
public partial struct InclusionShopItem
{
    [FieldOffset(0x10 * 0)] public AtkValue AtkValue0;
    [FieldOffset(0x10 * 1)] public AtkValue ItemId;
    [FieldOffset(0x10 * 2)] public AtkValue IconId;
    [FieldOffset(0x10 * 3)] public AtkValue Stacksize;
    [FieldOffset(0x10 * 4)] public AtkValue AmountOwned;
    [FieldOffset(0x10 * 5)] public AtkValue GiveCount;
    [FieldOffset(0x10 * 6), FixedSizeArray] internal FixedSizeArray3<AtkValue> _giveItemId;
    [FieldOffset(0x10 * 9), FixedSizeArray] internal FixedSizeArray3<AtkValue> _giveIconId;
    [FieldOffset(0x10 * 12), FixedSizeArray] internal FixedSizeArray3<AtkValue> _giveAmount;
    [FieldOffset(0x10 * 14)] public AtkValue AtkValue14;
    [FieldOffset(0x10 * 15)] public AtkValue MaxAmount;
    [FieldOffset(0x10 * 16)] public AtkValue Flags; // 0b10 = CanSelectAmount
    [FieldOffset(0x10 * 17)] public AtkValue Index;
}
