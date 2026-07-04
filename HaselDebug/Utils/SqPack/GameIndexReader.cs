using System.IO;

namespace HaselDebug.Utils.SqPack;

public static class GameIndexReader
{
    public static List<CompositeIndexInfo> LoadAllIndexData(string directory)
    {
        var index1s = GetIndexFiles(directory, "*.index");
        var index2s = GetIndexFiles(directory, "*.index2");

        var combined = new List<CompositeIndexInfo>();

        foreach (var index1Element in index1s.ToArray())
        {
            if (index2s.TryGetValue(index1Element.Key, out var index2))
            {
                combined.Add(new CompositeIndexInfo(index1Element.Value, index2));
                index1s.Remove(index1Element.Key);
                index2s.Remove(index1Element.Key);
            }
        }

        foreach (var index1Element in index1s)
        {
            combined.Add(new CompositeIndexInfo(index1Element.Value, null));
        }

        foreach (var index2Element in index2s)
        {
            combined.Add(new CompositeIndexInfo(null, index2Element.Value));
        }

        return combined;
    }

    private static Dictionary<string, IndexFile> GetIndexFiles(string directory, string pattern)
    {
        var paths = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
        var dict = new Dictionary<string, IndexFile>();
        foreach (var path in paths)
        {
            // Skip textools backup/cache stuff, usually we don't need it, but keep it if present in game dir?
            if (path.Contains("090000"))
                continue;

            dict[Path.GetFileNameWithoutExtension(path)] = IndexFile.Read(path);
        }
        return dict;
    }
}
