using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using Lumina.Text;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class RaptureTextModuleTab : DebugTab, IDisposable
{
    private readonly List<TextEntry> _entries = [
        new TextEntry(TextEntryType.String, "Test1 "),
        new TextEntry(TextEntryType.Macro, "<color(0xFF9000)>"),
        new TextEntry(TextEntryType.String, "Test2 "),
        new TextEntry(TextEntryType.Macro, "<color(0)>"),
        new TextEntry(TextEntryType.String, "Test3 "),
        new TextEntry(TextEntryType.Macro, "<color(stackcolor)>"),
        new TextEntry(TextEntryType.String, "Test 4 "),
        new TextEntry(TextEntryType.Macro, "<color(stackcolor)>"),
        new TextEntry(TextEntryType.String, "Test 5"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,1,1,Some Player)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,2,28,100,0,0,ClassJob)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,3,156,65561,65035,-696153,63,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,3,156,65561,65035,-696153,-63,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,4,39246,1,0,0,Phoenix Riser Suit)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,5,4)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,6,1031195)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,6,1031197)>"),
        // 7 formats a string??
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,8,190)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,8,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        // 9 writes a uint to PronounModule
        new TextEntry(TextEntryType.Fixed, "<fixed(200,10,3,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,10,3,1,Title,Description)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,11,12345,0,65536,0,Player Name)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,12,70058,0,0,0,The Ultimate Weapon)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(48,209)>"), // Mount
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(49,28)>"), // ClassJob
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(50,2957)>"), // PlaceName
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(51,4)>"), // Race
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(52,7)>"), // Tribe
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(64,13)>"), // Companion
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(60,21)>"), // MainCommand
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<ordinal(501)>"), // 501st
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<split(Hello World, ,1)>"), // Hello
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<split(Hello World, ,2)>"), // World
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<link(2,1,0,0,A)>"),
        new TextEntry(TextEntryType.String, "Item Link Test"),
        new TextEntry(TextEntryType.Macro, "<link(0xCE,0,0,0,)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<link(4,66822,0,0,Q)>"),
        new TextEntry(TextEntryType.String, "Quest Link Test"),
        new TextEntry(TextEntryType.Macro, "<link(0xCE,0,0,0,)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<link(5,910,0,0,A)>"),
        new TextEntry(TextEntryType.String, "Achievement Link Test"),
        new TextEntry(TextEntryType.Macro, "<link(0xCE,0,0,0,)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<link(6,1,0,0,H)>"),
        new TextEntry(TextEntryType.String, "HowTo Link Test"),
        new TextEntry(TextEntryType.Macro, "<link(0xCE,0,0,0,)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<link(8,327,0,0,S)>"),
        new TextEntry(TextEntryType.String, "Status Link Test"),
        new TextEntry(TextEntryType.Macro, "<link(0xCE,0,0,0,)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<link(10,1,0,0,A)>"),
        new TextEntry(TextEntryType.String, "AkatsukiNote Link Test"),
        new TextEntry(TextEntryType.Macro, "<link(0xCE,0,0,0,)>"),
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly AddonObserver _addonObserver;
    private readonly LanguageProvider _languageProvider;
    private readonly GfdService _gfdService;
    private readonly UldService _uldService;

    private SeStringInspectorWindow? _inspectorWindow;
    private bool _isInitialized;

    public override bool DrawInChild => false;

    private void Initialize()
    {
        _languageProvider.LanguageChanged += OnLanguageChanged;
    }

    public void Dispose()
    {
        _languageProvider.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string langCode)
    {
        if (_inspectorWindow != null)
            _inspectorWindow.Language = _languageProvider.ClientLanguage;
    }

    public override unsafe void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        using var hostchild = ImRaii.Child("RaptureTextModuleTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("RaptureTextModuleTab_TabBar");
        if (!tabs) return;

        DrawGlobalParameters();
        DrawDefinitions();
        DrawIcon2Mapping();
        DrawStringMaker();
    }

    private void DrawGlobalParameters()
    {
        using var tab = ImRaii.TabItem("GlobalParameters");
        if (!tab) return;

        using var table = ImRaii.Table("GlobalParametersTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ValuePtr"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Description"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var deque = RaptureTextModule.Instance()->GlobalParameters;
        for (var i = 0u; i < deque.MySize; i++)
        {
            var item = deque[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Type
            ImGui.Text(item.Type.ToString());

            ImGui.TableNextColumn(); // ValuePtr
            _debugRenderer.DrawAddress(item.ValuePtr);

            ImGui.TableNextColumn(); // Value
            switch (item.Type)
            {
                case TextParameterType.Integer:
                    ImGuiUtilsEx.DrawCopyableText($"0x{item.IntValue:X}");
                    ImGui.SameLine();
                    ImGuiUtilsEx.DrawCopyableText(item.IntValue.ToString());
                    break;

                case TextParameterType.ReferencedUtf8String:
                    if (item.ReferencedUtf8StringValue != null)
                    {
                        _debugRenderer.DrawSeString(item.ReferencedUtf8StringValue->Utf8String.StringPtr, new NodeOptions
                        {
                            AddressPath = new AddressPath([(nint)i, (nint)item.ReferencedUtf8StringValue]),
                            SeStringTitle = $"GlobalParameter {i}",
                            Indent = false
                        });
                    }
                    else
                    {
                        ImGui.Text("null"u8);
                    }

                    break;

                case TextParameterType.String:
                    _debugRenderer.DrawSeString(item.StringValue, new NodeOptions
                    {
                        SeStringTitle = $"GlobalParameter {i}",
                        Indent = false
                    });
                    break;
            }

            ImGui.TableNextColumn();
            ImGui.Text(i switch
            {
                0 => "Player Name",
                1 => "Temp Entity 1: Name",
                2 => "Temp Entity 2: Name",
                3 => "Player Sex",
                4 => "Temp Entity 1: Sex",
                5 => "Temp Entity 2: Sex",
                6 => "Temp Entity 1: ObjStrId",
                7 => "Temp Entity 2: ObjStrId",
                10 => "Eorzea Time Hours",
                11 => "Eorzea Time Minutes",
                12 => "ColorSay",
                13 => "ColorShout",
                14 => "ColorTell",
                15 => "ColorParty",
                16 => "ColorAlliance",
                17 => "ColorLS1",
                18 => "ColorLS2",
                19 => "ColorLS3",
                20 => "ColorLS4",
                21 => "ColorLS5",
                22 => "ColorLS6",
                23 => "ColorLS7",
                24 => "ColorLS8",
                25 => "ColorFCompany",
                26 => "ColorPvPGroup",
                27 => "ColorPvPGroupAnnounce",
                28 => "ColorBeginner",
                29 => "ColorEmoteUser",
                30 => "ColorEmote",
                31 => "ColorYell",
                32 => "ColorFCAnnounce",
                33 => "ColorBeginnerAnnounce",
                34 => "ColorCWLS",
                35 => "ColorAttackSuccess",
                36 => "ColorAttackFailure",
                37 => "ColorAction",
                38 => "ColorItem",
                39 => "ColorCureGive",
                40 => "ColorBuffGive",
                41 => "ColorDebuffGive",
                42 => "ColorEcho",
                43 => "ColorSysMsg",
                51 => "Player Grand Company Rank (Maelstrom)",
                52 => "Player Grand Company Rank (Twin Adders)",
                53 => "Player Grand Company Rank (Immortal Flames)",
                54 => "Companion Name",
                55 => "Content Name",
                56 => "ColorSysBattle",
                57 => "ColorSysGathering",
                58 => "ColorSysErr",
                59 => "ColorNpcSay",
                60 => "ColorItemNotice",
                61 => "ColorGrowup",
                62 => "ColorLoot",
                63 => "ColorCraft",
                64 => "ColorGathering",
                65 => "Temp Entity 1: Name starts with Vowel",
                66 => "Temp Entity 2: Name starts with Vowel",
                67 => "Player ClassJobId",
                68 => "Player Level",
                69 => "Player StartTown",
                70 => "Player Race",
                71 => "Player Synced Level",
                73 => "Quest#66047: Has met Alphinaud and Alisaie",
                74 => "PlayStation Generation",
                75 => "Is Legacy Player",
                77 => "Client/Platform?",
                78 => "Player BirthMonth",
                79 => "PadMode",
                82 => "Datacenter Region",
                83 => "ColorCWLS2",
                84 => "ColorCWLS3",
                85 => "ColorCWLS4",
                86 => "ColorCWLS5",
                87 => "ColorCWLS6",
                88 => "ColorCWLS7",
                89 => "ColorCWLS8",
                91 => "Player Grand Company",
                92 => "TerritoryType Id",
                93 => "Is Soft Keyboard Enabled",
                94 => "LogSetRoleColor 1: LogColorRoleTank",
                95 => "LogSetRoleColor 2: LogColorRoleTank",
                96 => "LogSetRoleColor 1: LogColorRoleHealer",
                97 => "LogSetRoleColor 2: LogColorRoleHealer",
                98 => "LogSetRoleColor 1: LogColorRoleDPS",
                99 => "LogSetRoleColor 2: LogColorRoleDPS",
                100 => "LogSetRoleColor 1: LogColorOtherClass",
                101 => "LogSetRoleColor 2: LogColorOtherClass",
                102 => "Has Login Security Token",
                103 => "Is subscribed to PlayStation Plus",
                104 => "PadMouseMode",
                106 => "Preferred World Bonus Max Level",
                107 => "Occult Crescent Support Job Level",
                108 => "Deep Dungeon Id",
                _ => "",
            });
        }
    }

    private void DrawDefinitions()
    {
        using var tab = ImRaii.TabItem("Definitions");
        if (!tab) return;

        using var table = ImRaii.Table("DefinitionsTable"u8, 13, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Code"u8, ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("TotalParamCount"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("ParamCount"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("IsTerminated"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        for (var i = 0; i < 7; i++)
            ImGui.TableSetupColumn($"{i}", ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("DecoderFunc"u8, ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var raptureTextModule = RaptureTextModule.Instance();

        foreach (var item in raptureTextModule->TextModule.MacroEncoder.MacroCodeMap.OrderBy(item => item.Value.Id))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.Text($"0x{item.Value.Id:X}");

            ImGui.TableNextColumn(); // Code
            ImGui.Text(item.Key.ToString());

            ImGui.TableNextColumn(); // TotalParamCount
            ImGui.Text(item.Value.TotalParamCount.ToString());

            ImGui.TableNextColumn(); // ParamCount
            ImGui.Text(item.Value.ParamCount.ToString());

            ImGui.TableNextColumn(); // IsTerminated
            ImGui.Text(item.Value.IsTerminated.ToString());

            for (var i = 0; i < 7; i++)
            {
                ImGui.TableNextColumn();
                if (i < item.Value.TotalParamCount)
                {
                    var character = ((char)item.Value.ParamTypes[i]).ToString();
                    if (character != "\0")
                        ImGui.Text(character);
                }
            }

            ImGui.TableNextColumn();
            if (raptureTextModule->DecoderFuncs[item.Value.Id] != 0)
            {
                // resolve jmp [eax+offset]

                var span = new Span<byte>((byte*)raptureTextModule->DecoderFuncs[item.Value.Id], 9);
                var resolvedVf = nint.Zero;
                var vfOffset = 0;

                if (span.StartsWith((ReadOnlySpan<byte>)[0x48, 0x8B, 0x01, 0xFF, 0x60])) // 8-bit displacement
                {
                    vfOffset = span[5];
                    resolvedVf = *(nint*)(*(nint*)&raptureTextModule->TextModule.MacroDecoder + vfOffset);
                }
                else if (span.StartsWith((ReadOnlySpan<byte>)[0x48, 0x8B, 0x01, 0xFF, 0xA0])) // 32-bit displacement
                {
                    vfOffset = *(int*)span.GetPointer(5);
                    resolvedVf = *(nint*)(*(nint*)&raptureTextModule->TextModule.MacroDecoder + vfOffset);
                }

                _debugRenderer.DrawAddress(raptureTextModule->DecoderFuncs[item.Value.Id]);

                if (resolvedVf != 0)
                {
                    ImGui.SameLine();
                    ImGui.Text("->"u8);
                    ImGui.SameLine();
                    _debugRenderer.DrawAddress(resolvedVf);
                    if (vfOffset > 0)
                    {
                        ImGui.SameLine();
                        ImGui.Text("(vfunc: "u8);
                        ImGui.SameLine(0, 0);
                        ImGuiUtilsEx.DrawCopyableText($"{vfOffset / 8}");
                        ImGui.SameLine(0, 0);
                        ImGui.Text(")"u8);
                    }
                }
            }
        }
    }

    private void DrawIcon2Mapping()
    {
        using var tab = ImRaii.TabItem("Icon2 Mapping");
        if (!tab) return;

        using var table = ImRaii.Table("PadButtonMappingTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Requested Icon"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Displayed Icon"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var iconMapping = RaptureAtkModule.Instance()->AtkFontManager.Icon2RemapTable;
        for (var i = 0; i < 30; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{i}");

            ImGui.TableNextColumn();
            _gfdService.Draw(iconMapping[i].IconId, ImGui.GetTextLineHeightWithSpacing());
            ImGui.SameLine();
            ImGui.Text($"{iconMapping[i].IconId}");

            ImGui.TableNextColumn();
            _gfdService.Draw(iconMapping[i].RemappedIconId, ImGui.GetTextLineHeightWithSpacing());
            ImGui.SameLine();
            ImGui.Text($"{iconMapping[i].RemappedIconId}");
        }
    }

    private void DrawStringMaker()
    {
        using var tab = ImRaii.TabItem("StringMaker");
        if (!tab) return;

        if (_inspectorWindow == null)
        {
            _inspectorWindow = _windowManager.CreateOrOpen("StringMaker Preview", () => new SeStringInspectorWindow(_windowManager, _textService, _addonObserver, _serviceProvider)
            {
                String = "",
                Language = _languageProvider.ClientLanguage,
                WindowName = "StringMaker Preview",
            });
            UpdateInspectorString();
        }

        var raptureTextModule = RaptureTextModule.Instance();

        if (ImGui.Button("Add entry"))
        {
            _entries.Add(new(TextEntryType.String, string.Empty));
        }

        ImGui.SameLine();

        if (ImGui.Button("PrintString"))
        {
            var output = Utf8String.CreateEmpty();
            var temp = Utf8String.CreateEmpty();
            var temp2 = Utf8String.CreateEmpty();

            foreach (var entry in _entries)
            {
                switch (entry.Type)
                {
                    case TextEntryType.String:
                        output->ConcatCStr(entry.Message);
                        break;

                    case TextEntryType.Macro:
                        temp->Clear();
                        RaptureTextModule.Instance()->MacroEncoder.EncodeString(temp, entry.Message);
                        output->Append(temp);
                        break;

                    case TextEntryType.Fixed:
                        temp->SetString(entry.Message);
                        temp2->Clear();

                        RaptureTextModule.Instance()->TextModule.ProcessMacroCode(temp2, temp->StringPtr.Value);
                        var out1 = PronounModule.Instance()->ProcessString(temp2, true);
                        var out2 = PronounModule.Instance()->ProcessString(out1, false);

                        output->Append(out2);
                        break;
                }
            }

            RaptureLogModule.Instance()->PrintString(output->StringPtr.Value);
            temp2->Dtor(true);
            temp->Dtor(true);
            output->Dtor(true);
        }

        ImGui.SameLine();

        if (ImGui.Button("Print Evaluated"))
        {
            var sb = new SeStringBuilder();

            foreach (var entry in _entries)
            {
                switch (entry.Type)
                {
                    case TextEntryType.String:
                        sb.Append(entry.Message);
                        break;

                    case TextEntryType.Macro:
                    case TextEntryType.Fixed:
                        sb.AppendMacroString(entry.Message);
                        break;
                }
            }

            RaptureLogModule.Instance()->PrintString(_seStringEvaluator.Evaluate(sb.ToReadOnlySeString()));
        }

        if (_entries.Count != 0)
        {
            ImGui.SameLine();

            if (ImGui.Button("Clear entries"))
                _entries.Clear();
        }

        ImGui.SameLine();

        if (ImGui.Button("Copy as hex"))
        {
            var sb = new SeStringBuilder();

            foreach (var entry in _entries)
            {
                switch (entry.Type)
                {
                    case TextEntryType.String:
                        sb.Append(entry.Message);
                        break;

                    case TextEntryType.Macro:
                    case TextEntryType.Fixed:
                        sb.AppendMacroString(entry.Message);
                        break;
                }
            }

            ImGui.SetClipboardText(string.Join(", ", sb.ToArray().Select(b => $"0x{b:X2}")));
        }

        ImGui.SameLine();

        if (!_inspectorWindow.IsOpen && ImGui.Button("Open Inspector"))
        {
            _inspectorWindow.Open();
        }
        else if (_inspectorWindow.IsOpen && ImGui.Button("Close Inspector"))
        {
            _inspectorWindow.Close();
        }

        if (!raptureTextModule->MacroEncoder.EncoderError.IsEmpty)
            ImGui.Text(raptureTextModule->MacroEncoder.EncoderError.ToString()); // TODO: EncoderError doesn't clear

        using var table = ImRaii.Table("StringMakerTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var ArrowUpButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowUp);
        var ArrowDownButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowDown);
        var TrashButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash);
        var TerminalButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Terminal);

        var entryToRemove = -1;
        var entryToMoveUp = -1;
        var entryToMoveDown = -1;
        var updateString = false;

        for (var i = 0; i < _entries.Count; i++)
        {
            var key = $"##Entry{i}";
            var entry = _entries[i];

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Type
            ImGui.SetNextItemWidth(-1);
            var type = (int)entry.Type;
            if (ImGui.Combo($"##Type{i}", ref type, ["String", "Macro", "Fixed"], 3))
            {
                entry.Type = (TextEntryType)type;
                updateString |= true;
            }

            ImGui.TableNextColumn(); // Text
            var message = entry.Message;
            if (ImGui.InputText($"##{i}_Message", ref message, 255))
            {
                entry.Message = message;
                updateString |= true;
            }

            ImGui.TableNextColumn(); // Actions

            if (i > 0)
            {
                if (ImGuiUtils.IconButton(key + "_Up", FontAwesomeIcon.ArrowUp, "Move up"))
                {
                    entryToMoveUp = i;
                }
            }
            else
            {
                ImGui.Dummy(ArrowUpButtonSize);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

            if (i < _entries.Count - 1)
            {
                if (ImGuiUtils.IconButton(key + "_Down", FontAwesomeIcon.ArrowDown, "Move down"))
                {
                    entryToMoveDown = i;
                }
            }
            else
            {
                ImGui.Dummy(ArrowDownButtonSize);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, "Delete"))
                {
                    entryToRemove = i;
                }
            }
            else
            {
                ImGuiUtils.IconButton(
                    key + "_Delete",
                    FontAwesomeIcon.Trash,
                    "Delete with shift",
                    disabled: true);
            }
        }

        table.Dispose();

        if (entryToMoveUp != -1)
        {
            var removedItem = _entries[entryToMoveUp];
            _entries.RemoveAt(entryToMoveUp);
            _entries.Insert(entryToMoveUp - 1, removedItem);
            updateString |= true;
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = _entries[entryToMoveDown];
            _entries.RemoveAt(entryToMoveDown);
            _entries.Insert(entryToMoveDown + 1, removedItem);
            updateString |= true;
        }

        if (entryToRemove != -1)
        {
            _entries.RemoveAt(entryToRemove);
            updateString |= true;
        }

        if (updateString)
        {
            UpdateInspectorString();
        }
    }

    private void UpdateInspectorString()
    {
        if (_inspectorWindow == null)
            return;

        var sb = new SeStringBuilder();

        foreach (var entry in _entries)
        {
            switch (entry.Type)
            {
                case TextEntryType.String:
                    sb.Append(entry.Message);
                    break;

                case TextEntryType.Macro:
                case TextEntryType.Fixed:
                    sb.AppendMacroString(entry.Message);
                    break;
            }
        }

        _inspectorWindow.Language = _languageProvider.ClientLanguage;
        _inspectorWindow.String = sb.ToReadOnlySeString();
    }

    private class TextEntry(TextEntryType type, string text)
    {
        public string Message { get; set; } = text;
        public TextEntryType Type { get; set; } = type;
    }

    private enum TextEntryType
    {
        String,
        Macro,
        Fixed
    }
}
