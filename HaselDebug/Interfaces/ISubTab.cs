namespace HaselDebug.Interfaces;

public interface ISubTab<TTab> : IDebugTab where TTab : IDebugTab;
