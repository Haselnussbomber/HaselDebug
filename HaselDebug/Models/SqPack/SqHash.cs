using HaselDebug.Utils.SqPack;

namespace HaselDebug.Models.SqPack;

[StructLayout(LayoutKind.Explicit, Size = 0x0C)]
public struct SqHash : IEquatable<SqHash>
{
    [FieldOffset(0x00)] public ulong Combined;
    [FieldOffset(0x00)] public SqFileHash File;
    [FieldOffset(0x04)] public SqFolderHash Folder;
    [FieldOffset(0x08)] public SqFullHash Full;

    public SqHash(ReadOnlySpan<char> path, bool isFolder = false)
    {
        if (isFolder)
        {
            Span<char> lowerPath = stackalloc char[256];
            lowerPath = lowerPath[..path.Length];
            path.ToLowerInvariant(lowerPath);
            var hash = Lumina.Misc.Crc32.Get(lowerPath);
            Folder = hash;
            Full = hash;
        }
        else
        {
            (File, Folder) = HashUtils.GetHash(path);
            Full = Lumina.Misc.Crc32.Get(path);
        }
    }

    public SqHash(string path, bool isFolder = false) : this(path.AsSpan(), isFolder)
    {
    }

    public bool Equals(SqHash other) => Combined == other.Combined && Full == other.Full;
    public override bool Equals(object? obj) => obj is SqHash hash && Equals(hash);
    public override int GetHashCode() => HashCode.Combine(Combined, Full);
    public static bool operator ==(SqHash left, SqHash right) => left.Equals(right);
    public static bool operator !=(SqHash left, SqHash right) => !(left == right);
}
