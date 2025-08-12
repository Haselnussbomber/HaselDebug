using System.Text;
using Dalamud.Interface.Utility;
using HaselCommon;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class TranslationTestTab : DebugTab
{
    private readonly CommonTranslations _commonTranslations;
    private readonly Translations _translations;

    public override void Draw()
    {
        ImGui.Text(_commonTranslations.FormatCoordsXY(15.6f, 22.7f));
        ImGui.Text(_commonTranslations.CompassHeadings_S);

        ImGuiHelpers.SeStringWrapped(_translations.EvaluatePayloadTest("italic"));

        ImGui.Text(_translations.UnlocksTab_CabinetNotLoaded);

        if (_translations.TryGetTranslation("CompassHeadings.W"u8, out var heading))
            ImGui.Text(Encoding.UTF8.GetString(heading) + "test");

        if (_translations.TryGetTranslation("UnlocksTab.CabinetNotLoaded"u8, out var text))
            ImGui.Text(text);
    }
}
