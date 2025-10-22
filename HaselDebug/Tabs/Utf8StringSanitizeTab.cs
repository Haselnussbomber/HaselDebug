using System.Text.Unicode;
using FFXIVClientStructs.FFXIV.Client.System.String;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class Utf8StringSanitizeTab : DebugTab
{
    private readonly List<Entry> _list =
    [
        new ("BasicLatin", UnicodeRanges.BasicLatin),
        new ("Latin1Supplement", UnicodeRanges.Latin1Supplement),
        new ("LatinExtendedA", UnicodeRanges.LatinExtendedA),
        new ("LatinExtendedB", UnicodeRanges.LatinExtendedB),
        new ("IpaExtensions", UnicodeRanges.IpaExtensions),
        new ("SpacingModifierLetters", UnicodeRanges.SpacingModifierLetters),
        new ("CombiningDiacriticalMarks", UnicodeRanges.CombiningDiacriticalMarks),
        new ("GreekandCoptic", UnicodeRanges.GreekandCoptic),
        new ("Cyrillic", UnicodeRanges.Cyrillic),
        new ("CyrillicSupplement", UnicodeRanges.CyrillicSupplement),
        new ("Armenian", UnicodeRanges.Armenian),
        new ("Hebrew", UnicodeRanges.Hebrew),
        new ("Arabic", UnicodeRanges.Arabic),
        new ("Syriac", UnicodeRanges.Syriac),
        new ("ArabicSupplement", UnicodeRanges.ArabicSupplement),
        new ("Thaana", UnicodeRanges.Thaana),
        new ("NKo", UnicodeRanges.NKo),
        new ("Samaritan", UnicodeRanges.Samaritan),
        new ("Mandaic", UnicodeRanges.Mandaic),
        new ("SyriacSupplement", UnicodeRanges.SyriacSupplement),
        new ("ArabicExtendedB", UnicodeRanges.ArabicExtendedB),
        new ("ArabicExtendedA", UnicodeRanges.ArabicExtendedA),
        new ("Devanagari", UnicodeRanges.Devanagari),
        new ("Bengali", UnicodeRanges.Bengali),
        new ("Gurmukhi", UnicodeRanges.Gurmukhi),
        new ("Gujarati", UnicodeRanges.Gujarati),
        new ("Oriya", UnicodeRanges.Oriya),
        new ("Tamil", UnicodeRanges.Tamil),
        new ("Telugu", UnicodeRanges.Telugu),
        new ("Kannada", UnicodeRanges.Kannada),
        new ("Malayalam", UnicodeRanges.Malayalam),
        new ("Sinhala", UnicodeRanges.Sinhala),
        new ("Thai", UnicodeRanges.Thai),
        new ("Lao", UnicodeRanges.Lao),
        new ("Tibetan", UnicodeRanges.Tibetan),
        new ("Myanmar", UnicodeRanges.Myanmar),
        new ("Georgian", UnicodeRanges.Georgian),
        new ("HangulJamo", UnicodeRanges.HangulJamo),
        new ("Ethiopic", UnicodeRanges.Ethiopic),
        new ("EthiopicSupplement", UnicodeRanges.EthiopicSupplement),
        new ("Cherokee", UnicodeRanges.Cherokee),
        new ("UnifiedCanadianAboriginalSyllabics", UnicodeRanges.UnifiedCanadianAboriginalSyllabics),
        new ("Ogham", UnicodeRanges.Ogham),
        new ("Runic", UnicodeRanges.Runic),
        new ("Tagalog", UnicodeRanges.Tagalog),
        new ("Hanunoo", UnicodeRanges.Hanunoo),
        new ("Buhid", UnicodeRanges.Buhid),
        new ("Tagbanwa", UnicodeRanges.Tagbanwa),
        new ("Khmer", UnicodeRanges.Khmer),
        new ("Mongolian", UnicodeRanges.Mongolian),
        new ("UnifiedCanadianAboriginalSyllabicsExtended", UnicodeRanges.UnifiedCanadianAboriginalSyllabicsExtended),
        new ("Limbu", UnicodeRanges.Limbu),
        new ("TaiLe", UnicodeRanges.TaiLe),
        new ("NewTaiLue", UnicodeRanges.NewTaiLue),
        new ("KhmerSymbols", UnicodeRanges.KhmerSymbols),
        new ("Buginese", UnicodeRanges.Buginese),
        new ("TaiTham", UnicodeRanges.TaiTham),
        new ("CombiningDiacriticalMarksExtended", UnicodeRanges.CombiningDiacriticalMarksExtended),
        new ("Balinese", UnicodeRanges.Balinese),
        new ("Sundanese", UnicodeRanges.Sundanese),
        new ("Batak", UnicodeRanges.Batak),
        new ("Lepcha", UnicodeRanges.Lepcha),
        new ("OlChiki", UnicodeRanges.OlChiki),
        new ("CyrillicExtendedC", UnicodeRanges.CyrillicExtendedC),
        new ("GeorgianExtended", UnicodeRanges.GeorgianExtended),
        new ("SundaneseSupplement", UnicodeRanges.SundaneseSupplement),
        new ("VedicExtensions", UnicodeRanges.VedicExtensions),
        new ("PhoneticExtensions", UnicodeRanges.PhoneticExtensions),
        new ("PhoneticExtensionsSupplement", UnicodeRanges.PhoneticExtensionsSupplement),
        new ("CombiningDiacriticalMarksSupplement", UnicodeRanges.CombiningDiacriticalMarksSupplement),
        new ("LatinExtendedAdditional", UnicodeRanges.LatinExtendedAdditional),
        new ("GreekExtended", UnicodeRanges.GreekExtended),
        new ("GeneralPunctuation", UnicodeRanges.GeneralPunctuation),
        new ("SuperscriptsandSubscripts", UnicodeRanges.SuperscriptsandSubscripts),
        new ("CurrencySymbols", UnicodeRanges.CurrencySymbols),
        new ("CombiningDiacriticalMarksforSymbols", UnicodeRanges.CombiningDiacriticalMarksforSymbols),
        new ("LetterlikeSymbols", UnicodeRanges.LetterlikeSymbols),
        new ("NumberForms", UnicodeRanges.NumberForms),
        new ("Arrows", UnicodeRanges.Arrows),
        new ("MathematicalOperators", UnicodeRanges.MathematicalOperators),
        new ("MiscellaneousTechnical", UnicodeRanges.MiscellaneousTechnical),
        new ("ControlPictures", UnicodeRanges.ControlPictures),
        new ("OpticalCharacterRecognition", UnicodeRanges.OpticalCharacterRecognition),
        new ("EnclosedAlphanumerics", UnicodeRanges.EnclosedAlphanumerics),
        new ("BoxDrawing", UnicodeRanges.BoxDrawing),
        new ("BlockElements", UnicodeRanges.BlockElements),
        new ("GeometricShapes", UnicodeRanges.GeometricShapes),
        new ("MiscellaneousSymbols", UnicodeRanges.MiscellaneousSymbols),
        new ("Dingbats", UnicodeRanges.Dingbats),
        new ("MiscellaneousMathematicalSymbolsA", UnicodeRanges.MiscellaneousMathematicalSymbolsA),
        new ("SupplementalArrowsA", UnicodeRanges.SupplementalArrowsA),
        new ("BraillePatterns", UnicodeRanges.BraillePatterns),
        new ("SupplementalArrowsB", UnicodeRanges.SupplementalArrowsB),
        new ("MiscellaneousMathematicalSymbolsB", UnicodeRanges.MiscellaneousMathematicalSymbolsB),
        new ("SupplementalMathematicalOperators", UnicodeRanges.SupplementalMathematicalOperators),
        new ("MiscellaneousSymbolsandArrows", UnicodeRanges.MiscellaneousSymbolsandArrows),
        new ("Glagolitic", UnicodeRanges.Glagolitic),
        new ("LatinExtendedC", UnicodeRanges.LatinExtendedC),
        new ("Coptic", UnicodeRanges.Coptic),
        new ("GeorgianSupplement", UnicodeRanges.GeorgianSupplement),
        new ("Tifinagh", UnicodeRanges.Tifinagh),
        new ("EthiopicExtended", UnicodeRanges.EthiopicExtended),
        new ("CyrillicExtendedA", UnicodeRanges.CyrillicExtendedA),
        new ("SupplementalPunctuation", UnicodeRanges.SupplementalPunctuation),
        new ("CjkRadicalsSupplement", UnicodeRanges.CjkRadicalsSupplement),
        new ("KangxiRadicals", UnicodeRanges.KangxiRadicals),
        new ("IdeographicDescriptionCharacters", UnicodeRanges.IdeographicDescriptionCharacters),
        new ("CjkSymbolsandPunctuation", UnicodeRanges.CjkSymbolsandPunctuation),
        new ("Hiragana", UnicodeRanges.Hiragana),
        new ("Katakana", UnicodeRanges.Katakana),
        new ("Bopomofo", UnicodeRanges.Bopomofo),
        new ("HangulCompatibilityJamo", UnicodeRanges.HangulCompatibilityJamo),
        new ("Kanbun", UnicodeRanges.Kanbun),
        new ("BopomofoExtended", UnicodeRanges.BopomofoExtended),
        new ("CjkStrokes", UnicodeRanges.CjkStrokes),
        new ("KatakanaPhoneticExtensions", UnicodeRanges.KatakanaPhoneticExtensions),
        new ("EnclosedCjkLettersandMonths", UnicodeRanges.EnclosedCjkLettersandMonths),
        new ("CjkCompatibility", UnicodeRanges.CjkCompatibility),
        new ("CjkUnifiedIdeographsExtensionA", UnicodeRanges.CjkUnifiedIdeographsExtensionA),
        new ("YijingHexagramSymbols", UnicodeRanges.YijingHexagramSymbols),
        new ("CjkUnifiedIdeographs", UnicodeRanges.CjkUnifiedIdeographs),
        new ("YiSyllables", UnicodeRanges.YiSyllables),
        new ("YiRadicals", UnicodeRanges.YiRadicals),
        new ("Lisu", UnicodeRanges.Lisu),
        new ("Vai", UnicodeRanges.Vai),
        new ("CyrillicExtendedB", UnicodeRanges.CyrillicExtendedB),
        new ("Bamum", UnicodeRanges.Bamum),
        new ("ModifierToneLetters", UnicodeRanges.ModifierToneLetters),
        new ("LatinExtendedD", UnicodeRanges.LatinExtendedD),
        new ("SylotiNagri", UnicodeRanges.SylotiNagri),
        new ("CommonIndicNumberForms", UnicodeRanges.CommonIndicNumberForms),
        new ("Phagspa", UnicodeRanges.Phagspa),
        new ("Saurashtra", UnicodeRanges.Saurashtra),
        new ("DevanagariExtended", UnicodeRanges.DevanagariExtended),
        new ("KayahLi", UnicodeRanges.KayahLi),
        new ("Rejang", UnicodeRanges.Rejang),
        new ("HangulJamoExtendedA", UnicodeRanges.HangulJamoExtendedA),
        new ("Javanese", UnicodeRanges.Javanese),
        new ("MyanmarExtendedB", UnicodeRanges.MyanmarExtendedB),
        new ("Cham", UnicodeRanges.Cham),
        new ("MyanmarExtendedA", UnicodeRanges.MyanmarExtendedA),
        new ("TaiViet", UnicodeRanges.TaiViet),
        new ("MeeteiMayekExtensions", UnicodeRanges.MeeteiMayekExtensions),
        new ("EthiopicExtendedA", UnicodeRanges.EthiopicExtendedA),
        new ("LatinExtendedE", UnicodeRanges.LatinExtendedE),
        new ("CherokeeSupplement", UnicodeRanges.CherokeeSupplement),
        new ("MeeteiMayek", UnicodeRanges.MeeteiMayek),
        new ("HangulSyllables", UnicodeRanges.HangulSyllables),
        new ("HangulJamoExtendedB", UnicodeRanges.HangulJamoExtendedB),
        new ("CjkCompatibilityIdeographs", UnicodeRanges.CjkCompatibilityIdeographs),
        new ("AlphabeticPresentationForms", UnicodeRanges.AlphabeticPresentationForms),
        new ("ArabicPresentationFormsA", UnicodeRanges.ArabicPresentationFormsA),
        new ("VariationSelectors", UnicodeRanges.VariationSelectors),
        new ("VerticalForms", UnicodeRanges.VerticalForms),
        new ("CombiningHalfMarks", UnicodeRanges.CombiningHalfMarks),
        new ("CjkCompatibilityForms", UnicodeRanges.CjkCompatibilityForms),
        new ("SmallFormVariants", UnicodeRanges.SmallFormVariants),
        new ("ArabicPresentationFormsB", UnicodeRanges.ArabicPresentationFormsB),
        new ("HalfwidthandFullwidthForms", UnicodeRanges.HalfwidthandFullwidthForms),
        new ("Specials", UnicodeRanges.Specials)
    ];

    private readonly DebugRenderer _debugRenderer;

    public override string Title => "Utf8String Sanitize";

    public Utf8StringSanitizeTab(DebugRenderer debugRenderer)
    {
        _debugRenderer = debugRenderer;

        var str = Utf8String.CreateEmpty();

        foreach (var entry in _list)
        {
            var range = entry.Range;
            var teststring = string.Empty;

            for (var codePoint = range.FirstCodePoint; codePoint <= range.FirstCodePoint + range.Length - 1; codePoint++)
            {
                if (codePoint < 0x04)
                    continue;

                teststring += (char)codePoint;
            }

            entry.Input = teststring;

            for (var i = 0; i < 12; i++)
            {
                if (i == 4) continue;
                str->SetString(teststring);
                str->SanitizeString((AllowedEntities)(1 << i), null);
                entry.Output[i] = str->ToString();
            }
        }

        str->Dtor(true);
    }

    public override void Draw()
    {
        foreach (var entry in _list)
        {
            using var node = ImRaii.TreeNode($"U+{entry.Range.FirstCodePoint:X4}-U+{entry.Range.FirstCodePoint + entry.Range.Length - 1:X4} - {entry.Name}###{entry.Name}", ImGuiTreeNodeFlags.SpanFullWidth);
            if (!node) continue;

            ImGui.Text("Input:"u8);
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText(entry.Input);

            using var table = ImRaii.Table(entry.Name + "Table", 2, ImGuiTableFlags.Borders);
            if (!table) continue;

            ImGui.TableSetupColumn("Flag"u8, ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Output"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            for (var i = 0; i < entry.Output.Length; i++)
            {
                ImGui.TableNextColumn();
                ImGui.Text($"{(AllowedEntities)(1 << i)}");
                ImGui.TableNextColumn();
                ImGuiUtils.DrawCopyableText(entry.Output[i]);
            }
        }
    }

    public record Entry
    {
        public Entry(string name, UnicodeRange range)
        {
            Name = name;
            Range = range;
        }

        public string Name { get; }
        public UnicodeRange Range { get; }
        public string Input { get; set; } = string.Empty;
        public string[] Output { get; set; } = new string[12];
    }
}
