using HaselCommon.Gui.Yoga;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Windows.ItemTooltips.Components;
using Lumina.Excel.Sheets;
using YogaSharp;

namespace HaselDebug.Windows.ItemTooltips;

public class TripleTriadCardTooltip : Node
{
    private readonly ExcelService _excelService;
    private readonly TextNode _infoLine;
    private readonly TripleTriadCardNode _card;
    private uint _cardRowId;

    public TripleTriadCardTooltip(
        TextureService textureService,
        ExcelService excelService,
        TripleTriadNumberFontManager tripleTriadNumberFontManager) : base()
    {
        _excelService = excelService;

        Margin = 8;
        AlignItems = YGAlign.Center;
        RowGap = 4;

        Add(
            _infoLine = new TextNode(),
            _card = new TripleTriadCardNode(textureService, tripleTriadNumberFontManager)
        );
    }

    public void SetItem(Item item)
    {
        SetCard(item.ItemAction.Value!.Data[0]);
    }

    public void SetCard(uint cardId)
    {
        if (_cardRowId == cardId)
            return;

        if (!_excelService.TryGetRow<TripleTriadCard>(cardId, out var cardRow))
            return;

        if (!_excelService.TryGetRow<TripleTriadCardResident>(cardId, out var cardResident))
            return;

        _cardRowId = cardId;

        _infoLine.Text = $"{(cardResident.TripleTriadCardRarity.RowId == 5 ? "Ex" : "No")}. {cardResident.Order} - {cardRow.Name}";
        _card.SetCard(_cardRowId, cardRow, cardResident);
    }
}
