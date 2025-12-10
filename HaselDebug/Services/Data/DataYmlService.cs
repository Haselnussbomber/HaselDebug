using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace HaselDebug.Services.Data;

// Yoinked from https://github.com/pohky/XivReClassPlugin <3

[RegisterSingleton, AutoConstruct]
public partial class DataYmlService
{
    public static nint BaseAddress => unchecked((nint)0x140000000);

    private readonly ILogger<DataYmlService> _logger;
    private readonly IFramework _framework;
    private readonly PluginAssemblyProvider _assemblyProvider;

    public ClientStructsData Data { get; set; } = new();
    public List<ClassInfo> Classes { get; } = [];
    public Dictionary<nint, ClassInfo> ClassMap { get; } = [];

    public event Action? Loaded;

    [AutoPostConstruct]
    private void Initialize()
    {
        Task.Run(Load);
    }

    private async Task Load()
    {
        Classes.Clear();
        ClassMap.Clear();

        using var stream = _assemblyProvider.Assembly.GetManifestResourceStream("HaselDebug.data.yml");
        if (stream == null)
        {
            _logger.LogWarning("Could not find data.yml");
            return;
        }

        using var reader = new StreamReader(stream);

        _logger.LogDebug("Loading...");

        try
        {
            Data = new DeserializerBuilder()
                .WithNodeDeserializer(new AddressDeserializer())
                .Build()
                .Deserialize<ClientStructsData>(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deserializing data.yml");
        }

        UpdateClasses();

        _logger.LogDebug("Loaded {num} classes", Data.Classes.Count);

        _ = _framework.RunOnFrameworkThread(() => Loaded?.Invoke());
    }

    private void UpdateClasses()
    {
        foreach (var kv in Data.Classes)
        {
            try
            {
                if (kv.Value is { VirtualTables.Count: > 1 })
                {
                    foreach (var vTable in kv.Value.VirtualTables)
                    {
                        var vtInfo = new ClassInfo(Data, kv.Key, kv.Value, vTable);
                        Classes.Add(vtInfo);
                        if (vtInfo.Offset != 0)
                            ClassMap[vtInfo.Offset] = vtInfo;
                    }
                }
                else
                {
                    var info = new ClassInfo(Data, kv.Key, kv.Value, null);
                    Classes.Add(info);
                    if (info.Offset == 0) continue;
                    ClassMap[info.Offset] = info;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating classes");
            }
        }
    }

    private class AddressDeserializer : INodeDeserializer
    {
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            value = null;
            var underlyingType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
            if (underlyingType != typeof(nint))
                return false;

            if (!reader.TryConsume<Scalar>(out var scalar) || !TryGetAddress(scalar.Value, out var address))
                return false;

            if (address < BaseAddress)
                return false;

            value = address - BaseAddress;
            return true;
        }

        private static bool TryGetAddress(string value, out nint address)
        {
            address = 0;
            if (value.Length <= 2 || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return false;
            return nint.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }
    }
}
