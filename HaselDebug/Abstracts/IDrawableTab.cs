namespace HaselDebug.Abstracts;

public interface IDrawableTab
{
    string Title { get; }
    string InternalName { get; }
    bool DrawInChild { get; }
    void Draw();
}
