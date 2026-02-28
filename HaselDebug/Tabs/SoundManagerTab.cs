using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Sound;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SoundManagerTab : DebugTab, IDisposable
{
    private readonly ILogger<SoundManagerTab> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly DebugRenderer _debugRenderer;

    private bool _initialized;
    private bool _logEnabled;

    private Hook<SoundManager.Delegates.PlaySound>? _hook1;
    private Hook<SoundManager.Delegates.PlayClipSound>? _hook2;
    private Hook<SoundManager.Delegates.PlayLayoutSound>? _hook3;
    private Hook<SoundManager.Delegates.PlayBGMSound>? _hook4;
    private Hook<SoundManager.Delegates.PlayGAYATitleSound>? _hook5;
    private Hook<SoundManager.Delegates.PlayOrchestrionSound>? _hook6;
    private Hook<SoundManager.Delegates.PlaySystemSound>? _hook7;
    private Hook<SoundManager.Delegates.PlayMovieSound>? _hook8;
    private Hook<SoundManager.Delegates.PlayCutsceneVoSound>? _hook9;

    private string _path = "music/ffxiv/BGM_System_Chara.scd";
    private float _volume = 1;
    private uint _fadeInDuration;
    private Vector3 _position = Vector3.Zero;
    private float _speed = 1;
    private int _a9;
    private uint _soundNumber = 0;
    private bool _autoRelease = true;
    private SoundVolumeCategory _volumeCategory = SoundVolumeCategory.BypassVolumeRules;
    private bool _a13;
    private int _midiNote = -1;
    private bool _a15;
    private bool _defaultFadeOut;
    private bool _isPositional;
    private bool _a18;

    private void Initialize()
    {
        _hook1 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlaySound>(SoundManager.MemberFunctionPointers.PlaySound, PlaySoundDetour);
        _hook2 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayClipSound>(SoundManager.MemberFunctionPointers.PlayClipSound, PlayClipSoundDetour);
        _hook3 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayLayoutSound>(SoundManager.MemberFunctionPointers.PlayLayoutSound, PlayLayoutSoundDetour);
        _hook4 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayBGMSound>(SoundManager.MemberFunctionPointers.PlayBGMSound, PlayBGMSoundDetour);
        _hook5 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayGAYATitleSound>(SoundManager.MemberFunctionPointers.PlayGAYATitleSound, PlayGAYATitleSoundDetour);
        _hook6 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayOrchestrionSound>(SoundManager.MemberFunctionPointers.PlayOrchestrionSound, PlayOrchestrionSoundDetour);
        _hook7 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlaySystemSound>(SoundManager.MemberFunctionPointers.PlaySystemSound, PlaySystemSoundDetour);
        _hook8 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayMovieSound>(SoundManager.MemberFunctionPointers.PlayMovieSound, PlayMovieSoundDetour);
        _hook9 = _gameInteropProvider.HookFromAddress<SoundManager.Delegates.PlayCutsceneVoSound>(SoundManager.MemberFunctionPointers.PlayCutsceneVoSound, PlayCutsceneVoSoundDetour);
    }

    public void Dispose()
    {
        _hook1?.Dispose();
        _hook2?.Dispose();
        _hook3?.Dispose();
        _hook4?.Dispose();
        _hook5?.Dispose();
        _hook6?.Dispose();
        _hook7?.Dispose();
        _hook8?.Dispose();
        _hook9?.Dispose();
    }

    private SoundData* PlaySoundDetour(SoundManager* thisPtr, CStringPointer path, float volume, uint fadeInDuration, float posX, float posY, float posZ, float speed, int a9, uint soundNumber, bool autoRelease, SoundVolumeCategory volumeCategory, bool a13, int note, bool a15, bool a16, bool a17, bool a18)
    {
        if (!path.ToString().StartsWith("sound/battle/") && !path.ToString().StartsWith("sound/foot/"))
        {
            _logger.LogTrace(
                "PlaySound(path: {path}, volume: {volume}, fadeInDuration: {fadeInDuration}, x: {x}, y: {y}, z: {z}, speed: {speed}, a9: {a9}, soundNumber: {soundNumber}, autoRelease: {autoRelease}, volumeCategory: {volumeCategory}, a13: {a13}, note: {note}, a15: {a15}, a16: {a16}, a17: {a17}, a18: {a18})",
                path, volume, fadeInDuration, posX, posY, posZ, speed, a9, soundNumber, autoRelease, volumeCategory, a13, note, a15, a16, a17, a18);
        }

        return _hook1!.OriginalDisposeSafe(thisPtr, path, volume, fadeInDuration, posX, posY, posZ, speed, a9, soundNumber, autoRelease, volumeCategory, a13, note, a15, a16, a17, a18);
    }

    private SoundData* PlayClipSoundDetour(SoundManager* thisPtr, CStringPointer path, float volume, uint fadeInDuration, float posX, float posY, float posZ, float speed, int priority, uint soundNumber, bool autoRelease, bool a12)
    {
        _logger.LogTrace(
            "PlayClipSound(path: {path}, volume: {volume}, fadeInDuration: {fadeInDuration}, x: {x}, y: {y}, z: {z}, speed: {speed}, priority: {priority}, soundNumber: {soundNumber}, autoRelease: {autoRelease}, a12: {a12})",
            path, volume, fadeInDuration, posX, posY, posZ, speed, priority, soundNumber, autoRelease, a12);

        return _hook2!.OriginalDisposeSafe(thisPtr, path, volume, fadeInDuration, posX, posY, posZ, speed, priority, soundNumber, autoRelease, a12);
    }

    private SoundData* PlayLayoutSoundDetour(SoundManager* thisPtr, SoundResourceHandle* resourceHandle, SoundLayoutOptions* options, ushort size)
    {
        var filename = "N/A";

        if (resourceHandle != null)
        {
            filename = resourceHandle->Id.ToString();

            if (resourceHandle->FileName.BufferPtr != null)
                filename = resourceHandle->FileName.ToString();
        }

        var type = options != null ? options->Type : 0;

        _logger.LogTrace("PlayLayoutSound(FileName: {fileName}, type: {type}, size: 0x{size:X})", filename, type, size);
        return _hook3!.OriginalDisposeSafe(thisPtr, resourceHandle, options, size);
    }

    private SoundData* PlayBGMSoundDetour(SoundManager* thisPtr, CStringPointer path)
    {
        _logger.LogTrace("PlayBGMSound(path: {path})", path);
        return _hook4!.OriginalDisposeSafe(thisPtr, path);
    }

    private SoundData* PlayGAYATitleSoundDetour(SoundManager* thisPtr, CStringPointer path, float volume, uint fadeInDuration)
    {
        _logger.LogTrace("PlayGAYATitleSound(path: {path}, volume: {volume}, fadeInDuration: {fadeInDuration})", path, volume, fadeInDuration);
        return _hook5!.OriginalDisposeSafe(thisPtr, path, volume, fadeInDuration);
    }

    private SoundData* PlayOrchestrionSoundDetour(SoundManager* thisPtr, CStringPointer path, float posX, float posY, float posZ, bool a6)
    {
        _logger.LogTrace("PlayOrchestrionSound(path: {path}, x: {x}, y: {y}, z: {z}, a6: {a6})", path, posX, posY, posZ, a6);
        return _hook6!.OriginalDisposeSafe(thisPtr, path, posX, posY, posZ, a6);
    }

    private SoundData* PlaySystemSoundDetour(SoundManager* thisPtr, CStringPointer path, float volume, uint soundNumber, uint fadeInDuration, bool autoRelease, SoundVolumeCategory volumeCategory)
    {
        _logger.LogTrace("PlaySystemSound(path: {path}, volume: {volume}, soundNumber: {soundNumber}, fadeInDuration: {fadeInDuration}, autoRelease: {autoRelease}, volumeCategory: {volumeCategory})", path, volume, soundNumber, fadeInDuration, autoRelease, volumeCategory);
        return _hook7!.OriginalDisposeSafe(thisPtr, path, volume, soundNumber, fadeInDuration, autoRelease, volumeCategory);
    }

    private SoundData* PlayMovieSoundDetour(SoundManager* thisPtr, CStringPointer path, float volume, uint soundNumber, uint fadeInDuration, bool autoRelease)
    {
        _logger.LogTrace("PlayMovieSound(path: {path}, volume: {volume}, soundNumber: {soundNumber}, fadeInDuration: {fadeInDuration}, autoRelease: {autoRelease})", path, volume, soundNumber, fadeInDuration, autoRelease);
        return _hook8!.OriginalDisposeSafe(thisPtr, path, volume, soundNumber, fadeInDuration, autoRelease);
    }

    private SoundData* PlayCutsceneVoSoundDetour(SoundManager* thisPtr, CStringPointer path)
    {
        _logger.LogTrace("PlayCutsceneVoSound(path: {path})", path);
        return _hook9!.OriginalDisposeSafe(thisPtr, path);
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            _initialized = true;
            Initialize();
        }

        if (ImGui.Checkbox("Log to Console (Verbose)", ref _logEnabled))
        {
            if (_logEnabled)
            {
                _hook1?.Enable();
                _hook2?.Enable();
                _hook3?.Enable();
                _hook4?.Enable();
                _hook5?.Enable();
                _hook6?.Enable();
                _hook7?.Enable();
                _hook8?.Enable();
                _hook9?.Enable();
            }
            else
            {
                _hook1?.Disable();
                _hook2?.Disable();
                _hook3?.Disable();
                _hook4?.Disable();
                _hook5?.Disable();
                _hook6?.Disable();
                _hook7?.Disable();
                _hook8?.Disable();
                _hook9?.Disable();
            }
        }

        var manager = SoundManager.Instance();
        if (manager == null)
        {
            ImGui.Text("SoundManager unavailable"u8);
            return;
        }

        _debugRenderer.DrawPointerType(manager, typeof(SoundManager), new NodeOptions());

        using (var node = ImRaii.TreeNode("Play Sound", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                ImGui.InputText("Path"u8, ref _path);
                ImGui.DragFloat("Volume"u8, ref _volume, 0.01f, 0f, 1f);
                ImGui.InputUInt("FadeInDuration"u8, ref _fadeInDuration);
                ImGui.DragFloat3("Position"u8, ref _position);
                ImGui.DragFloat("Speed"u8, ref _speed, 0.01f, 0, 2);
                ImGui.InputInt("a9"u8, ref _a9);
                ImGui.InputUInt("Track Index"u8, ref _soundNumber, 1);
                ImGui.Checkbox("AutoRelease"u8, ref _autoRelease);
                ImGuiUtilsEx.EnumCombo("VolumeCategory", ref _volumeCategory);
                ImGui.Checkbox("a13"u8, ref _a13);
                ImGui.InputInt("MidiNote"u8, ref _midiNote);
                ImGui.Checkbox("a15"u8, ref _a15);
                ImGui.Checkbox("DefaultFadeOut"u8, ref _defaultFadeOut); // forces it to be 10 seconds
                ImGui.Checkbox("IsPositional"u8, ref _isPositional);
                ImGui.Checkbox("a18"u8, ref _a18);

                if (ImGui.Button("Play"))
                {
                    var sd = SoundManager.Instance()->PlaySound(
                        path: _path,
                        volume: _volume,
                        fadeInDuration: _fadeInDuration,
                        posX: _position.X,
                        posY: _position.Y,
                        posZ: _position.Z,
                        speed: _speed,
                        a9: _a9,
                        soundNumber: _soundNumber,
                        autoRelease: _autoRelease,
                        volumeCategory: _volumeCategory,
                        a13: _a13,
                        midiNote: _midiNote,
                        a15: _a15,
                        defaultFadeOut: _defaultFadeOut,
                        isPositional: _isPositional,
                        a18: _a18);

                    // sd->SetFadeInDuration(2000);
                    // sd->SetFadeInEnabled(true);
                }
            }
        }

        using var table = ImRaii.Table("SoundDataTable"u8, 6, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Active"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("FileName"u8, ImGuiTableColumnFlags.WidthFixed, 400);
        ImGui.TableSetupColumn("ElapsedTime"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Actions"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Struct"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var pool = manager->SoundDataPool;
        for (var i = 0; i < pool->Entries.Length; i++)
        {
            ref var entry = ref pool->Entries[i];
            using var id = ImRaii.PushId($"SoundData{i}");

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Active
            ImGui.Text(entry.IsActive.ToString());

            ImGui.TableNextColumn(); // FileName
            ImGuiUtils.DrawCopyableText(entry.GetFileName());

            ImGui.TableNextColumn(); // ElapsedTime
            ImGuiUtils.DrawCopyableText($"{entry.GetElapsedTime():0.00}");

            ImGui.TableNextColumn(); // Actions

            if (ImGui.Button("Stop"))
                entry.Stop();

            ImGui.SameLine();

            var volume = entry.GetVolume();
            if (ImGui.SliderFloat("Volume", ref volume, 0, 1))
                entry.SetVolume(volume, 0);

            ImGui.TableNextColumn(); // Struct
            _debugRenderer.DrawPointerType(Unsafe.AsPointer(ref entry), typeof(SoundData), new NodeOptions());
        }
    }
}
