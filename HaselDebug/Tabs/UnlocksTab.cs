using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    public override bool DrawInChild => false;

    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly ITextureProvider TextureProvider;
    private readonly MapService MapService;

    public UnlocksTab(
        ExcelService excelService,
        TextService textService,
        ITextureProvider textureProvider,
        MapService mapService)
    {
        ExcelService = excelService;
        TextService = textService;
        TextureProvider = textureProvider;
        MapService = mapService;

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
        DrawTitles();
        DrawEmotes();
        DrawCutscenes();
    }
}
