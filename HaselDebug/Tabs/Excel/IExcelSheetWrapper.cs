namespace HaselDebug.Tabs.Excel;

public interface IExcelSheetWrapper
{
    string SheetName { get; }
    ClientLanguage Language { get; }
    void Draw();
    void ReloadSheet();
}
