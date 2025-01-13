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
    private readonly SeStringEvaluatorService _seStringEvaluator;
    private readonly TextNode _infoLine;
    private readonly TripleTriadCardNode _card;
    private uint _cardRowId;

    public TripleTriadCardTooltip(
        TextureService textureService,
        ExcelService excelService,
        SeStringEvaluatorService seStringEvaluator,
        TripleTriadNumberFontManager tripleTriadNumberFontManager) : base()
    {
        _excelService = excelService;
        _seStringEvaluator = seStringEvaluator;

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

        var isEx = cardResident.UIPriority == 5;
        var order = (uint)cardResident.Order;
        var addonRowId = isEx ? 9773u : 9772;

        _infoLine.Text = $"{_seStringEvaluator.EvaluateFromAddon(addonRowId, new() { LocalParameters = [order] }).ExtractText()} - {cardRow.Name}";
        _card.SetCard(_cardRowId, cardResident);
    }
}
