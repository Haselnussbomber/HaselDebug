using System.IO;
using System.Text;
using System.Timers;
using HaselDebug.Config;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Memory;
using Windows.Win32.System.SystemServices;

namespace HaselDebug.Service;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ProcessInfoService : IDisposable
{
    private readonly PluginConfig _pluginConfig;
    private Timer? _timer;

    [AutoPostConstruct]
    private void Initialize()
    {
        Refresh();
        _timer = new();
        _timer.Elapsed += (s, e) => Refresh();
        _timer.Interval = 1000; // every second
        _timer.Start();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public ModuleInfo[] Modules { get; private set; } = [];
    public SectionInfo[] Sections { get; private set; } = [];

    private void Refresh()
    {
        var processHandle = PInvoke.GetCurrentProcess();
        Modules = GetModules(processHandle);
        Sections = GetSections(processHandle, Modules);
    }

    public bool IsPointerValid(nint ptr) => IsPointerValid((void*)ptr);

    public bool IsPointerValid(void* ptr)
    {
        return ptr != null && GetSectionToPointer((nint)ptr) != default;
    }

    public string GetAddressName(nint address)
    {
        // if (NamedAddresses.TryGetValue(address, out var namedAddress))
        // {
        //     return namedAddress;
        // }

        var section = GetSectionToPointer(address);
        if (section != default)
        {
            switch (section.Category)
            {
                case SectionCategory.CODE:
                case SectionCategory.DATA:
                    return $"{(section.ModuleName == "ffxiv_dx11.exe" ? "" : section.ModuleName)}+0x{address - section.Start:X}"; // <{section.Category}>

                case SectionCategory.HEAP:
                    return address.ToString("X");
            }
        }

        var module = GetModuleToPointer(address);
        if (module != default)
        {
            var offset = address - module.BaseAddress;
            return $"{(module.Name == "ffxiv_dx11.exe" ? "" : module.Name)}+0x{offset:X}";
        }

        return address.ToString("X");
    }

    public ModuleInfo GetModuleToPointer(nint address)
    {
        var modules = Modules;
        var index = FindModuleIndex(modules, address);
        return index < 0 ? default : modules[index];

        static int FindModuleIndex(ModuleInfo[] modules, nint address)
        {
            var min = 0;
            var max = modules.Length - 1;
            while (min <= max)
            {
                var mid = (min + max) / 2;
                var module = modules[mid];

                if (address >= module.BaseAddress && address < module.BaseAddress + module.Size)
                    return mid;

                if (address < module.BaseAddress)
                    max = mid - 1;
                else
                    min = mid + 1;
            }
            return -1;
        }
    }

    public SectionInfo GetSectionToPointer(nint address)
    {
        var sections = Sections;
        var index = FindSectionIndex(sections, address);
        return index < 0 ? default : sections[index];

        static int FindSectionIndex(SectionInfo[] sections, nint address)
        {
            var min = 0;
            var max = sections.Length - 1;
            while (min <= max)
            {
                var mid = (min + max) / 2;
                var section = sections[mid];

                if (address >= section.Start && address < section.End)
                    return mid;

                if (address < section.Start)
                    max = mid - 1;
                else
                    min = mid + 1;
            }
            return -1;
        }
    }

    private ModuleInfo[] GetModules(HANDLE processHandle)
    {
        var handle = PInvoke.CreateToolhelp32Snapshot(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPMODULE, PInvoke.GetProcessId(processHandle));
        if (handle == HANDLE.Null)
        {
            return [];
        }

        var list = new List<ModuleInfo>();
        var me32 = new MODULEENTRY32W
        {
            dwSize = (uint)sizeof(MODULEENTRY32W)
        };

        if (PInvoke.Module32FirstW(handle, ref me32))
        {
            do
            {
                list.Add(new()
                {
                    BaseAddress = (nint)me32.modBaseAddr,
                    Size = me32.modBaseSize,
                    Path = me32.szExePath.ToString(),
                    Name = Path.GetFileName(me32.szExePath.ToString())
                });
            } while (PInvoke.Module32NextW(handle, ref me32));
        }

        PInvoke.CloseHandle(handle);

        return [.. list.OrderBy(m => m.BaseAddress)];
    }

    private SectionInfo[] GetSections(HANDLE processHandle, ModuleInfo[] modules)
    {
        var sections = new List<SectionInfo>();

        nuint address = 0;
        while (PInvoke.VirtualQuery((void*)address, out var memory) != 0 && address + memory.RegionSize > address)
        {
            if (memory.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT)
            {
                sections.Add(new()
                {
                    Start = (nint)memory.BaseAddress,
                    End = (nint)memory.BaseAddress + (nint)memory.RegionSize,
                    Size = (nint)memory.RegionSize,
                    Category = memory.Type == PAGE_TYPE.MEM_PRIVATE ? SectionCategory.HEAP : SectionCategory.Unknown
                });
            }

            address = (nuint)memory.BaseAddress + memory.RegionSize;
        }

        foreach (var module in modules)
        {
            var imageDosHeader = new IMAGE_DOS_HEADER();
            if (!PInvoke.ReadProcessMemory(processHandle, (void*)module.BaseAddress, &imageDosHeader, (nuint)sizeof(IMAGE_DOS_HEADER)))
                continue;

            var ntHeaderPtr = (void*)(module.BaseAddress + imageDosHeader.e_lfanew);

            var imageNtHeaders = new IMAGE_NT_HEADERS64();
            if (!PInvoke.ReadProcessMemory(processHandle, ntHeaderPtr, &imageNtHeaders, (nuint)sizeof(IMAGE_NT_HEADERS64)))
                continue;

            var sectionHeaders = new IMAGE_SECTION_HEADER[imageNtHeaders.FileHeader.NumberOfSections];

            fixed (void* sectionHeadersPtr = sectionHeaders)
                PInvoke.ReadProcessMemory(processHandle, (void*)((nuint)ntHeaderPtr + (nuint)sizeof(IMAGE_NT_HEADERS64)), sectionHeadersPtr, imageNtHeaders.FileHeader.NumberOfSections * (nuint)sizeof(IMAGE_SECTION_HEADER));

            foreach (var sectionHeader in sectionHeaders)
            {
                var sectionAddress = module.BaseAddress + sectionHeader.VirtualAddress;

                var idx = FindSectionIndexInList(sections, (nint)sectionAddress);
                if (idx == -1)
                    continue;

                ref var section = ref CollectionsMarshal.AsSpan(sections)[idx];

                if (sectionAddress < section.Start)
                    continue;

                if (sectionAddress >= section.Start + section.Size)
                    continue;

                if (sectionHeader.VirtualAddress + sectionHeader.Misc.VirtualSize > module.Size)
                    continue;

                if (sectionHeader.Characteristics.HasFlag(IMAGE_SECTION_CHARACTERISTICS.IMAGE_SCN_CNT_CODE))
                {
                    section.Category = SectionCategory.CODE;
                }
                else if (sectionHeader.Characteristics.HasFlag(IMAGE_SECTION_CHARACTERISTICS.IMAGE_SCN_CNT_INITIALIZED_DATA)
                    || sectionHeader.Characteristics.HasFlag(IMAGE_SECTION_CHARACTERISTICS.IMAGE_SCN_CNT_UNINITIALIZED_DATA))
                {
                    section.Category = SectionCategory.DATA;
                }

                section.Name = Encoding.UTF8.GetString(sectionHeader.Name.AsSpan()).TrimEnd('\0');
                section.ModulePath = module.Path;
                section.ModuleName = Path.GetFileName(module.Path);
            }
        }

        return [.. sections.OrderBy(section => section.Start)];

        static int FindSectionIndexInList(List<SectionInfo> sections, nint address)
        {
            var min = 0;
            var max = sections.Count - 1;
            while (min <= max)
            {
                var mid = (min + max) / 2;
                var section = sections[mid];

                if (address >= section.Start && address < section.End)
                    return mid;

                if (address < section.Start)
                    max = mid - 1;
                else
                    min = mid + 1;
            }
            return -1;
        }
    }
}

public enum SectionCategory
{
    Unknown,
    CODE,
    DATA,
    HEAP
}

public record struct SectionInfo
{
    public required nint Start { get; set; }
    public required nint End { get; set; }
    public required nint Size { get; set; }
    public required SectionCategory Category { get; set; }
    public string? Name { get; set; }
    public string? ModuleName { get; set; }
    public string? ModulePath { get; set; }
}

public readonly record struct ModuleInfo
{
    public required nint BaseAddress { get; init; }
    public required uint Size { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }
}
