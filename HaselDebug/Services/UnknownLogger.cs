using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon.Game;

namespace HaselDebug.Services;

[RegisterSingleton, RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class UnknownLogger : IHostedService
{
    private readonly IFramework _framework;

    private readonly Dictionary<WarpType, HashSet<uint>> _loggedWrapTypes = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _framework.Update += OnFrameworkUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _framework.Update -= OnFrameworkUpdate;
        return Task.CompletedTask;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var warpInfo = WarpInfo.Instance();
        if (warpInfo->WarpType != WarpType.None && (Enum.GetName(warpInfo->WarpType) == null || Enum.GetName(warpInfo->WarpType)?.StartsWith("Unk") == true))
        {
            if (!_loggedWrapTypes.TryGetValue(warpInfo->WarpType, out var territoryTypes))
                _loggedWrapTypes.Add(warpInfo->WarpType, territoryTypes = []);

            var territoryTypeFrom = GameMain.Instance()->CurrentTerritoryTypeId;
            var territoryTypeTo = GameMain.Instance()->TransitionTerritoryTypeId;

            if (territoryTypes.Add(territoryTypeFrom))
            {
                using var rssb = new RentedSeStringBuilder();
                Chat.Print(rssb.Builder
                    .PushColorType(32)
                    .Append("[HaselDebug] ")
                    .PopColorType()
                    .Append($"Detected WarpType {warpInfo->WarpType} with TerritoryTypeId {territoryTypeFrom} -> {territoryTypeTo}").ToReadOnlySeString());
                UIGlobals.PlaySoundEffect(44);
            }
        }
    }
}
