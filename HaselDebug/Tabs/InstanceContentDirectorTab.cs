using System.Collections.Generic;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselDebug.Tabs;

public unsafe class InstanceContentDirectorTab(ISigScanner SigScanner, ExcelService ExcelService) : DebugTab
{

    private static readonly Dictionary<(string, InstanceContentType), nint> InstanceContentTypeVtables = [];

    public override void Draw()
    {
        foreach (var ((name, type), vtableAddr) in InstanceContentTypeVtables)
        {
            DebugUtils.DrawCopyableText($"{type}: {name} @", $"+0x{vtableAddr - SigScanner.Module.BaseAddress:X} - {name}");
            ImGui.SameLine();
            DebugUtils.DrawAddress(vtableAddr);
        }

        ImGui.Separator();

        foreach (var directorPtr in EventFramework.Instance()->DirectorModule.DirectorList)
        {
            if (directorPtr.Value == null) continue;
            if ((uint)directorPtr.Value->EventHandlerInfo->EventId.ContentId > 0x8000)
                ImGui.TextUnformatted($"[{directorPtr.Value->EventHandlerInfo->EventId.ContentId}:{directorPtr.Value->EventHandlerInfo->EventId.EntryId}]");
            else
                ImGui.TextUnformatted($"[{directorPtr.Value->EventHandlerInfo->EventId.ContentId}]");
            ImGui.SameLine();
            DebugUtils.DrawPointerType(directorPtr.Value, typeof(Director), new NodeOptions());
        }

        ImGui.TextUnformatted("ContentDirector:");
        ImGui.SameLine();
        var contentDirector = EventFramework.Instance()->GetContentDirector();
        if (contentDirector == null)
        {
            ImGui.TextUnformatted("None active");
        }
        else
        {
            DebugUtils.DrawPointerType(contentDirector, typeof(ContentDirector), new NodeOptions());
        }

        ImGui.Separator();

        ImGui.TextUnformatted("CraftLeveEventHandler:");
        ImGui.SameLine();
        var craftLeveEventHandler = EventFramework.Instance()->EventHandlerModule.CraftLeveEventHandler;
        if (craftLeveEventHandler == null)
        {
            ImGui.TextUnformatted("None active");
        }
        else
        {
            DebugUtils.DrawPointerType((nint)craftLeveEventHandler, typeof(EventHandler), new NodeOptions());
        }

        ImGui.Separator();

        ImGui.TextUnformatted("PublicContentDirector:");
        ImGui.SameLine();
        var publicContentDirector = EventFramework.Instance()->GetPublicContentDirector();
        if (publicContentDirector == null)
        {
            ImGui.TextUnformatted("None active");
        }
        else
        {
            DebugUtils.DrawPointerType((nint)publicContentDirector, typeof(PublicContentDirector), new NodeOptions());
        }

        ImGui.Separator();

        ImGui.TextUnformatted("InstanceContentDirector:");
        ImGui.SameLine();
        var instanceContentDirector = EventFramework.Instance()->GetInstanceContentDirector();
        if (instanceContentDirector == null)
        {
            ImGui.TextUnformatted("None active");
        }
        else
        {
            var ic = ExcelService.GetSheet<InstanceContent>().GetRow(instanceContentDirector->ContentDirector.Director.ContentId);
            var cfc = ExcelService.GetSheet<ContentFinderCondition>(ClientLanguage.English).GetRow(ic?.Order ?? 0);
            var key = (cfc?.Name ?? instanceContentDirector->ContentDirector.Director.UnkString0.ToString(), instanceContentDirector->InstanceContentType);
            if (!InstanceContentTypeVtables.ContainsKey(key))
            {
                InstanceContentTypeVtables.Add(key, *(nint*)instanceContentDirector);
            }

            DebugUtils.DrawPointerType((nint)instanceContentDirector, typeof(InstanceContentDirector), new NodeOptions());
        }
    }
}
