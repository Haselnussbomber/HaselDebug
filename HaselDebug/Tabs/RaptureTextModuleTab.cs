using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Text;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class RaptureTextModuleTab : DebugTab, IDisposable
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
        new TextEntry(TextEntryType.Macro, "<fixed(1,105)>"), // auto-translate needs to be evaluated as macro
    ];

    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly SeStringEvaluatorService _seStringEvaluator;
    private readonly LanguageProvider _languageProvider;

    private SeStringInspectorWindow? _inspectorWindow;

    public override bool DrawInChild => false;

    public RaptureTextModuleTab(
        DebugRenderer debugRenderer,
        WindowManager windowManager,
        SeStringEvaluatorService seStringEvaluator,
        LanguageProvider languageProvider)
    {
        _debugRenderer = debugRenderer;
        _windowManager = windowManager;
        _seStringEvaluator = seStringEvaluator;
        _languageProvider = languageProvider;

        _languageProvider.LanguageChanged += OnLanguageChanged;
    }

    public void Dispose()
    {
        _languageProvider.LanguageChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        if (_inspectorWindow != null)
            _inspectorWindow.Language = _languageProvider.ClientLanguage;
    }

    public override unsafe void Draw()
    {
        using var hostchild = ImRaii.Child("RaptureTextModuleTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("RaptureTextModuleTab_TabBar");
        if (!tabs) return;

        DrawGlobalParameters();
        DrawDefinitions();
        DrawStringMaker();
    }

    private void DrawGlobalParameters()
    {
        using var tab = ImRaii.TabItem("GlobalParameters");
        if (!tab) return;

        using var table = ImRaii.Table("GlobalParametersTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ValuePtr", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var deque = RaptureTextModule.Instance()->GlobalParameters;
        for (var i = 0u; i < deque.MySize; i++)
        {
            var item = deque[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Type
            ImGui.TextUnformatted(item.Type.ToString());

            ImGui.TableNextColumn(); // ValuePtr
            _debugRenderer.DrawAddress(item.ValuePtr);

            ImGui.TableNextColumn(); // Value
            switch (item.Type)
            {
                case TextParameterType.Integer:
                    _debugRenderer.DrawCopyableText($"0x{item.IntValue:X}");
                    ImGui.SameLine();
                    _debugRenderer.DrawCopyableText(item.IntValue.ToString());
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
                        ImGui.TextUnformatted("null");
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
            ImGui.TextUnformatted(i switch
            {
                0 => "Player Name",
                1 => "Log Message Name 1",
                2 => "Log Message Name 2",
                3 => "Player Sex",
                10 => "Eorzea Time Hours",
                11 => "Eorzea Time Minutes",
                12 => "Log Text Colors - Chat 1 - Say",
                13 => "Log Text Colors - Chat 1 - Shout",
                14 => "Log Text Colors - Chat 1 - Tell",
                15 => "Log Text Colors - Chat 1 - Party",
                16 => "Log Text Colors - Chat 1 - Alliance",
                17 => "Log Text Colors - Chat 2 - LS1",
                18 => "Log Text Colors - Chat 2 - LS2",
                19 => "Log Text Colors - Chat 2 - LS3",
                20 => "Log Text Colors - Chat 2 - LS4",
                21 => "Log Text Colors - Chat 2 - LS5",
                22 => "Log Text Colors - Chat 2 - LS6",
                23 => "Log Text Colors - Chat 2 - LS7",
                24 => "Log Text Colors - Chat 2 - LS8",
                25 => "Log Text Colors - Chat 2 - Free Company",
                26 => "Log Text Colors - Chat 2 - PvP Team",
                29 or 30 => "Log Text Colors - Chat 1 - Emotes",
                31 => "Log Text Colors - Chat 1 - Yell",
                34 => "Log Text Colors - Chat 2 - CWLS1",
                27 => "Log Text Colors - General - PvP Team Announcements",
                28 => "Log Text Colors - Chat 2 - Novice Network",
                32 => "Log Text Colors - General - Free Company Announcements",
                33 => "Log Text Colors - General - Novice Network Announcements",
                35 => "Log Text Colors - Battle - Damage Dealt",
                36 => "Log Text Colors - Battle - Missed Attacks",
                37 => "Log Text Colors - Battle - Actions",
                38 => "Log Text Colors - Battle - Items",
                39 => "Log Text Colors - Battle - Healing",
                40 => "Log Text Colors - Battle - Enchanting Effects",
                41 => "Log Text Colors - Battle - Enfeebing Effects",
                42 => "Log Text Colors - General - Echo",
                43 => "Log Text Colors - General - System Messages",
                54 => "Companion Name",
                56 => "Log Text Colors - General - Battle System Messages",
                57 => "Log Text Colors - General - Gathering System Messages",
                58 => "Log Text Colors - General - Error Messages",
                59 => "Log Text Colors - General - NPC Dialogue",
                60 => "Log Text Colors - General - Item Drops",
                61 => "Log Text Colors - General - Level Up",
                62 => "Log Text Colors - General - Loot",
                63 => "Log Text Colors - General - Synthesis",
                64 => "Log Text Colors - General - Gathering",
                67 => "Player ClassJobId",
                68 => "Player Level",
                70 => "Player Race",
                71 => "Player Sycned Level",
                77 => "Client/Plattform?",
                82 => "Datacenter Region (see WorldDCGroupType sheet)",
                92 => "TerritoryType Id",
                95 => "Log Role Color - Tank (LogColorRoleTank)",
                97 => "Log Role Color - Healer (LogColorRoleHealer)",
                99 => "Log Role Color - DPS (LogColorRoleDPS)",
                101 => "Log Role Color - Other (LogColorOtherClass)",
                102 => "Has Login Security Token (set in UIModule.HandlePacket case 0)",
                _ => "",
            });
        }
    }

    private void DrawDefinitions()
    {
        using var tab = ImRaii.TabItem("Definitions");
        if (!tab) return;

        using var table = ImRaii.Table("DefinitionsTable", 13, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Code", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("TotalParamCount", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("ParamCount", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("IsTerminated", ImGuiTableColumnFlags.WidthFixed, 60);
        for (var i = 0; i < 7; i++)
            ImGui.TableSetupColumn($"{i}", ImGuiTableColumnFlags.WidthFixed, 20);

        ImGui.TableSetupScrollFreeze(13, 1);
        ImGui.TableHeadersRow();

        var raptureTextModule = RaptureTextModule.Instance();

        foreach (var item in raptureTextModule->TextModule.MacroEncoder.MacroCodeMap)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Code
            ImGui.TextUnformatted(item.Item1.ToString());

            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted($"0x{item.Item2.Id:X}");

            ImGui.TableNextColumn(); // TotalParamCount
            ImGui.TextUnformatted(item.Item2.TotalParamCount.ToString());

            ImGui.TableNextColumn(); // ParamCount
            ImGui.TextUnformatted(item.Item2.ParamCount.ToString());

            ImGui.TableNextColumn(); // IsTerminated
            ImGui.TextUnformatted(item.Item2.IsTerminated.ToString());

            ImGui.TableNextColumn();
            for (var i = 0; i < 7; i++)
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(((char)item.Item2.ParamTypes[i]).ToString());
            }
        }
    }

    private void DrawStringMaker()
    {
        using var tab = ImRaii.TabItem("StringMaker");
        if (!tab) return;

        if (_inspectorWindow == null)
        {
            _inspectorWindow = _windowManager.CreateOrOpen("StringMaker Preview", () => new SeStringInspectorWindow(_windowManager, _debugRenderer, _seStringEvaluator, "", _languageProvider.ClientLanguage, "StringMaker Preview"));
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

                        RaptureTextModule.Instance()->TextModule.ProcessMacroCode(temp2, temp->StringPtr);
                        var out1 = PronounModule.Instance()->ProcessString(temp2, true);
                        var out2 = PronounModule.Instance()->ProcessString(out1, false);

                        output->Append(out2);
                        break;
                }
            }

            RaptureLogModule.Instance()->PrintString(output->StringPtr);
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

        if (!_inspectorWindow.IsOpen && ImGui.Button("Open Inspector"))
        {
            _inspectorWindow.Open();
        }
        else if (_inspectorWindow.IsOpen && ImGui.Button("Close Inspector"))
        {
            _inspectorWindow.Close();
        }

        if (!raptureTextModule->MacroEncoder.EncoderError.IsEmpty)
            ImGui.TextUnformatted(raptureTextModule->MacroEncoder.EncoderError.ToString()); // TODO: EncoderError doesn't clear

        using var table = ImRaii.Table("StringMakerTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
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
