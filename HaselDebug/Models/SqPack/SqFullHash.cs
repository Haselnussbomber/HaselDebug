namespace HaselDebug.Models.SqPack;

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct SqFullHash : IEquatable<SqFullHash>
{
    [FieldOffset(0x00)] public uint Value;

    public bool Equals(SqFullHash other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is SqFullHash hash && Equals(hash);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("X");
    public static bool operator ==(SqFullHash left, SqFullHash right) => left.Equals(right);
    public static bool operator !=(SqFullHash left, SqFullHash right) => !(left == right);

    public static implicit operator uint(SqFullHash hash) => hash.Value;
    public static implicit operator SqFullHash(uint value) => new() { Value = value };
}
