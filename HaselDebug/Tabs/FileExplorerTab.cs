using System.Threading;
using System.Threading.Tasks;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Models.SqPack;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class FileExplorerTab : DebugTab, IDisposable
{
    private readonly PathList _pathList;
    private readonly IFramework _framework;

    private string _filterTerm = string.Empty;
    private List<SqNode>? _filteredNodes;
    private Debouncer _filterDebouncer;
    private CancellationTokenSource? _filterCts;
    private Task? _filterTask;
    private bool _initialized;

    [AutoPostConstruct]
    private void Initialize()
    {
        _filterDebouncer = _framework.CreateDebouncer(TimeSpan.FromMilliseconds(100), StartFilter);
        _pathList.StatusChange += OnStatusChange;
    }

    public void Dispose()
    {
        _filterDebouncer.Dispose();
        _pathList.StatusChange -= OnStatusChange;
    }

    private void OnStatusChange(PathListStatus status)
    {
        if (status == PathListStatus.Loaded)
        {
            StartFilter();
        }

        if (status == PathListStatus.NotLoaded)
        {
            CancelFilter();
        }
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            if (_pathList.Status == PathListStatus.NotLoaded && _pathList.IsCached)
            {
                Task.Run(() => _pathList.LoadPathList(!_pathList.IsCached));
            }

            _initialized = true;
        }

        if (_pathList.Status == PathListStatus.NotLoaded)
        {
            ImGui.Text("The Path List is not loaded.");
            ImGui.Text("You need to download the path list to use the File Explorer.");

            if (ImGui.Button("Download & Load Path List"))
            {
                Task.Run(() => _pathList.LoadPathList(true));
            }

            return;
        }

        if (_pathList.Status is PathListStatus.Loading or PathListStatus.Downloading or PathListStatus.Processing)
        {
            switch (_pathList.Status)
            {
                case PathListStatus.Downloading:
                    ImGui.Text("Downloading..."u8);
                    break;
                case PathListStatus.Loading:
                    ImGui.Text("Loading..."u8);
                    ImGuiUtilsEx.ProgressBar((float)_pathList.LoadProgress, new Vector2(-1, 0));
                    break;
                case PathListStatus.Processing:
                    ImGui.Text("Processing unknown paths..."u8);
                    ImGuiUtilsEx.ProgressBar((float)(-1.0 * ImGui.GetTime()), new Vector2(-1, 0));
                    break;
            }

            return;
        }

        if (_pathList.Status != PathListStatus.Loaded)
        {
            ImGui.Text($"Status: {_pathList.Status}");
            return;
        }

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##SearchTermInput"u8, "Search..."u8, ref _filterTerm, 512, ImGuiInputTextFlags.AutoSelectAll))
            StartFilter();

        if (_filterTask != null)
            ImGuiUtilsEx.ProgressBar((float)(-1.0f * ImGui.GetTime()), new Vector2(-1, 1));
        else
            ImGui.Dummy(new Vector2(-1, 1));

        using var table = ImRaii.Table("FileTable"u8, 1, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        if (_filteredNodes == null)
            return;

        using var clip = new ImRaiiListClipper(_filteredNodes.Count, ImGui.GetTextLineHeightWithSpacing());

        foreach (var row in clip)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Path
            ImGui.Selectable(_filteredNodes[row].Path);
        }
    }

    private void StartFilter()
    {
        CancelFilter();
        _filterCts ??= new();
        _filterTask = Task.Run(FilterList, _filterCts.Token);
    }

    private void FilterList()
    {
        var filterTerm = _filterTerm;

        if (string.IsNullOrEmpty(filterTerm))
        {
            _filteredNodes = _pathList.Nodes.Values
                .AsParallel()
            .WithCancellation(_filterCts!.Token)
                .OrderBy(n => n.Path, StringComparer.Ordinal)
                .ToList();

            _filterTask = null;
            return;
        }

        _filteredNodes = _pathList.Nodes.Values
            .AsParallel()
            .WithCancellation(_filterCts!.Token)
            .Where(n => n.Path.Contains(filterTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.Path, StringComparer.Ordinal)
            .ToList();

        _filterTask = null;
    }

    private void CancelFilter()
    {
        _filterCts?.Cancel();
        _filterCts?.Dispose();
        _filterCts = null;
        _filterTask = null;
    }
}
