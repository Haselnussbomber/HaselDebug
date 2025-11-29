using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Common.Lua;
using HaselDebug.Config;

namespace HaselDebug.Services;

[RegisterSingleton, RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LuaLogger : IHostedService, IDisposable
{
    private const int LUA_GLOBALSINDEX = -10002; // _G

    private readonly ILogger<LuaLogger> _logger;
    private readonly PluginConfig _pluginConfig;
    private readonly IGameInteropProvider _gameInteropProvider;

    private Hook<LuaFuncDelegate>? _luaPanicHook;
    private Hook<LuaFuncDelegate>? _luaPrintHook;

    private delegate ulong LuaFuncDelegate(lua_State* thisPtr);

    [AutoPostConstruct]
    private void Initialize()
    {
        _luaPanicHook = _gameInteropProvider.HookFromSignature<LuaFuncDelegate>("48 83 EC ?? 45 33 C0 41 8D 50 ?? E8 ?? ?? ?? ?? 33 C0", LuaPanicDetour);
        _luaPrintHook = _gameInteropProvider.HookFromSignature<LuaFuncDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B F9 E8", LuaPrintDetour);

        if (_pluginConfig.EnableLuaLogger)
            Enable();
    }

    public void Dispose()
    {
        Disable();
        _luaPanicHook?.Dispose();
        _luaPrintHook?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Enable()
    {
        _luaPanicHook?.Enable();
        _luaPrintHook?.Enable();
        _logger.LogInformation("Enabled");
    }

    public void Disable()
    {
        _luaPanicHook?.Disable();
        _luaPrintHook?.Disable();
        _logger.LogInformation("Disabled");
    }

    private ulong LuaPanicDetour(lua_State* thisPtr)
    {
        var str = thisPtr->lua_tolstring(-1, null);
        if (str.HasValue && *str.Value != 0)
            _logger.LogError("[panic] {message}", str.ToString());

        return 0;
    }

    private ulong LuaPrintDetour(lua_State* thisPtr)
    {
        var argCount = thisPtr->lua_gettop();

        thisPtr->lua_getfield(LUA_GLOBALSINDEX, "tostring");

        for (var i = 1; i <= argCount; ++i)
        {
            thisPtr->lua_pushvalue(-1);
            thisPtr->lua_pushvalue(i);
            thisPtr->lua_call(1, 1);

            var str = thisPtr->lua_tolstring(-1, null);
            if (str.HasValue && *str.Value != 0)
                _logger.LogDebug("[print] {message}", str.ToString());

            thisPtr->lua_settop(-1);
        }

        return 0;
    }
}
