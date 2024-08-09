using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

// TODO: Achievements, Bardings, Mounts, Minions, Fashion Accessories, Facewear, Orchestrion...

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    public override bool DrawInChild => false;

    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly ITextureProvider TextureProvider;
    private readonly MapService MapService;
    private readonly IDataManager DataManager;
    private readonly TranslationManager TranslationManager;
    private readonly ItemService ItemService;
    private readonly TextureService TextureService;
    private readonly ImGuiService ImGuiService;

    public UnlocksTab(
        ExcelService excelService,
        TextService textService,
        ITextureProvider textureProvider,
        MapService mapService,
        IDataManager dataManager,
        TranslationManager translationManager,
        ItemService itemService,
        TextureService textureService,
        ImGuiService imGuiService)
    {
        ExcelService = excelService;
        TextService = textService;
        TextureProvider = textureProvider;
        MapService = mapService;
        DataManager = dataManager;
        TranslationManager = translationManager;
        ItemService = itemService;
        TextureService = textureService;
        ImGuiService = imGuiService;

        TextService.LanguageChanged += OnLanguageChanged;

        Update();
    }

    public void Dispose()
    {
        TextService.LanguageChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        Update();
    }

    private void Update()
    {
        UpdateUnlockLinks();
        UpdateCutscenes();
        UpdateStoreItems();
    }

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("UnlocksTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("UnlocksTabBar");
        if (!tabs) return;

        DrawUnlockLinks();
        DrawAdventure();
        DrawRecipes();
        DrawStoreItems();
        DrawTitles();
        DrawEmotes();
        DrawCutscenes();
    }
}
