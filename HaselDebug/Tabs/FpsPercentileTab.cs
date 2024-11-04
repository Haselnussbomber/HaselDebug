using System.IO;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselDebug.Abstracts;
using ImGuiNET;
using InteropGenerator.Runtime.Attributes;

namespace HaselDebug.Tabs;

public unsafe class FpsPercentileTab : DebugTab
{
    public override void Draw()
    {
        if (ImGui.Button("Start"))
        {
            FpsPerentile.Start(Framework.Instance(), 3);
        }
        if (ImGui.Button("Stop"))
        {
            FpsPerentile.Stop(Framework.Instance());
        }
        if (ImGui.Button("Write"))
        {
            FpsPerentile.Write(Framework.Instance());
        }
        if (ImGui.Button("Open output folder"))
        {
            Util.OpenLink(new FileInfo(Framework.Instance()->ConfigPath.ToString()).Directory!.FullName);
        }
    }
}

[GenerateInterop]
public unsafe partial struct FpsPerentile
{
    [MemberFunction("48 83 EC 38 80 3D ?? ?? ?? ?? ?? 0F 29 74 24 ?? 0F 28 F1 0F 85")]
    public static partial void Start(Framework* thisPtr, int a2);

    [MemberFunction("48 83 EC 38 80 3D ?? ?? ?? ?? ?? 0F 84")]
    public static partial void Stop(Framework* thisPtr);

    [MemberFunction("48 89 5C 24 ?? 57 48 81 EC F0 04 00 00")]
    public static partial void Write(Framework* thisPtr);
}
