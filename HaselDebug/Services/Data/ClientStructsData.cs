using YamlDotNet.Serialization;

namespace HaselDebug.Services.Data;

// Yoinked from https://github.com/pohky/XivReClassPlugin <3

public sealed class ClientStructsData {
    [YamlMember(Alias = "version")] public string Version { get; set; } = string.Empty;
    [YamlMember(Alias = "globals")] public Dictionary<ulong, string> Globals { get; set; } = [];
    [YamlMember(Alias = "functions")] public Dictionary<ulong, string> Functions { get; set; } = [];
    [YamlMember(Alias = "classes")] public Dictionary<string, XivClass?> Classes { get; set; } = [];
}

public sealed class XivClass {
    [YamlMember(Alias = "instances")] public List<XivInstance> Instances { get; set; } = [];
    [YamlMember(Alias = "funcs")] public Dictionary<ulong, string> Functions { get; set; } = [];
    [YamlMember(Alias = "vtbls")] public List<XivVTable>? VirtualTables { get; set; } = [];
    [YamlMember(Alias = "vfuncs")] public Dictionary<int, string> VirtualFunctions { get; set; } = [];
}

public sealed class XivVTable {
    [YamlMember(Alias = "ea")] public ulong Address { get; set; }
    [YamlMember(Alias = "base")] public string Base { get; set; } = string.Empty;
}

public sealed class XivInstance {
    [YamlMember(Alias = "ea")] public ulong Address { get; set; }
    [YamlMember(Alias = "name")] public string? Name { get; set; } = string.Empty;
    [YamlMember(Alias = "pointer")] public bool IsPointer { get; set; } = false;
}
