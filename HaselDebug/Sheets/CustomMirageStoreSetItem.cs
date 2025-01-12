using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Sheets;

[Sheet("MirageStoreSetItem")]
public readonly unsafe struct CustomMirageStoreSetItem(ExcelPage page, uint offset, uint row) : IExcelRow<CustomMirageStoreSetItem>
{
    public uint RowId => row;

    public readonly RowRef<Item> Set => new(page.Module, RowId, page.Language);

    /* based on EquipSlotCategory sheet used in E8 ?? ?? ?? ?? 85 C0 74 56 48 8B 0D
       0: MainHand?
       1: OffHand?
       2: Head
       3: Body
       4: Gloves
       5: Legs
       6: Feet
       7: Ears
       8: Neck
       9: Wrists
       10: Ring
    */
    public readonly Collection<RowRef<Item>> Items => new(page, parentOffset: offset, offset: offset, &ItemCtor, size: 11);

    private static RowRef<Item> ItemCtor(ExcelPage page, uint parentOffset, uint offset, uint i) =>
        new(page.Module, page.ReadUInt32(offset + i * 4), page.Language);

    static CustomMirageStoreSetItem IExcelRow<CustomMirageStoreSetItem>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
