using System.Collections.Immutable;

namespace HaselDebug.Interfaces;

public interface IDebugTab
{
    string Title { get; }
    string InternalName { get; }
    bool DrawInChild { get; }
    bool IsEnabled { get; }
    bool IsPinnable { get; }
    bool CanPopOut { get; }
    ImmutableArray<IDebugTab>? SubTabs { get; }
    void Draw();
}
