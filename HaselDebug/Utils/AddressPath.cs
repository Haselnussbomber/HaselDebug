using System.Linq;

namespace HaselDebug.Utils;

public record struct AddressPath
{
    public nint[] Path { get; private set; }
    public int Count => Path.Length;

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

    public override string ToString() => string.Join("_", Path.Select(address => address.ToString("X")));
}
