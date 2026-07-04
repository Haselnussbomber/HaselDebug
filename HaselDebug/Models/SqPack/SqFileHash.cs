namespace HaselDebug.Models.SqPack;

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct SqFileHash : IEquatable<SqFileHash>
{
    [FieldOffset(0x00)] public uint Value;

    public bool Equals(SqFileHash other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is SqFileHash hash && Equals(hash);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("X");
    public static bool operator ==(SqFileHash left, SqFileHash right) => left.Equals(right);
    public static bool operator !=(SqFileHash left, SqFileHash right) => !(left == right);

    public static implicit operator uint(SqFileHash hash) => hash.Value;
    public static implicit operator SqFileHash(uint value) => new() { Value = value };
}
