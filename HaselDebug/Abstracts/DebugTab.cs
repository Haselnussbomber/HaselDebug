using System.Text.RegularExpressions;

namespace HaselDebug.Abstracts;

public abstract partial class DebugTab : IDebugTab
{
    private string? _title = null;
    public virtual string Title => _title ??= NameRegex().Replace(TabRegex().Replace(GetType().Name, ""), "$1 $2");
    public virtual bool DrawInChild => true;
    public virtual string InternalName => GetType().Name;

    [GeneratedRegex("Tab$")]
    private static partial Regex TabRegex();

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex NameRegex();

    public virtual void Draw() { }

    public bool Equals(IDebugTab? other)
    {
        return other?.Title == _title;
    }
}
