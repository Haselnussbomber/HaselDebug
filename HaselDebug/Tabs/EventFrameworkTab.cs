using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.STD;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text;
using Lumina.Text.ReadOnly;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselDebug.Tabs;

public unsafe class EventFrameworkTab(DebugRenderer DebugRenderer, TextService TextService) : DebugTab
{
    public override string GetTitle() => "EventFramework";
    public override bool DrawInChild => false;

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

        DebugRenderer.DrawPointerType(eventFramework, typeof(EventFramework), new NodeOptions());

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
            DebugRenderer.DrawAddress(director);
            ImGui.SameLine();

            ImGui.TextUnformatted(director->EventHandlerInfo->EventId.ContentId.ToString());
            ImGui.SameLine();

            if (director->IconId != 0)
                DebugRenderer.DrawIcon(director->IconId);

            ReadOnlySeString? title = null;
            if (!director->Title.IsEmpty)
                title = new ReadOnlySeString(director->Title.AsSpan().ToArray());

            DebugRenderer.DrawPointerType(director, typeof(Director), new NodeOptions()
            {
                Title = title
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

            DebugRenderer.DrawAddress(eventHandler);
            ImGui.SameLine(110);

            ImGui.TextUnformatted(kv.Item1.ToString("X4"));
            ImGui.SameLine(155);

            ReadOnlySeString? title = null;

            if (type == EventHandlerType.Quest)
            {
                var questId = kv.Item2.Value->Info.EventId.Id;

                // i don't think that's accurate
                /*
                var isQuestComplete = QuestManager.IsQuestComplete(questId);
                var iconId = ExcelService.GetRow<Quest>(questId)!.EventIconType.Value!.MapIconAvailable + (isQuestComplete ? 5u : 1u);
                DebugUtils.DrawIcon(TextureProvider, iconId);
                */

                title = new SeStringBuilder().Append($"{type} {questId} ({TextService.GetQuestName(questId)})").ToReadOnlySeString();
            }
            else
            {
                if (eventHandler->IconId != 0)
                    DebugRenderer.DrawIcon(eventHandler->IconId);
            }

            if (title == null)
                title = new SeStringBuilder().Append($"{type} {kv.Item2.Value->Info.EventId.Id}").ToReadOnlySeString();

            DebugRenderer.DrawPointerType(eventHandler, typeof(EventHandler), new NodeOptions()
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

        foreach (EventSceneTaskInterface* task in tasks)
        {
            DebugRenderer.DrawAddress(task);
            ImGui.SameLine();
            ImGui.TextUnformatted(task->Type.ToString());
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

        using var table = ImRaii.Table($"EventObjectsTable_{(nint)eventHandler:X}", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
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
            DebugRenderer.DrawPointerType(eventObject.Value, typeof(GameObject), new NodeOptions());
            i++;
        }
    }

    private void DrawCustomTalkTexts(EventHandler* eventHandler)
    {
        if (eventHandler->Info.EventId.ContentId != EventHandlerType.CustomTalk)
            return;

        var texts = *(StdMap<uint, LuaText>*)((nint)eventHandler + 0x310); // TODO: contribute
        if (texts.Count == 0)
            return;

        using var treenode = ImRaii.TreeNode($"Texts ({texts.Count})###CustomTalkTexts_{(nint)eventHandler:X}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treenode)
            return;

        using var table = ImRaii.Table($"CustomTalkTextsTable_{(nint)eventHandler:X}", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(-1, 500));
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
            DebugRenderer.DrawUtf8String((nint)(&text.Item2.Key), new NodeOptions());

            ImGui.TableNextColumn(); // Value
            DebugRenderer.DrawUtf8String((nint)(&text.Item2.Value), new NodeOptions());
        }
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct LuaText
{
    [FieldOffset(0)] public Utf8String Key;
    [FieldOffset(0x68)] public Utf8String Value;
}
