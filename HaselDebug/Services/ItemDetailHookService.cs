/*
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselDebug.Services;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append)]
public unsafe class ItemDetailHookService : IHostedService, IDisposable
{
    private readonly Hook<AddonItemDetail.Delegates.Hide> _hideHook;

    public ItemDetailHookService(IGameInteropProvider gameInteropProvider)
    {
        _hideHook = gameInteropProvider.HookFromSignature<AddonItemDetail.Delegates.Hide>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 0F B6 F2 48 8B E9 48 81 C1", HideDetour);
    }

    public void Dispose()
    {
        _hideHook.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hideHook.Enable();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hideHook.Disable();
        return Task.CompletedTask;
    }

    private void HideDetour(AddonItemDetail* thisPtr, bool unkBool, bool callHideCallback, uint setShowHideFlags)
    {

    }
}
*/
