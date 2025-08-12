using Dalamud.Plugin.Services;
using HaselCommon;
using HaselCommon.Services;

namespace HaselDebug;

[AutoConstruct]
[RegisterSingleton]
[RegisterSingleton<ITranslationProvider>(Duplicate = DuplicateStrategy.Append)]
public partial class Translations : ITranslationProvider
{
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly LanguageProvider _languageProvider;
}
