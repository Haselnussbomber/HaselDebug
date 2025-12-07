using Iced.Intel;

namespace HaselDebug.Utils;

public sealed class NativeCodeReader(nint address) : CodeReader
{
    private int _position;

    public bool CanReadByte => _position < 1024; // TODO?

    public override unsafe int ReadByte()
    {
        return ((byte*)address)[_position++];
    }
}
