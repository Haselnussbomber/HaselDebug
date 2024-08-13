namespace HaselDebug.Abstracts;

public interface IDrawableTab
{
    string InternalName { get; }
    bool DrawInChild { get; }
    void Draw();
}
