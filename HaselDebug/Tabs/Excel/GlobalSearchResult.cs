namespace HaselDebug.Tabs.Excel;

public record GlobalSearchResult(bool IsSubrowSheet, string SheetType, string SheetName, string RowId, int ColumnIndex, string ColumnName, string Value);
