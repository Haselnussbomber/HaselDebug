using System.Collections.Immutable;
using HaselDebug.Tabs;

namespace HaselDebug.Interfaces;

public interface IDrawableTab
{
    string Title { get; }
    string InternalName { get; }
    bool DrawInChild { get; }
    bool IsEnabled { get; }
    bool IsPinnable { get; }
    bool CanPopOut { get; }
    ImmutableArray<ISubTab<UnlocksTab>>? SubTabs { get; }
    void Draw();
}
