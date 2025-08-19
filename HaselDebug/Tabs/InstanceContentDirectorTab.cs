using System.Collections.Generic;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;
using InstanceContentType = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentType;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InstanceContentDirectorTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ISigScanner _sigScanner;
    private readonly ExcelService _excelService;

    private static readonly Dictionary<(ReadOnlySeString, InstanceContentType), nint> InstanceContentTypeVtables = [];

    public override void Draw()
    {
        if (InstanceContentTypeVtables.Count != 0)
        {
            foreach (var ((name, type), vtableAddr) in InstanceContentTypeVtables)
            {
                ImGuiUtilsEx.DrawCopyableText($"{type}: {name} @", $"+0x{vtableAddr - _sigScanner.Module.BaseAddress:X} - {name}");
                ImGui.SameLine();
                _debugRenderer.DrawAddress(vtableAddr);
            }

            ImGui.Separator();
        }

        foreach (var directorPtr in EventFramework.Instance()->DirectorModule.DirectorList)
        {
            if (directorPtr.Value == null) continue;
            if ((uint)directorPtr.Value->EventHandlerInfo->EventId.ContentId > 0x8000)
                ImGui.Text($"[{directorPtr.Value->EventHandlerInfo->EventId.ContentId}:{directorPtr.Value->EventHandlerInfo->EventId.EntryId}]");
            else
                ImGui.Text($"[{directorPtr.Value->EventHandlerInfo->EventId.ContentId}]");
            ImGui.SameLine();
            _debugRenderer.DrawPointerType(directorPtr.Value, typeof(Director), new NodeOptions() { AddressPath = new([1, (nint)directorPtr.Value]) });
        }

        ImGui.Text("ContentDirector:");
        ImGui.SameLine();
        var contentDirector = EventFramework.Instance()->GetContentDirector();
        if (contentDirector == null)
        {
            ImGui.Text("None active");
        }
        else
        {
            _debugRenderer.DrawPointerType(contentDirector, typeof(ContentDirector), new NodeOptions() { AddressPath = new([2, (nint)contentDirector]) });
        }

        ImGui.Separator();

        ImGui.Text("CraftLeveEventHandler:");
        ImGui.SameLine();
        var craftLeveEventHandler = EventFramework.Instance()->EventHandlerModule.CraftLeveEventHandler;
        if (craftLeveEventHandler == null)
        {
            ImGui.Text("None active");
        }
        else
        {
            _debugRenderer.DrawPointerType(craftLeveEventHandler, typeof(EventHandler), new NodeOptions() { AddressPath = new([3, (nint)craftLeveEventHandler]) });
        }

        ImGui.Separator();

        ImGui.Text("PublicContentDirector:");
        ImGui.SameLine();
        var publicContentDirector = EventFramework.Instance()->GetPublicContentDirector();
        if (publicContentDirector == null)
        {
            ImGui.Text("None active");
        }
        else
        {
            _debugRenderer.DrawPointerType(publicContentDirector, typeof(EventHandler), new NodeOptions() { AddressPath = new([4, (nint)publicContentDirector]) });
        }

        ImGui.Separator();

        ImGui.Text("InstanceContentDirector:");
        ImGui.SameLine();
        var instanceContentDirector = EventFramework.Instance()->GetInstanceContentDirector();
        if (instanceContentDirector == null)
        {
            ImGui.Text("None active");
        }
        else
        {
            var ic = _excelService.GetSheet<InstanceContent>().GetRow(instanceContentDirector->ContentDirector.Director.ContentId);
            var cfc = _excelService.GetSheet<ContentFinderCondition>(ClientLanguage.English).GetRow(ic.ContentFinderCondition.RowId);
            var key = (!cfc.Name.IsEmpty ? cfc.Name : instanceContentDirector->ContentDirector.Director.UnkString0.ToString(), instanceContentDirector->InstanceContentType);
            if (!InstanceContentTypeVtables.ContainsKey(key))
            {
                InstanceContentTypeVtables.Add(key, *(nint*)instanceContentDirector);
            }

            _debugRenderer.DrawPointerType(instanceContentDirector, typeof(EventHandler), new NodeOptions() { AddressPath = new([5, (nint)instanceContentDirector]) });
        }
    }
}
