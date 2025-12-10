using System.Text.RegularExpressions;

namespace HaselDebug.Services.Data;

// Yoinked from https://github.com/pohky/XivReClassPlugin <3

public partial class ClassInfo
{
    [GeneratedRegex("\\w+::", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NameRegex();

    [GeneratedRegex("(?(?=.*<)::|::(?!.*>))", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NamespaceSplitRegex();

    private string _inheritanceNameCache = string.Empty;
    private string _inheritanceNameFullCache = string.Empty;

    public string Name { get; set; }
    public string Namespace { get; set; }
    public string FullName { get; set; }
    public nint Offset { get; set; }
    public ClassInfo? ParentClass { get; set; }
    public Dictionary<nint, string> Functions { get; } = [];
    public Dictionary<int, string> VirtualFunctions { get; } = [];
    public Dictionary<nint, string> Instances { get; } = [];

    public ClassInfo(ClientStructsData data, string rawName, XivClass? xivClass, XivVTable? baseClass)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            rawName = string.Empty;

        Name = NameRegex().Replace(rawName, string.Empty);
        var nsSplit = NamespaceSplitRegex().Split(rawName);
        if (nsSplit.Length > 1)
        {
            nsSplit[^1] = Name;
            Namespace = string.Join("::", nsSplit.Take(nsSplit.Length - 1));
        }
        else
        {
            Namespace = string.Empty;
        }

        FullName = string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}::{Name}";

        if (xivClass == null)
            return;

        var baseVtable = baseClass ?? xivClass.VirtualTables?.FirstOrDefault();

        Offset = baseVtable?.Address ?? 0;

        try
        {
            foreach (var func in xivClass.Functions)
            {
                if (func.Key == 0 || string.IsNullOrWhiteSpace(func.Value))
                    continue;
                Functions[func.Key] = $"{FullName}.{func.Value}";
            }
        }
        catch (NullReferenceException)
        {
            /* ignored, can happen with incomplete defs after patch */
        }

        try
        {
            foreach (var vf in xivClass.VirtualFunctions)
            {
                if (string.IsNullOrWhiteSpace(vf.Value))
                    continue;
                VirtualFunctions[vf.Key] = vf.Value;
            }
        }
        catch (NullReferenceException)
        {
            /* ignored, can happen with incomplete defs after patch */
        }

        var parentName = baseVtable?.Base;
        if (!string.IsNullOrEmpty(parentName) && data.Classes.TryGetValue(parentName!, out var parentClass))
        {
            ParentClass = new ClassInfo(data, parentName!, parentClass, null);

            var parent = ParentClass;
            while (parent != null)
            {
                foreach (var vf in parent.VirtualFunctions.Where(vf => !VirtualFunctions.ContainsKey(vf.Key)))
                    VirtualFunctions[vf.Key] = vf.Value;
                parent = parent.ParentClass;
            }
        }

        var instanceIndex = 0;
        foreach (var instance in xivClass.Instances)
        {
            var name = !string.IsNullOrEmpty(instance.Name) ? instance.Name : $"Instance{(instanceIndex++ == 0 ? string.Empty : instanceIndex.ToString())}";
            Instances[instance.Address] = $"{FullName}_{name}";
        }
    }

    public string GetInheritanceName(bool includeNamespace)
    {
        if (includeNamespace)
        {
            if (!string.IsNullOrEmpty(_inheritanceNameFullCache))
                return _inheritanceNameFullCache;
            var name = FullName;
            var parent = ParentClass;
            while (parent != null)
            {
                name += $" : {parent.FullName}";
                parent = parent.ParentClass;
            }

            _inheritanceNameFullCache = name;
            return name;
        }
        else
        {
            if (!string.IsNullOrEmpty(_inheritanceNameCache))
                return _inheritanceNameCache;
            var name = Name;
            var parent = ParentClass;
            while (parent != null)
            {
                name += $" : {parent.Name}";
                parent = parent.ParentClass;
            }

            _inheritanceNameCache = name;
            return name;
        }
    }
}
