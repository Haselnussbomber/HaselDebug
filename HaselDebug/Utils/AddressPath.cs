using System.Linq;

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

    public override string ToString() => string.Join("_", Path.Select(address => address.ToString("X")));
}
