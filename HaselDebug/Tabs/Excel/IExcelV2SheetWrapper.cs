namespace HaselDebug.Tabs.Excel;

public interface IExcelV2SheetWrapper
{
    string SheetName { get; }
    ClientLanguage Language { get; }
    void Draw();
    void ReloadSheet();
}
