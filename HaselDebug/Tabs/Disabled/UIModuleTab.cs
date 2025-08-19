namespace HaselDebug.Tabs;
/*
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0xEE030)]
[VirtualTable("48 8D 0D ?? ?? ?? ?? 49 89 46 10", 3)]
public unsafe partial struct HaselDebugUIModuleModules
{
    public static HaselDebugUIModuleModules* Instance() => (HaselDebugUIModuleModules*)Framework.Instance()->GetUIModule();
    
    [VirtualFunction(12)]
    public partial UserFileEvent* GetRaptureMacroModule();
    
    [VirtualFunction(13)]
    public partial UserFileEvent* GetRaptureHotbarModule();
    
    [VirtualFunction(14)]
    public partial UserFileEvent* GetRaptureGearsetModule();
    
    [VirtualFunction(15)]
    public partial UserFileEvent* GetAcquaintanceModule();
    
    [VirtualFunction(16)]
    public partial UserFileEvent* GetItemOrderModule();

    [VirtualFunction(17)]
    public partial UserFileEvent* GetItemFinderModule();

    [VirtualFunction(19)]
    public partial UserFileEvent* GetAddonConfig();
    
    [VirtualFunction(20)]
    public partial UserFileEvent* GetUiSavePackModule();
    
    [VirtualFunction(21)]
    public partial UserFileEvent* GetLetterDataModule();
    
    [VirtualFunction(22)]
    public partial UserFileEvent* GetRetainerTaskDataModule();
    
    [VirtualFunction(23)]
    public partial UserFileEvent* GetFlagStatusModule();
    
    [VirtualFunction(24)]
    public partial UserFileEvent* GetRecipeFavoriteModule();
    
    [VirtualFunction(25)]
    public partial UserFileEvent* GetCraftModule();
    
    [VirtualFunction(26)]
    public partial UserFileEvent* GetRaptureUiDataModule();

    [VirtualFunction(29)]
    public partial UserFileEvent* GetGoldSaucerModule();

    [VirtualFunction(30)]
    public partial UserFileEvent* GetRaptureTeleportHistory();
    
    [VirtualFunction(31)]
    public partial UserFileEvent* GetItemContextCustomizeModule();
    
    [VirtualFunction(33)]
    public partial UserFileEvent* GetPvpSetModule();
    
    [VirtualFunction(42)]
    public partial UserFileEvent* GetEmoteHistoryModule();
    
    [VirtualFunction(43)]
    public partial UserFileEvent* GetMinionListModule();

    [VirtualFunction(44)]
    public partial UserFileEvent* GetMountListModule();
    
    [VirtualFunction(45)]
    public partial UserFileEvent* GetEmjModule();
    
    [VirtualFunction(46)]
    public partial UserFileEvent* GetAozNoteModule();

    [VirtualFunction(47)]
    public partial UserFileEvent* GetCrossWorldLinkShellModule();
    
    [VirtualFunction(48)]
    public partial UserFileEvent* GetAchievementListModule();

    [VirtualFunction(49)]
    public partial UserFileEvent* GetGroupPoseModule();

    [VirtualFunction(50)]
    public partial UserFileEvent* GetFieldMarkerModule();

    [VirtualFunction(52)]
    public partial UserFileEvent* GetMycNoteModule();

    [VirtualFunction(53)]
    public partial UserFileEvent* GetOrnamentListModule();
    
    [VirtualFunction(54)]
    public partial UserFileEvent* GetMycItemModule();

    [VirtualFunction(55)]
    public partial UserFileEvent* GetGroupStampModule();

    [VirtualFunction(57)]
    public partial UserFileEvent* GetMcAggreModule();

    [VirtualFunction(58)]
    public partial UserFileEvent* GetRetainerCommentModule();

    [VirtualFunction(59)]
    public partial UserFileEvent* GetBannerModule();
    
    [VirtualFunction(60)]
    public partial UserFileEvent* GetAdventureNoteModule();
    
    [VirtualFunction(61)]
    public partial UserFileEvent* GetAkatsukiNoteModule();

    [VirtualFunction(62)]
    public partial UserFileEvent* GetVVDNoteModule();

    [VirtualFunction(63)]
    public partial UserFileEvent* GetVVDActionModule();
    
    [VirtualFunction(64)]
    public partial UserFileEvent* GetTofuModule();

    [VirtualFunction(65)]
    public partial UserFileEvent* GetFishingModule();

    [VirtualFunction(69)]
    public partial UserFileEvent* GetLogFilterConfig();
}

public unsafe class UIModuleTab : DebugTab
{

    private readonly MethodInfo[] _modules;
    public delegate nint GetModuleDelegate(HaselDebugUIModuleModules* module);

    private readonly Dictionary<string, (int VfIndex, int FileType, int FileSize, int FileVersion, int DataSize, nint Offset)> _savedModules = new() {
        // Patch 6.51-6.57
        // TODO: add new ones
        ["MACRO.DAT"] = (12, 0x1, 0x46000, 0x2, 0x46000, 0x4E70),
        ["HOTBAR.DAT"] = (13, 0x2, 0x32000, 0x4, 0x32000, 0x56918),
        ["GEARSET.DAT"] = (14, 0x5, 0xB0C5, 0x6C, 0x10000, 0x7F210),
        ["ACQ.DAT"] = (15, 0x6, 0x1000, 0x64, 0x1000, 0x8A880),
        ["ITEMODR.DAT"] = (16, 0x7, 0x3AF3, 0x68, 0x3BE9, 0x8B978),
        ["ITEMFDR.DAT"] = (17, 0x8, 0x3D0E, 0xCA, 0x3D1B, 0x8BA50),
        ["ADDON.DAT"] = (19, 0x0, 0x12000, 0x69, 0x12000, 0x8CC20),
        ["UISAVE.DAT"] = (20, 0x9, 0x1A000, 0x1, 0x1A000, 0x8D1B0),
        ["LETTER.DAT"] = (21, 0x0, 0xA14, 0x65, 0xA80, 0x8D200),
        ["RETTASK.DAT"] = (22, 0x1, 0xA9, 0x65, 0xA9, 0x8DC48),
        ["FLAGS.DAT"] = (23, 0x2, 0x200, 0x64, 0x200, 0x8DCF8),
        ["RCFAV.DAT"] = (24, 0x3, 0x143, 0x64, 0x145, 0x8DFA0),
        ["CRAFT.DAT"] = (25, 0x10, 0x180, 0x1, 0x180, 0x8E128),
        ["UIDATA.DAT"] = (26, 0x4, 0x6000, 0x64, 0x6000, 0x8E180),
        ["GS.DAT"] = (29, 0xA, 0x289, 0x67, 0x28A, 0x93CC8),
        ["TLPH.DAT"] = (30, 0x5, 0x7B, 0x65, 0x7D, 0x93F90),
        ["ITCC.DAT"] = (31, 0x6, 0x121, 0x5A, 0x121, 0x94050),
        ["PVPSET.DAT"] = (33, 0x7, 0x64, 0xFA, 0x400, 0x94260),
        ["EMTH.DAT"] = (41, 0x8, 0x137, 0x66, 0x137, 0x94318),
        ["MNONLST.DAT"] = (42, 0x9, 0x55, 0x66, 0x55, 0x94490),
        ["MUNTLST.DAT"] = (43, 0xA, 0x55, 0x66, 0x55, 0x94528),
        ["EMJ.DAT"] = (44, 0xB, 0xBC, 0x0, 0xBC, 0x945C0),
        ["AOZNOTE.DAT"] = (45, 0xC, 0x722, 0x3, 0x722, 0x94680),
        ["CWLS.DAT"] = (46, 0xD, 0x5B1, 0x3, 0x5B1, 0x953A8),
        ["ACHVLST.DAT"] = (47, 0xE, 0x41, 0x65, 0x41, 0x95998),
        ["GRPPOS.DAT"] = (48, 0xF, 0xF1, 0x64, 0xF1, 0x95A20),
        ["FMARKER.DAT"] = (49, 0x11, 0xC44, 0x65, 0xC44, 0x95B50),
        ["MYCNOT.DAT"] = (51, 0x12, 0x6F, 0x65, 0x6F, 0x967D8),
        ["ORNMLST.DAT"] = (52, 0x13, 0x17, 0x65, 0x17, 0x96888),
        ["MYCITEM.DAT"] = (53, 0x14, 0xAED, 0x1, 0xAED, 0x968E0),
        ["GPSTAMP.DAT"] = (54, 0x15, 0x92B5, 0x66, 0x92B5, 0x969F8),
        ["MCAGGRE.DAT"] = (56, 0x0, 0x274, 0x64, 0x274, 0xA01E8),
        ["RTNR.DAT"] = (57, 0x16, 0x561, 0x66, 0x561, 0xA0490),
        ["BANNER.DAT"] = (58, 0x17, 0x5018, 0x0, 0x5018, 0xA0A30),
        ["ADVNOTE.DAT"] = (59, 0x18, 0x17, 0x64, 0x17, 0xA0A78),
        ["AKTKNOT.DAT"] = (60, 0x19, 0xA1, 0x65, 0xA1, 0xA0AD0),
        ["VVDNOTE.DAT"] = (61, 0x1A, 0x25, 0x12C, 0x25, 0xA0BB0),
        ["VVDACT.DAT"] = (62, 0x1A, 0x3, 0x64, 0x3, 0xA0C18),
        ["TOFU.DAT"] = (63, 0x1C, 0x4, 0x0, 0x4, 0xA0C60),
        ["FISHING.DAT"] = (64, 0x1D, 0x6C, 0x64, 0x6C, 0xA0CA8),
        ["LOGFLTR.DAT"] = (68, 0x4, 0x800, 0x3, 0x800, 0x8CC88),
    };

    public unsafe UIModuleTab()
    {
        _modules = typeof(HaselDebugUIModuleModules)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(mi => mi.Name.StartsWith("Get") && mi.GetParameters().Length == 0 && !mi.ReturnType.IsVoid())
            .ToArray();
    }

    public override void Draw()
    {
        var uiModule = HaselDebugUIModuleModules.Instance();
        if (uiModule == null)
        {
            ImGui.Text("No UIModule");
            return;
        }

        using var table = ImRaii.Table("UIModuleModules", 9);
        if (!table) return;

        ImGui.TableSetupColumn("ModuleName");
        ImGui.TableSetupColumn("FileName");
        ImGui.TableSetupColumn("VfIndex", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("FileType", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("FileSize", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("FileVersion", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("DataSize", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();
         
        foreach (var methodInfo in _modules)
        {
            var vfa = methodInfo.GetCustomAttribute<VirtualFunctionAttribute>();
            if (vfa == null)
                continue;

            var elementType = methodInfo.ReturnType.GetElementType();
            if (elementType == null || elementType.StructLayoutAttribute == null)
                continue;

            var vfPointer = *(nint*)((nint)uiModule->VTable + 8 * vfa.Index);
            var vfDelegate = Marshal.GetDelegateForFunctionPointer<GetModuleDelegate>(vfPointer);
            var address = vfDelegate?.Invoke(uiModule);
            if (address == null || address == 0)
                continue;

            var userFileEvent = (UserFileEvent*)address;

            var moduleName = methodInfo.Name[3..];
            var fileName = Marshal.PtrToStringUTF8((nint)userFileEvent->FileName) ?? string.Empty;
            var offset = address - (nint)uiModule;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(moduleName);

            if (_savedModules.TryGetValue(fileName, out var module))
            {
                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Green))
                    ImGui.Text(fileName);

                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(module.VfIndex == vfa.Index ? Colors.Green : Colors.Red)))
                    ImGui.Text(vfa.Index.ToString());

                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(module.FileType == userFileEvent->GetFileType() ? Colors.Green : Colors.Red)))
                    ImGui.Text($"0x{userFileEvent->GetFileType():X}");

                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(module.FileSize == userFileEvent->GetFileSize() ? Colors.Green : Colors.Red)))
                    ImGui.Text($"0x{userFileEvent->GetFileSize():X}");

                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(module.FileVersion == userFileEvent->GetFileVersion() ? Colors.Green : Colors.Red)))
                    ImGui.Text($"0x{userFileEvent->GetFileVersion():X}");

                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(module.DataSize == userFileEvent->GetDataSize() ? Colors.Green : Colors.Red)))
                    ImGui.Text($"0x{userFileEvent->GetDataSize():X}");

                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(module.Offset == offset ? Colors.Green : Colors.Red)))
                    ImGui.Text($"0x{offset:X}");
            }
            else
            {
                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                    ImGui.Text(fileName);
                ImGui.TableNextColumn();
                ImGui.Text(vfa.Index.ToString());
                ImGui.TableNextColumn();
                ImGui.Text($"0x{userFileEvent->GetFileType():X}");
                ImGui.TableNextColumn();
                ImGui.Text($"0x{userFileEvent->GetFileSize():X}");
                ImGui.TableNextColumn();
                ImGui.Text($"0x{userFileEvent->GetFileVersion():X}");
                ImGui.TableNextColumn();
                ImGui.Text($"0x{userFileEvent->GetDataSize():X}");
                ImGui.TableNextColumn();
                ImGui.Text($"0x{offset:X}");
            }

            ImGui.TableNextColumn();
            if (ImGui.Button($"Copy##CopyVf{vfa.Index}"))
            {
                var sw = new Stopwatch();
                sw.Start();
                while (!OpenClipboard(0))
                {
                    Thread.Sleep(100);
                    if (sw.ElapsedMilliseconds > 2000)
                    {
                        sw.Stop();
                        return;
                    }
                }
                var str = $"[\"{fileName}\"] = ({vfa.Index}, 0x{userFileEvent->GetFileType():X}, 0x{userFileEvent->GetFileSize():X}, 0x{userFileEvent->GetFileVersion():X}, 0x{userFileEvent->GetDataSize():X}, 0x{offset:X}),";
                var ptr = Marshal.StringToHGlobalAnsi(str);
                EmptyClipboard();
                SetClipboardData(1, ptr);
                CloseClipboard();
                Service.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification() { Title = "Text copied!" });
            }
        }
    }

    [DllImport("user32.dll")]
    internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    internal static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    internal static extern bool SetClipboardData(uint uFormat, IntPtr data);
}
*/
