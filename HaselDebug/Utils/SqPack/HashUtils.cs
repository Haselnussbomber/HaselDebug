using System.Diagnostics;
using System.Text;

namespace HaselDebug.Utils.SqPack;

public static class HashUtils
{
    public static (uint Folder, uint File) GetHash(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty)
        {
            return (uint.MaxValue, 0); // root
        }

        Debug.Assert(path.Length < 260); // MAX_PATH

        Span<char> lowerPath = stackalloc char[260];
        Span<byte> byteBuffer = stackalloc byte[520];

        lowerPath = lowerPath[..path.Length];
        path.ToLowerInvariant(lowerPath);

        var lastSlash = lowerPath.LastIndexOf('/');
        var bytes = byteBuffer[..Encoding.UTF8.GetBytes(lowerPath, byteBuffer)];

        if (lastSlash == -1) // no separator
        {
            var hash = Lumina.Misc.Crc32.Get(bytes);
            return (hash, 0);
        }

        var byteCount = Encoding.UTF8.GetByteCount(lowerPath[..lastSlash]);
        var folderHash = Lumina.Misc.Crc32.Get(bytes[..byteCount]);
        var fileHash = Lumina.Misc.Crc32.Get(bytes[(byteCount + 1)..]);

        return (folderHash, fileHash);
    }
}
