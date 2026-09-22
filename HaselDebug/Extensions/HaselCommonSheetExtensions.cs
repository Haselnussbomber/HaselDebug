namespace HaselDebug.Extensions;

public static unsafe class HaselCommonSheetExtensions
{
    extension(Level row)
    {
        public Lumina.Excel.Sheets.Level ToDalamud() => new(row.ExcelPage, row.RowOffset, row.RowId);
    }

    extension(MirageStoreSetItem row)
    {
        public RowRef<Item> Set => new(row.ExcelPage.Module, row.RowId, row.ExcelPage.Language);
        public Collection<RowRef<Item>> Items => new(row.ExcelPage, parentOffset: row.RowOffset, offset: row.RowOffset, &MirageStoreSetItemItemCtor, size: row.ExcelPage.Sheet.Columns.Count);
    }

    extension(Permission row)
    {
        public Collection<bool> Conditions
            => new(row.ExcelPage, row.RowOffset, row.RowOffset, &PermissionConditionCtor, row.ExcelPage.Sheet.Columns.Count);
    }

    extension(Lumina.Excel.Sheets.Aetheryte row)
    {
        public Aetheryte ToCustom() => new(row.ExcelPage, row.RowOffset, row.RowId);
    }

    extension(MapService service)
    {
        public void OpenMap(Level level)
        {
            service.OpenMap(level.ToDalamud());
        }

        public float GetDistanceFromPlayer(Level level)
        {
            return service.GetDistanceFromPlayer(level.ToDalamud());
        }

        public string GetCompassDirection(Level level)
        {
            return service.GetCompassDirection(level.ToDalamud());
        }
    }

    extension(TeleportService service)
    {
        public bool TryGetClosestAetheryte(Level level, out Aetheryte aetheryte)
        {
            var ret = service.TryGetClosestAetheryte(level.ToDalamud(), out var lAetheryte);
            aetheryte = lAetheryte.ToCustom();
            return ret;
        }
    }

    private static RowRef<Item> MirageStoreSetItemItemCtor(ExcelPage page, uint parentOffset, uint offset, uint i)
        => new(page.Module, page.ReadUInt32(offset + i * 4), page.Language);

    private static bool PermissionConditionCtor(ExcelPage page, uint parentOffset, uint offset, uint i)
        => page.ReadBool(offset + i);
}
