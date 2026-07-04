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
    private readonly Dictionary<SqHash, SqNode> _nodes = [];
    private List<SqNode> _sortedNodes = [];

    public bool IsCached { get; private set; } = File.Exists(PathListCachePath);
    public PathListStatus Status { get; private set { field = value; StatusChange?.Invoke(value); } } = PathListStatus.NotLoaded;
    public double LoadProgress { get; private set; }

    public IReadOnlyDictionary<SqHash, SqNode> Nodes => _nodes;
    public IReadOnlyList<SqNode> SortedNodes => _sortedNodes;

    public event Action<PathListStatus>? StatusChange;

    public void Dispose()
    {
        Clear();
    }

    private void Clear()
    {
        _nodes.Clear();
        _sortedNodes.Clear();
        LoadProgress = 0;
        Status = PathListStatus.NotLoaded;
    }

    public async Task LoadPathList(bool download = false)
    {
        try
        {
            if (download)
            {
                await DownloadPathList();
            }

            _logger.Information("Processing path list");

            LoadCachedPathList();

            _logger.Information("Loaded path lists");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to process path lists");
            Status = PathListStatus.Error;
        }
    }

    private async Task DownloadPathList()
    {
        Status = PathListStatus.Downloading;

        if (!Directory.Exists(AppDataPath))
            Directory.CreateDirectory(AppDataPath);

        if (File.Exists(PathListCachePath))
            File.Delete(PathListCachePath);

        await using var req = await _httpClient.GetStreamAsync("https://rl2.perchbird.dev/download/export/CurrentPathListWithHashes.gz");
        using var reader = new StreamReader(req);

        await using var writer = new StreamWriter(PathListCachePath);
        await reader.BaseStream.CopyToAsync(writer.BaseStream);

        _logger.Information("Downloaded pathlist to {path}", PathListCachePath);

        IsCached = true;
        Status = PathListStatus.Downloaded;
    }

    private void LoadCachedPathList()
    {
        Clear();

        if (_dataManager.GameData == null)
            throw new NullReferenceException("GameData is not set");

        Status = PathListStatus.Loading;

        using var stream = File.OpenRead(PathListCachePath);
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new Utf8CsvReader(gzip);

        var totalBytes = stream.Length;
        var linesRead = 0;

        _nodes.EnsureCapacity(FileUtils.CountLines(PathListCachePath));

        reader.ReadNextRow(); // skip header

        while (reader.ReadNextRow())
        {
            var row = reader.GetRowReader();

            if (!row.Skip() || !row.TryRead(out uint folderhash) || !row.TryRead(out uint filehash) || !row.TryRead(out uint fullhash) || !row.TryRead(out var path))
                continue;

            if (!_dataManager.FileExists(path))
            {
                _logger.Warning("Path {path} not found.", path);
                continue;
            }

            var hash = new SqHash()
            {
                Full = fullhash,
                Folder = folderhash,
                File = filehash,
            };

            var node = new SqNode(path, hash);

            _nodes.TryAdd(hash, node);

            if (++linesRead % 10000 == 0 && totalBytes > 0)
                LoadProgress = Math.Min((double)stream.Position / totalBytes, 100);
        }

        Status = PathListStatus.Processing;

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

                var node = new SqNode($"~{indexEntry.FolderHash:X8}/~{indexEntry.FullHash:X8}", hash);

                _nodes.TryAdd(hash, node);
            }
        }

        _sortedNodes = _nodes.Values
            .AsParallel()
            .OrderBy(node => node.Path, StringComparer.Ordinal)
            .ToList();

        Status = PathListStatus.Loaded;

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
    Processing,
    Error,
}
