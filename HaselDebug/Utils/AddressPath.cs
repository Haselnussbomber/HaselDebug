namespace HaselDebug.Utils;

public record AddressPath
{
    public nint[] Path { get; private set; }

    public AddressPath()
    {
        Path = [];
    }

    public AddressPath(nint value)
    {
        Path = [value];
    }

    public AddressPath(nint[] path)
    {
        Path = path;
    }

    public AddressPath With(nint value)
        => new([.. Path, value]);

    public AddressPath With(nint[] values)
        => new([.. Path, .. values]);

    public override string ToString() => GetHashCode().ToString("X");

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var address in Path)
            hashCode.Add(address);
        return hashCode.ToHashCode();
    }
}
