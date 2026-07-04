using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using HaselDebug.Models.SqPack;
using HaselDebug.Utils.SqPack;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public partial class PathList : IDisposable
{
    public static string AppDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HaselDebug");
    public static string PathListCachePath => Path.Combine(AppDataPath, "CurrentPathListWithHashes.gz");

    private readonly IPluginLog _logger;
    private readonly IDataManager _dataManager;

    private readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, SqNode> _paths = [];
    private readonly Dictionary<SqHash, SqNode> _nodes = [];
    private readonly Dictionary<SqFolderHash, HashSet<SqNode>> _folderContents = [];
    private readonly object _processLock = new();

    public bool IsCached { get; private set; }
    public PathListStatus Status { get; private set; }
    public int Count { get; private set; }
    public int TotalCount { get; private set; }
    public double LoadProgress { get; private set; }

    public SqNode? RootNode => _nodes.GetValueOrDefault(new SqHash("", true));

    [AutoPostConstruct]
    private void Initialize()
    {
        IsCached = File.Exists(PathListCachePath);
        Status = PathListStatus.NotLoaded;
    }

    public void Dispose()
    {
        Clear();
    }

    public async Task LoadPathList(bool download = false)
    {
        try
        {
            if (download)
            {
                await DownloadPathList();
            }

            lock (_processLock)
            {
                Clear();

                _logger.Information("Processing path list");

                Status = PathListStatus.Loading;
                LoadCachedPathList();
                Status = PathListStatus.Loaded;

                _logger.Information("Loaded path lists");
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to process path lists");
            Status = PathListStatus.Error;
        }
    }

    public IEnumerable<SqNode> GetNodesInFolder(SqFolderHash folderHash)
    {
        lock (_processLock)
        {
            if (!_folderContents.TryGetValue(folderHash, out var nodes))
                return [];

            return [.. nodes];
        }
    }

    public bool TryGetNodeByPath(string path, [NotNullWhen(returnValue: true)] out SqNode? node)
    {
        return _paths.TryGetValue(path, out node);
    }

    public bool TryGetNodeByHash(SqHash hash, [NotNullWhen(returnValue: true)] out SqNode? node)
    {
        return _nodes.TryGetValue(hash, out node);
    }

    private void Clear()
    {
        _nodes.Clear();
        _paths.Clear();
        _folderContents.Clear();
        Count = 0;
        LoadProgress = 0;
        Status = PathListStatus.NotLoaded;
    }

    private async Task DownloadPathList()
    {
        Status = PathListStatus.Downloading;

        if (!Directory.Exists(AppDataPath))
            Directory.CreateDirectory(AppDataPath);

        if (File.Exists(PathListCachePath))
            File.Delete(PathListCachePath);

        await using var req = await _httpClient.GetStreamAsync("https://rl2.perchbird.dev/download/export/PathListWithHashes.gz");
        using var reader = new StreamReader(req);

        await using var writer = new StreamWriter(PathListCachePath);
        await reader.BaseStream.CopyToAsync(writer.BaseStream);

        _logger.Information("Downloaded pathlist to {path}", PathListCachePath);

        IsCached = true;
        Status = PathListStatus.Downloaded;
    }

    private void LoadCachedPathList()
    {
        if (_dataManager.GameData == null)
            throw new NullReferenceException("GameData is not set");

        using var stream = File.OpenRead(PathListCachePath);
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new Utf8CsvReader(gzip);

        var totalBytes = stream.Length;
        var linesRead = 0;

        Count = 0;
        TotalCount = FileUtils.CountLines(PathListCachePath);

        _nodes.EnsureCapacity(TotalCount);
        _paths.EnsureCapacity(TotalCount);

        var rootHash = new SqHash("", true);
        var rootNode = new SqNode("", rootHash);
        _nodes.TryAdd(rootHash, rootNode);
        _paths.TryAdd("", rootNode);

        var unkHash = new SqHash("<unknown>", true);
        var unkNode = new SqNode("<unknown>", unkHash);
        _nodes.TryAdd(unkHash, unkNode);
        _paths.TryAdd("<unknown>", unkNode);

        _folderContents[unkHash.Folder] = [];
        _folderContents[rootHash.Folder] = [unkNode];

        reader.ReadNextRow(); // skip header

        while (reader.ReadNextRow())
        {
            var row = reader.GetRowReader();

            if (!row.Skip() || !row.TryRead(out uint folderhash) || !row.TryRead(out uint filehash) || !row.TryRead(out uint fullhash) || !row.TryRead(out var path))
            {
                TotalCount--;
                continue;
            }

            if (!_dataManager.FileExists(path))
            {
                TotalCount--;
                continue;
            }

            var hash = new SqHash()
            {
                Full = fullhash,
                Folder = folderhash,
                File = filehash,
            };

            var node = new SqNode(path, hash);

            _nodes[hash] = node;
            _paths[path] = node;

            if (!_folderContents.TryGetValue(folderhash, out var children))
                _folderContents[folderhash] = children = [];

            children.Add(node);

            var pathSpan = path.AsSpan();
            if (pathSpan.EndsWith("/"))
                pathSpan = pathSpan[..^1];

            while (true)
            {
                var lastSlash = pathSpan.LastIndexOf('/');
                if (lastSlash <= 0)
                    break;

                pathSpan = pathSpan[..lastSlash];
                var parentPath = pathSpan.ToString();
                var parentHash = new SqHash(parentPath, true);

                if (_nodes.ContainsKey(parentHash))
                    break;

                var parentNode = new SqNode(parentPath, parentHash);

                _nodes[parentHash] = parentNode;
                _paths[parentPath] = parentNode;

                var grandParentSlash = pathSpan.LastIndexOf('/');
                uint grandParentHash = grandParentSlash <= 0
                    ? rootHash.Folder
                    : new SqHash(pathSpan[..grandParentSlash], true).Folder;

                if (!_folderContents.TryGetValue(grandParentHash, out var gpChildren))
                    _folderContents[grandParentHash] = gpChildren = [];

                gpChildren.Add(parentNode);
            }

            if (++linesRead % 10000 == 0 && totalBytes > 0)
            {
                Count = linesRead;
                LoadProgress = (double)stream.Position / totalBytes;
            }
        }

        Count = linesRead;
        LoadProgress = 1.0;

        var gamePath = _dataManager.GameData.DataPath.FullName;
        foreach (var index in GameIndexReader.LoadAllIndexData(gamePath))
        {
            foreach (var indexEntry in index.CombinedIndexEntries.Values)
            {
                var hash = new SqHash
                {
                    File = indexEntry.FileHash,
                    Folder = indexEntry.FolderHash,
                    Full = indexEntry.FullHash
                };

                if (_nodes.ContainsKey(hash))
                    continue;

                var path = TryGetNodeByHash(hash with { File = 0, Full = hash.Folder.Value }, out var folderNode)
                    ? $"{folderNode.Path}/~{indexEntry.FullHash:X8}"
                    : $"~{indexEntry.FolderHash:X8}/~{indexEntry.FullHash:X8}";

                var node = new SqNode(path, hash);

                _nodes[hash] = node;
                _paths[path] = node;

                if (!_folderContents.TryGetValue(indexEntry.FolderHash, out var children))
                {
                    _folderContents[indexEntry.FolderHash] = children = [];

                    var folderPath = $"~{indexEntry.FolderHash:X8}";
                    var unkFolderHash = new SqHash
                    {
                        File = 0,
                        Folder = indexEntry.FolderHash,
                        Full = indexEntry.FolderHash
                    };

                    if (!_nodes.ContainsKey(unkFolderHash))
                    {
                        var unkFolderNode = new SqNode(folderPath, unkFolderHash)
                        {
                            Name = folderPath
                        };

                        _nodes[unkFolderHash] = unkFolderNode;
                        _paths[folderPath] = unkFolderNode;

                        _folderContents[unkHash.Folder].Add(unkFolderNode);
                    }
                }

                children.Add(node);
            }
        }

        GC.Collect();
    }
}

public enum PathListStatus
{
    NotLoaded,
    Loading,
    Loaded,
    Downloading,
    Downloaded,
    Error,
}
