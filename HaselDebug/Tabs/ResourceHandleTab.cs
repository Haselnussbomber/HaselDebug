using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ResourceHandleTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;

    private string _path = string.Empty;
    private ResourceHandle* _handle;

    public override void Draw()
    {
        if (ImGui.InputTextWithHint("##ResourceHandlePath"u8, "Path..."u8, ref _path, flags: ImGuiInputTextFlags.EnterReturnsTrue))
        {
            LoadAsync();
        }

        ImGui.SameLine();

        if (_handle == null)
        {
            if (ImGui.Button("LoadAsync"u8))
            {
                LoadAsync();
            }
            ImGui.SameLine();
            if (ImGui.Button("LoadSync"u8))
            {
                LoadSync();
            }
        }
        else
        {
            if (ImGui.Button("Unload"u8))
            {
                Unload();
            }

            _debugRenderer.DrawPointerType(_handle);
        }
    }

    private void Unload()
    {
        if (_handle != null)
        {
            _handle->DecRef();
            _handle = null;
        }
    }

    private void LoadAsync()
    {
        Unload();
        _handle = ResourceHandle.GetAsync(_path);
    }

    private void LoadSync()
    {
        Unload();
        _handle = ResourceHandle.GetSync(_path);
    }

    public void Dispose()
    {
        Unload();
    }
}
