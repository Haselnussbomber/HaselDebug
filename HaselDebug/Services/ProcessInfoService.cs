using System.Text;
using System.Threading;
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
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _swapLock = new();

    private HANDLE _processHandle;
    private Thread _workerThread;

    private ModuleInfo[] _modulesBufferA = new ModuleInfo[512];
    private ModuleInfo[] _modulesBufferB = new ModuleInfo[512];
    private SectionInfo[] _sectionsBufferA = new SectionInfo[4096];
    private SectionInfo[] _sectionsBufferB = new SectionInfo[4096];

    private int _activeModuleCount;
    private int _activeSectionCount;

    private volatile ModuleInfo[] _activeModules;
    private volatile SectionInfo[] _activeSections;

    public bool Enabled { get; set; }

    public ReadOnlySpan<ModuleInfo> Modules
    {
        get
        {
            using var scope = _swapLock.EnterScope();
            return new ReadOnlySpan<ModuleInfo>(_activeModules, 0, _activeModuleCount);
        }
    }

    public ReadOnlySpan<SectionInfo> Sections
    {
        get
        {
            using var scope = _swapLock.EnterScope();
            return new ReadOnlySpan<SectionInfo>(_activeSections, 0, _activeSectionCount);
        }
    }

    [AutoPostConstruct]
    private void Initialize()
    {
        _processHandle = PInvoke.GetCurrentProcess();

        _activeModules = _modulesBufferA;
        _activeSections = _sectionsBufferA;

        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "ProcessInfoServiceWorker",
            Priority = ThreadPriority.BelowNormal
        };

        _workerThread.Start();
    }

    public void Dispose()
    {
        _cts.Cancel();

        if (_workerThread.IsAlive)
            _workerThread.Join(500);
    }

    private void WorkerLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (Enabled)
            {
                var isAActive = ReferenceEquals(_activeModules, _modulesBufferA);

                var targetModules = isAActive ? _modulesBufferB : _modulesBufferA;
                var targetSections = isAActive ? _sectionsBufferB : _sectionsBufferA;

                var modCount = FillModules(ref targetModules);
                var secCount = FillSections(ref targetSections, targetModules, modCount);

                if (isAActive)
                {
                    _modulesBufferB = targetModules;
                    _sectionsBufferB = targetSections;
                }
                else
                {
                    _modulesBufferA = targetModules;
                    _sectionsBufferA = targetSections;
                }

                using (var lockScope = _swapLock.EnterScope())
                {
                    _activeModules = targetModules;
                    _activeSections = targetSections;
                    _activeModuleCount = modCount;
                    _activeSectionCount = secCount;
                }
            }

            Thread.Sleep(1000);
        }
    }

    public bool IsPointerValid(nint ptr) => IsPointerValid((void*)ptr);

    public bool IsPointerValid(void* ptr)
    {
        return ptr != null && GetSectionToPointer((nint)ptr) != default;
    }

    public string GetAddressName(nint address)
    {
        var module = GetModuleToPointer(address);

        if (module != default)
        {
            var moduleOffset = address - module.BaseAddress;
            var name = module.Name == "ffxiv_dx11.exe" ? "" : module.Name;

            return $"{name}+0x{moduleOffset:X}";
        }

        return $"0x{address:X}";
    }

    public ModuleInfo GetModuleToPointer(nint address)
    {
        var index = BinarySearchMemoryRegion(Modules, address);
        return index < 0 ? default : Modules[index];
    }

    public SectionInfo GetSectionToPointer(nint address)
    {
        var index = BinarySearchMemoryRegion(Sections, address);
        return index < 0 ? default : Sections[index];
    }

    private static int FillModules(ref ModuleInfo[] buffer)
    {
        var snapshot = PInvoke.CreateToolhelp32Snapshot(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPMODULE | CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPMODULE32, 0);
        if (snapshot.IsNull)
            return 0;

        var count = 0;

        try
        {
            MODULEENTRY32W moduleEntry = new()
            {
                dwSize = (uint)sizeof(MODULEENTRY32W)
            };

            if (PInvoke.Module32FirstW(snapshot, ref moduleEntry))
            {
                do
                {
                    if (count >= buffer.Length)
                        Array.Resize(ref buffer, buffer.Length * 2);

                    buffer[count++] = new ModuleInfo
                    {
                        BaseAddress = (nint)moduleEntry.modBaseAddr,
                        Size = moduleEntry.modBaseSize,
                        Path = moduleEntry.szExePath.ToString(),
                        Name = moduleEntry.szModule.ToString()
                    };
                } while (PInvoke.Module32NextW(snapshot, ref moduleEntry));
            }
        }
        finally
        {
            PInvoke.CloseHandle(snapshot);
        }

        Array.Sort(buffer, 0, count, ModuleComparer.Instance);
        return count;
    }

    private static int FillSections(ref SectionInfo[] buffer, ModuleInfo[] modules, int modCount)
    {
        var count = 0;
        nuint address = 0;

        while (PInvoke.VirtualQuery((void*)address, out var memory) != 0)
        {
            if (memory.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT)
            {
                if (count >= buffer.Length)
                    Array.Resize(ref buffer, buffer.Length * 2);

                buffer[count++] = new SectionInfo
                {
                    Start = (nint)memory.BaseAddress,
                    End = (nint)memory.BaseAddress + (nint)memory.RegionSize,
                    Size = (nint)memory.RegionSize,
                    Category = memory.Type == PAGE_TYPE.MEM_PRIVATE ? SectionCategory.HEAP : SectionCategory.Unknown
                };
            }

            var next = (nuint)memory.BaseAddress + memory.RegionSize;
            if (next <= address)
                break;

            address = next;
        }

        var activeSections = new Span<SectionInfo>(buffer, 0, count);
        for (var i = 0; i < modCount; i++)
            ParseModuleSections(modules[i], activeSections);

        return count;
    }

    private static void ParseModuleSections(ModuleInfo module, Span<SectionInfo> sections)
    {
        if (module.BaseAddress == 0)
            return;

        var dos = (IMAGE_DOS_HEADER*)module.BaseAddress;
        if (dos->e_magic != 0x5A4D)
            return;

        var nt = (IMAGE_NT_HEADERS64*)(module.BaseAddress + dos->e_lfanew);
        if (nt->Signature != 0x00004550)
            return;

        var sectionHeader = (IMAGE_SECTION_HEADER*)((byte*)nt + sizeof(IMAGE_NT_HEADERS64));
        var count = nt->FileHeader.NumberOfSections;

        for (var i = 0; i < count; i++)
        {
            var header = sectionHeader + i;
            var sectionAddr = module.BaseAddress + (nint)header->VirtualAddress;

            var idx = BinarySearchMemoryRegion(sections, sectionAddr);
            if (idx >= 0)
            {
                ref var section = ref sections[idx];
                section.Category = header->Characteristics.HasFlag(IMAGE_SECTION_CHARACTERISTICS.IMAGE_SCN_CNT_CODE)
                    ? SectionCategory.CODE 
                    : SectionCategory.DATA;
                section.Name = Encoding.UTF8.GetString(header->Name.AsSpan()).TrimEnd('\0');
                section.ModuleName = module.Name;
            }
        }
    }

    private static int BinarySearchMemoryRegion<T>(ReadOnlySpan<T> regions, nint address) where T : IMemoryRegion
    {
        var low = 0;
        var high = regions.Length - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            ref readonly var midRegion = ref regions[mid];

            if (address >= midRegion.Start && address < midRegion.End)
                return mid;

            if (address < midRegion.Start)
                high = mid - 1;
            else
                low = mid + 1;
        }

        return -1;
    }

    private class ModuleComparer : IComparer<ModuleInfo>
    {
        public static readonly ModuleComparer Instance = new();
        public int Compare(ModuleInfo x, ModuleInfo y) => x.BaseAddress.CompareTo(y.BaseAddress);
    }
}

public enum SectionCategory
{
    Unknown,
    CODE,
    DATA,
    HEAP
}

public interface IMemoryRegion
{
    nint Start { get; }
    nint End { get; }
}

public record struct SectionInfo : IMemoryRegion
{
    public required nint Start { get; set; }
    public required nint End { get; set; }
    public required nint Size { get; set; }
    public required SectionCategory Category { get; set; }
    public string? Name { get; set; }
    public string? ModuleName { get; set; }
    public string? ModulePath { get; set; }
}

public readonly record struct ModuleInfo : IMemoryRegion
{
    public required nint BaseAddress { get; init; }
    public required uint Size { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }

    nint IMemoryRegion.Start => BaseAddress;
    nint IMemoryRegion.End => BaseAddress + (nint)Size;
}
