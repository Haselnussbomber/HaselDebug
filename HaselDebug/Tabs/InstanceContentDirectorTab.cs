using System.Collections.Generic;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;
using InstanceContentType = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentType;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class InstanceContentDirectorTab(DebugRenderer DebugRenderer, ISigScanner SigScanner, ExcelService ExcelService) : DebugTab
{

    private static readonly Dictionary<(ReadOnlySeString, InstanceContentType), nint> InstanceContentTypeVtables = [];

    public override void Draw()
    {
        if (InstanceContentTypeVtables.Count != 0)
        {
            foreach (var ((name, type), vtableAddr) in InstanceContentTypeVtables)
            {
                DebugRenderer.DrawCopyableText($"{type}: {name} @", $"+0x{vtableAddr - SigScanner.Module.BaseAddress:X} - {name}");
                ImGui.SameLine();
                DebugRenderer.DrawAddress(vtableAddr);
            }

            ImGui.Separator();
        }

        foreach (var directorPtr in EventFramework.Instance()->DirectorModule.DirectorList)
        {
            if (directorPtr.Value == null) continue;
            if ((uint)directorPtr.Value->EventHandlerInfo->EventId.ContentId > 0x8000)
                ImGui.TextUnformatted($"[{directorPtr.Value->EventHandlerInfo->EventId.ContentId}:{directorPtr.Value->EventHandlerInfo->EventId.EntryId}]");
            else
                ImGui.TextUnformatted($"[{directorPtr.Value->EventHandlerInfo->EventId.ContentId}]");
            ImGui.SameLine();
            DebugRenderer.DrawPointerType(directorPtr.Value, GetDirectorType(directorPtr), new NodeOptions() { AddressPath = new([1, (nint)directorPtr.Value]) });
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
            DebugRenderer.DrawPointerType(contentDirector, typeof(ContentDirector), new NodeOptions() { AddressPath = new([2, (nint)contentDirector]) });
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
            DebugRenderer.DrawPointerType(craftLeveEventHandler, typeof(EventHandler), new NodeOptions() { AddressPath = new([3, (nint)craftLeveEventHandler]) });
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
            DebugRenderer.DrawPointerType(publicContentDirector, GetPublicContentDirectorType((Director*)publicContentDirector), new NodeOptions() { AddressPath = new([4, (nint)publicContentDirector]) });
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
            var cfc = ExcelService.GetSheet<ContentFinderCondition>(ClientLanguage.English).GetRow(ic.Order);
            var key = (!cfc.Name.IsEmpty ? cfc.Name : instanceContentDirector->ContentDirector.Director.UnkString0.ToString(), instanceContentDirector->InstanceContentType);
            if (!InstanceContentTypeVtables.ContainsKey(key))
            {
                InstanceContentTypeVtables.Add(key, *(nint*)instanceContentDirector);
            }

            DebugRenderer.DrawPointerType(instanceContentDirector, GetInstanceContentDirectorType((Director*)instanceContentDirector), new NodeOptions() { AddressPath = new([5, (nint)instanceContentDirector]) });
        }
    }

    private Type GetDirectorType(Director* director)
    {
        if (director == null || director->EventHandlerInfo == null)
            return typeof(Director);

        return director->EventHandlerInfo->EventId.ContentId switch
        {
            EventHandlerType.InstanceContentDirector => typeof(InstanceContentDirector),
            EventHandlerType.PublicContentDirector => GetPublicContentDirectorType(director),
            _ => typeof(Director),
        };
    }

    private Type GetInstanceContentDirectorType(Director* director)
    {
        var instanceContentDirector = (InstanceContentDirector*)director;
        if (instanceContentDirector == null)
            return typeof(InstanceContentDirector);

        return instanceContentDirector->InstanceContentType switch
        {
            InstanceContentType.DeepDungeon => typeof(InstanceContentDeepDungeon),
            InstanceContentType.OceanFishing => typeof(InstanceContentOceanFishing),
            _ => typeof(InstanceContentDirector)
        };
    }

    private Type GetPublicContentDirectorType(Director* director)
    {
        var publicContentDirector = (PublicContentDirector*)director;
        if (publicContentDirector == null)
            return typeof(PublicContentDirector);

        return publicContentDirector->Type switch
        {
            PublicContentDirectorType.Bozja => typeof(PublicContentBozja),
            PublicContentDirectorType.Eureka => typeof(PublicContentEureka),
            _ => typeof(PublicContentDirector)
        };
    }
}
