namespace HaselDebug.Abstracts;

public interface IDebugTab : IEquatable<IDebugTab>
{
    string GetTitle();
    void Draw();
    bool DrawInChild { get; }
}
