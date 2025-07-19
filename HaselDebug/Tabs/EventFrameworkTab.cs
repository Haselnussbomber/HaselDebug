using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EventFrameworkTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly IGameInteropProvider _gameInteropProvider;

    private readonly List<(DateTime, nint, EventSceneTaskInterface)> _taskTypeHistory = [];
    private Hook<EventSceneModuleTaskManager.Delegates.AddTask>? _addTaskHook;
    private bool _logEnabled;
    private bool _isInitialized;

    public override string Title => "EventFramework";
    public override bool DrawInChild => false;

    private void Initialize()
    {
        _addTaskHook = _gameInteropProvider.HookFromAddress<EventSceneModuleTaskManager.Delegates.AddTask>(
            EventSceneModuleTaskManager.MemberFunctionPointers.AddTask,
            AddTaskDetour);

        _addTaskHook.Enable();
    }

    public void Dispose()
    {
        _addTaskHook?.Dispose();
    }

    private void AddTaskDetour(EventSceneModuleTaskManager* thisPtr, EventSceneTaskInterface* task)
    {
        if (_logEnabled)
        {
            _taskTypeHistory.Add((DateTime.Now, (nint)task, *task));
        }

        _addTaskHook!.Original(thisPtr, task);
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

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

            if (director->IconId != 0)
                _debugRenderer.DrawIcon(director->IconId);

            _debugRenderer.DrawPointerType(director, typeof(EventHandler), new NodeOptions() { UseSimpleEventHandlerName = true });
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

            _debugRenderer.DrawPointerType(eventHandler, typeof(EventHandler), new NodeOptions() { UseSimpleEventHandlerName = true });

            using var indent = ImRaii.PushIndent();
            DrawEventObjects(eventHandler);
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
}
