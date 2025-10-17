namespace HaselDebug.Sheets;

[Sheet]
public readonly struct QuestText(ExcelPage page, uint offset, uint row) : IExcelRow<QuestText>
{
    public uint RowId => row;

    public ReadOnlySeString LuaKey => page.ReadString(offset, offset);
    public ReadOnlySeString Text => page.ReadString(offset + 4, offset);

    public static QuestText Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
