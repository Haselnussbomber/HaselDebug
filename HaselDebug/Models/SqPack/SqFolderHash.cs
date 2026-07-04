namespace HaselDebug.Models.SqPack;

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct SqFolderHash : IEquatable<SqFolderHash>
{
    [FieldOffset(0x00)] public uint Value;

    public bool Equals(SqFolderHash other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is SqFolderHash hash && Equals(hash);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("X");
    public static bool operator ==(SqFolderHash left, SqFolderHash right) => left.Equals(right);
    public static bool operator !=(SqFolderHash left, SqFolderHash right) => !(left == right);

    public static implicit operator uint(SqFolderHash hash) => hash.Value;
    public static implicit operator SqFolderHash(uint value) => new() { Value = value };
}
