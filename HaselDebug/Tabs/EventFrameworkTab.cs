using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.STD;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class EventFrameworkTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly Hook<EventSceneModuleTaskManager.Delegates.AddTask> _addTaskHook;
    private readonly List<(DateTime, nint, EventSceneTaskInterface)> _taskTypeHistory = [];
    private bool _logEnabled;

    public override string Title => "EventFramework";
    public override bool DrawInChild => false;

    public EventFrameworkTab(DebugRenderer DebugRenderer, TextService TextService, IGameInteropProvider GameInteropProvider)
    {
        _debugRenderer = DebugRenderer;
        _textService = TextService;

        _addTaskHook = GameInteropProvider.HookFromAddress<EventSceneModuleTaskManager.Delegates.AddTask>(
            EventSceneModuleTaskManager.MemberFunctionPointers.AddTask,
            AddTaskDetour);

        _addTaskHook.Enable();
    }

    public void Dispose()
    {
        _addTaskHook.Dispose();
        GC.SuppressFinalize(this);
    }

    private void AddTaskDetour(EventSceneModuleTaskManager* thisPtr, EventSceneTaskInterface* task)
    {
        if (_logEnabled)
        {
            _taskTypeHistory.Add((DateTime.Now, (nint)task, *task));
        }

        _addTaskHook.Original(thisPtr, task);
    }

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("EventFrameworkTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("EventFrameworkTabs");
        if (!tabs) return;

        DrawOverviewTab();
        DrawDirectorsTab();
        DrawEventHandlersTab();
        DrawTasksTab();
    }

    public void DrawOverviewTab()
    {
        using var tab = ImRaii.TabItem("Overview");
        if (!tab) return;

        using var child = ImRaii.Child("OverviewTab", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var eventFramework = EventFramework.Instance();

        _debugRenderer.DrawPointerType(eventFramework, typeof(EventFramework), new NodeOptions());

        ImGui.Separator();

        ImGui.TextUnformatted($"CurrentContentType: {EventFramework.GetCurrentContentType()}");
        ImGui.TextUnformatted($"CurrentContentId: {EventFramework.GetCurrentContentId()}");

        if (eventFramework->DirectorModule.ActiveContentDirector != null)
            ImGui.TextUnformatted($"ActiveContentDirector: 0x{(nint)eventFramework->DirectorModule.ActiveContentDirector:X}");
    }

    private void DrawDirectorsTab()
    {
        var directorList = EventFramework.Instance()->DirectorModule.DirectorList;

        using var tab = ImRaii.TabItem($"Directors ({directorList.Count})###Directors");
        if (!tab) return;

        using var child = ImRaii.Child("DirectorsTab", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        foreach (Director* director in directorList)
        {
            _debugRenderer.DrawAddress(director);
            ImGui.SameLine();

            ImGui.TextUnformatted(director->EventHandlerInfo->EventId.ContentId.ToString());
            ImGui.SameLine();

            if (director->IconId != 0)
                _debugRenderer.DrawIcon(director->IconId);

            ReadOnlySeString? title = null;
            if (!director->Title.IsEmpty)
                title = new ReadOnlySeString(director->Title.AsSpan().ToArray());

            _debugRenderer.DrawPointerType(director, typeof(Director), new NodeOptions()
            {
                SeStringTitle = title
            });
        }
    }

    private void DrawEventHandlersTab()
    {
        using var tab = ImRaii.TabItem("EventHandlers");
        if (!tab) return;

        using var child = ImRaii.Child("EventHandlersTab", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        foreach (var kv in EventFramework.Instance()->EventHandlerModule.EventHandlerMap)
        {
            var eventHandler = kv.Item2.Value;
            var type = eventHandler->Info.EventId.ContentId;

            _debugRenderer.DrawAddress(eventHandler);
            ImGui.SameLine(110);

            ImGui.TextUnformatted(kv.Item1.ToString("X4"));
            ImGui.SameLine(155);

            string? title = null;

            if (type == EventHandlerType.Quest)
            {
                var questId = kv.Item2.Value->Info.EventId.Id;

                // i don't think that's accurate
                /*
                var isQuestComplete = QuestManager.IsQuestComplete(questId);
                var iconId = ExcelService.GetRow<Quest>(questId)!.EventIconType.Value!.MapIconAvailable + (isQuestComplete ? 5u : 1u);
                DebugUtils.DrawIcon(TextureProvider, iconId);
                */

                title = $"{type} {questId} ({_textService.GetQuestName(questId)})";
            }
            else
            {
                if (eventHandler->IconId != 0)
                    _debugRenderer.DrawIcon(eventHandler->IconId);
            }

            if (title == null)
                title = $"{type} {kv.Item2.Value->Info.EventId.Id}";

            _debugRenderer.DrawPointerType(eventHandler, typeof(EventHandler), new NodeOptions()
            {
                Title = title
            });

            using var indent = ImRaii.PushIndent();
            DrawEventObjects(eventHandler);
            DrawCustomTalkTexts(eventHandler);
        }
    }

    private void DrawTasksTab()
    {
        var tasks = EventFramework.Instance()->EventSceneModule.TaskManager.Tasks;

        using var tab = ImRaii.TabItem($"Tasks ({tasks.Count})###Tasks");
        if (!tab) return;

        using var child = ImRaii.Child("TasksTab", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        ImGui.TextUnformatted("Current Tasks:");

        foreach (EventSceneTaskInterface* task in tasks)
        {
            _debugRenderer.DrawAddress(task);
            ImGui.SameLine();
            ImGui.TextUnformatted($"Type: {task->Type}, Flags: {task->Flags}");
        }

        ImGui.Separator();

        ImGui.Checkbox("Enable Logging", ref _logEnabled);

        ImGui.TextUnformatted("History:");

        foreach (var (time, _, task) in _taskTypeHistory)
        {
            ImGui.TextUnformatted($"[{time}] Type: {task.Type}, Flags: {task.Flags}");
        }

        if (ImGui.Button("Reset History"))
        {
            _taskTypeHistory.Clear();
        }
    }

    private void DrawEventObjects(EventHandler* eventHandler)
    {
        var eventObjects = eventHandler->EventObjects;
        if (eventObjects.Count == 0)
            return;

        using var treenode = ImRaii.TreeNode($"EventObjects ({eventObjects.Count})###EventObjects_{(nint)eventHandler:X}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treenode)
            return;

        using var table = ImRaii.Table($"EventObjectsTable_{(nint)eventHandler:X}", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Object", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        foreach (var eventObject in eventObjects)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Object
            _debugRenderer.DrawPointerType(eventObject.Value, typeof(GameObject), new NodeOptions());
            i++;
        }
    }

    private void DrawCustomTalkTexts(EventHandler* eventHandler)
    {
        if (eventHandler->Info.EventId.ContentId != EventHandlerType.CustomTalk)
            return;

        var texts = *(StdMap<uint, LuaText>*)((nint)eventHandler + 0x310); // TODO: contribute, StdPair?
        if (texts.Count == 0)
            return;

        using var treenode = ImRaii.TreeNode($"Texts ({texts.Count})###CustomTalkTexts_{(nint)eventHandler:X}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treenode)
            return;

        using var table = ImRaii.Table($"CustomTalkTextsTable_{(nint)eventHandler:X}", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(-1, 500));
        if (!table)
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 2);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        foreach (var text in texts)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(text.Item1.ToString());

            ImGui.TableNextColumn(); // Key
            _debugRenderer.DrawUtf8String((nint)(&text.Item2.Key), new NodeOptions());

            ImGui.TableNextColumn(); // Value
            _debugRenderer.DrawUtf8String((nint)(&text.Item2.Value), new NodeOptions());
        }
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct LuaText
{
    [FieldOffset(0)] public Utf8String Key;
    [FieldOffset(0x68)] public Utf8String Value;
}
