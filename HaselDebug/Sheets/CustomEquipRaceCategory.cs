namespace HaselDebug.Sheets;

[Sheet("EquipRaceCategory")]
public readonly unsafe struct CustomEquipRaceCategory(ExcelPage page, uint offset, uint row) : IExcelRow<CustomEquipRaceCategory>
{
    public uint RowId => row;

    public readonly Collection<bool> Races => new(page, offset, offset, &RaceCtor, 8);
    public readonly Collection<bool> Sexes => new(page, offset, offset, &SexCtor, 2);

    private static bool RaceCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadBool(offset + i);
    private static bool SexCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadPackedBool(offset + 8, (byte)i);

    static CustomEquipRaceCategory IExcelRow<CustomEquipRaceCategory>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
