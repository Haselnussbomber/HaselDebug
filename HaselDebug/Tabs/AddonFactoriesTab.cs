using System.Text;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using Iced.Intel;
using Decoder = Iced.Intel.Decoder;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AddonFactoriesTab : DebugTab
{
    private const int LoadUldResourceHandleVfIndex = 45;

    private readonly FastFormatter _formatter = new();
    private readonly FastStringOutput _output = new();
    private readonly List<AddonEntry> _cache = [];
    private readonly ISigScanner _sigScanner;
    private readonly DebugRenderer _debugRenderer;
    private bool _isInitialized;
    private nint _memoryManager_Alloc;
    private nint _atkUnitBaseVtableAddress;

    private record AddonEntry(string Name, nint CtorAddress, nint VTableAddress, nint InheritanceVtableAddress);

    [GeneratedRegex("^_?(.*?)(?:Addon)?$")]
    private static partial Regex AddonNameRegex();

    private void Initialize()
    {
        _memoryManager_Alloc = _sigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B F0 BD") - _sigScanner.Module.BaseAddress;
        _atkUnitBaseVtableAddress = GetVTableAddress(_sigScanner.ScanText("E8 ?? ?? ?? ?? 33 D2 48 8D 9F"));

        var addonNames = RaptureAtkModule.Instance()->AddonNames;
        var addonAllocators = RaptureAtkModule.Instance()->AddonFactories;
        for (var i = 0; i < addonNames.Count; i++)
        {
            var createAddress = (nint)addonAllocators[i].Create;
            var ctorAddress = GetCtorAddress(createAddress);
            var vtableAddress = GetVTableAddress(ctorAddress);
            var inheritanceVtableAddress = GetInheritanceVTable(ctorAddress);

            _cache.Add(new AddonEntry(
                AddonNameRegex().Match(addonNames[i].ToString()).Groups[1].Value,
                ctorAddress,
                vtableAddress,
                inheritanceVtableAddress
            ));
        }
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        if (ImGui.Button("Copy for data.yml"))
        {
            var sb = new StringBuilder();
            var usedVTables = new HashSet<nint>();

            for (var i = 0; i < _cache.Count; i++)
            {
                var entry = _cache[i];

                sb.AppendLine($"  Client::UI::Addon{entry.Name}: # {i}");

                if (entry.CtorAddress == 0 || usedVTables.Contains(entry.VTableAddress))
                    continue;

                if (entry.VTableAddress == 0)
                {
                    sb.AppendLine("    funcs:");
                    sb.AppendLine($"      0x{entry.CtorAddress - _sigScanner.Module.BaseAddress + 0x140000000:X}: ctor");
                    continue;
                }

                usedVTables.Add(entry.VTableAddress);

                sb.AppendLine("    vtbls:");
                sb.AppendLine($"      - ea: 0x{entry.VTableAddress - _sigScanner.Module.BaseAddress + 0x140000000:X}");
                if (entry.InheritanceVtableAddress == _atkUnitBaseVtableAddress)
                {
                    sb.AppendLine("        base: Component::GUI::AtkUnitBase");
                }
                else
                {
                    var inheritanceEntry = entry.InheritanceVtableAddress != 0 ? _cache.FirstOrDefault(e => e.VTableAddress == entry.InheritanceVtableAddress) : null;
                    if (inheritanceEntry != null)
                    {
                        sb.AppendLine($"        base: Client::UI::Addon{inheritanceEntry.Name}");
                    }
                    else
                    {
                        sb.AppendLine($"        base: 0x{entry.InheritanceVtableAddress - _sigScanner.Module.BaseAddress + 0x140000000:X}");
                    }
                }
                sb.AppendLine("    funcs:");
                sb.AppendLine($"      0x{entry.CtorAddress - _sigScanner.Module.BaseAddress + 0x140000000:X}: ctor");
            }

            ImGui.SetClipboardText(sb.ToString());
        }

        ImGui.SameLine();

        ImGui.Text("Don't trust anything you see here."u8);

        using var table = ImRaii.Table("AddonsTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Ctor"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("VTable"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Inheritance"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < _cache.Count; i++)
        {
            var entry = _cache[i];

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Create
            _debugRenderer.DrawAddress(entry.CtorAddress);

            ImGui.TableNextColumn(); // VTable
            _debugRenderer.DrawAddress(entry.VTableAddress);

            var count = _cache.Count(e => e.VTableAddress == entry.VTableAddress);
            if (count > 1)
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, (uint)entry.VTableAddress | 0xFF000000);
                ImGui.SameLine();
                ImGui.Text($"({count})");
            }

            ImGui.TableNextColumn(); // Inheritance
            if (entry.InheritanceVtableAddress == _atkUnitBaseVtableAddress)
            {
                ImGuiUtils.DrawCopyableText("AtkUnitBase");
            }
            else
            {
                var inheritanceEntry = entry.InheritanceVtableAddress != 0 ? _cache.FirstOrDefault(e => e.VTableAddress == entry.InheritanceVtableAddress) : null;
                if (inheritanceEntry != null)
                {
                    ImGuiUtils.DrawCopyableText(inheritanceEntry.Name);
                }
                else
                {
                    _debugRenderer.DrawAddress(entry.InheritanceVtableAddress);
                }
            }

            ImGui.TableNextColumn(); // Name
            using var node = ImRaii.TreeNode(entry.Name, ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!node) continue;
            DrawDecoded(entry.CtorAddress);
        }
    }

    private void DrawDecoded(nint addr)
    {
        if (addr == 0) return;
        using var table = ImRaii.Table(addr.ToString("X") + "DecodedTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Address"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Assembly"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("FlowControl"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Code"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Op0"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Op1"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var codeReader = new NativeCodeReader(addr);
        var decoder = Decoder.Create(64, codeReader);
        decoder.IP = (ulong)(addr - _sigScanner.Module.BaseAddress);

        var instructions = new List<Instruction>();

        while (codeReader.CanReadByte)
        {
            decoder.Decode(out var instr);

            if (instr.IsInvalid)
                break;

            instructions.Add(instr);

            if (instr.FlowControl == FlowControl.Return)
                break;
        }

        for (var i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            _output.Clear();
            _formatter.Format(instr, _output);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Idx
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Address
            _debugRenderer.DrawAddress((nint)instr.IP + _sigScanner.Module.BaseAddress);

            ImGui.TableNextColumn(); // Assembly
            ImGui.Text(_output.ToString());

            ImGui.TableNextColumn(); // FlowControl
            ImGui.Text(instr.FlowControl.ToString());

            ImGui.TableNextColumn(); // Code
            ImGui.Text(instr.Code.ToString());

            for (var j = 0; j < 2; j++)
            {
                ImGui.TableNextColumn(); // OpX

                var kind = instr.GetOpKind(j);
                var register = instr.GetOpRegister(j);

                if (kind == OpKind.Register && register == Register.None)
                    continue;

                ImGui.Text(instr.GetOpKind(j).ToString());

                if (register != Register.None)
                {
                    ImGui.SameLine();
                    ImGui.Text($"({register})");
                }

                if (kind == OpKind.Memory)
                {
                    ImGui.SameLine();
                    ImGui.Text($"({instr.MemoryBase})");
                }
            }
        }
    }

    private nint GetCtorAddress(nint createAddress)
    {
        if (createAddress == 0)
            return 0;

        createAddress = ResolveJump(createAddress);

        var codeReader = new NativeCodeReader(createAddress);
        var decoder = Decoder.Create(64, codeReader);
        decoder.IP = (ulong)(createAddress - _sigScanner.Module.BaseAddress);

        var foundAlloc = false;

        while (codeReader.CanReadByte)
        {
            decoder.Decode(out var instr);

            if (instr.IsInvalid || instr.FlowControl == FlowControl.Return)
                break;

            if (instr.FlowControl != FlowControl.Call || instr.Op0Kind != OpKind.NearBranch64)
                continue;

            if (!foundAlloc)
            {
                if ((nint)instr.NearBranch64 == _memoryManager_Alloc)
                    foundAlloc = true;

                continue;
            }

            return (nint)instr.NearBranch64 + _sigScanner.Module.BaseAddress;
        }

        return 0;
    }

    private nint GetInheritanceVTable(nint ctorAddress)
    {
        if (ctorAddress == 0)
            return 0;

        ctorAddress = ResolveJump(ctorAddress);

        var codeReader = new NativeCodeReader(ctorAddress);
        var decoder = Decoder.Create(64, codeReader);
        decoder.IP = (ulong)(ctorAddress - _sigScanner.Module.BaseAddress);

        while (codeReader.CanReadByte)
        {
            decoder.Decode(out var instr);

            if (instr.IsInvalid || instr.FlowControl == FlowControl.Return)
                break;

            if (instr.FlowControl != FlowControl.Call)
                continue;

            return GetVTableAddress((nint)instr.NearBranch64 + _sigScanner.Module.BaseAddress);
        }

        return 0;
    }

    private nint GetVTableAddress(nint ctorAddress)
    {
        if (ctorAddress == 0)
            return 0;

        ctorAddress = ResolveJump(ctorAddress);

        var codeReader = new NativeCodeReader(ctorAddress);
        var decoder = Decoder.Create(64, codeReader);
        decoder.IP = (ulong)(ctorAddress - _sigScanner.Module.BaseAddress);

        var vtblAddress = nint.Zero;
        var rax = nint.Zero;

        while (codeReader.CanReadByte)
        {
            decoder.Decode(out var instr);

            if (instr.IsInvalid || instr.FlowControl == FlowControl.Return)
                break;

            // lea rax,[offset]
            if (instr.Code == Code.Lea_r64_m &&
                instr.Op0Kind == OpKind.Register &&
                instr.Op0Register == Register.RAX &&
                instr.Op1Kind == OpKind.Memory &&
                instr.MemoryBase == Register.RIP)
            {
                rax = (nint)instr.MemoryDisplacement64 + _sigScanner.Module.BaseAddress;
                continue;
            }

            // mov [rcx],rax
            if (instr.Code == Code.Mov_rm64_r64 &&
                instr.Op0Kind == OpKind.Memory &&
                instr.MemoryDisplacement64 == 0 &&
                instr.Op1Kind == OpKind.Register &&
                instr.Op1Register == Register.RAX)
            {
                vtblAddress = rax;
                break;
            }
        }

        return vtblAddress;
    }

    private nint ResolveJump(nint addr)
    {
        if (*(byte*)addr == 0xE9)
        {
            var relJmp = *(uint*)(addr + 1);
            return addr + 5 + (nint)relJmp;
        }

        return addr;
    }
}
