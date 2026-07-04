using System.IO;
using System.Threading.Tasks;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Models.SqPack;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class FileExplorerTab : DebugTab
{
    private readonly PathList _pathList;
    private readonly IDataManager _dataManager;
    private readonly ITextureProvider _textureProvider;

    private readonly Dictionary<SqFolderHash, bool> _hasFilesCache = [];

    private string _exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HaselDebugExport");
    private bool _thumbnailView = false;
    private SqNode? _selectedFolderNode = null;
    private bool _autoLoadStarted = false;

    public override void Draw()
    {
        if (_pathList.Status == PathListStatus.NotLoaded)
        {
            ImGui.Text("The Path List is not loaded.");
            ImGui.Text("You need to load the path list to use the File Explorer.");

            if (_pathList.IsCached)
            {
                if (!_autoLoadStarted && _pathList.Status == PathListStatus.NotLoaded)
                {
                    _autoLoadStarted = true;
                    Task.Run(() => _pathList.LoadPathList(!_pathList.IsCached));
                }
            }
            else
            {
                if (ImGui.Button("Download & Load Path List"))
                {
                    Task.Run(() => _pathList.LoadPathList(true));
                }
            }

            return;
        }

        if (_pathList.Status is PathListStatus.Loading or PathListStatus.Downloading)
        {
            switch (_pathList.Status)
            {
                case PathListStatus.Downloading:
                    ImGui.Text("Downloading..."u8);
                    break;
                case PathListStatus.Loading:
                    ImGui.Text("Loading..."u8);
                    ImGui.ProgressBar((float)_pathList.LoadProgress, new Vector2(-1, 0));
                    break;
            }

            return;
        }

        if (_pathList.Status != PathListStatus.Loaded)
        {
            ImGui.Text($"Status: {_pathList.Status}");
            return;
        }

        ImGui.InputText("Export Directory", ref _exportDirectory, 512);

        var root = _pathList.RootNode;
        if (root == null)
            return;

        ImGui.Separator();

        ImGui.Columns(2, "FileExplorerColumns", true);

        using (var fileTreeChild = ImRaii.Child("FileTree"u8))
        {
            if (fileTreeChild)
            {
                var rootChildren = _pathList.GetNodesInFolder(root.Hash.Folder)
                    .Where(n => n != root && n.IsDirectory && HasFilesRecursively(n))
                    .Sort();

                foreach (var child in rootChildren)
                {
                    DrawNode(child);
                }
            }
        }

        ImGui.NextColumn();

        using (var folderContentsChild = ImRaii.Child("FolderContents"u8))
        {
            if (folderContentsChild.Success && _selectedFolderNode != null)
            {
                ImGui.Checkbox("Thumbnail View", ref _thumbnailView);
                ImGui.Separator();

                if (_thumbnailView)
                {
                    DrawThumbnailView(_selectedFolderNode);
                }
                else
                {
                    DrawListView(_selectedFolderNode);
                }
            }
        }

        ImGui.Columns(1);
    }

    private bool HasFilesRecursively(SqNode folderNode)
    {
        if (_hasFilesCache.TryGetValue(folderNode.Hash.Folder, out var hasFiles))
            return hasFiles;

        foreach (var child in _pathList.GetNodesInFolder(folderNode.Hash.Folder).Where(n => n != folderNode))
        {
            if (!child.IsDirectory)
            {
                _hasFilesCache[folderNode.Hash.Folder] = true;
                return true;
            }

            if (HasFilesRecursively(child))
            {
                _hasFilesCache[folderNode.Hash.Folder] = true;
                return true;
            }
        }

        _hasFilesCache[folderNode.Hash.Folder] = false;
        return false;
    }

    private void DrawNode(SqNode node)
    {
        if (!node.IsDirectory || !HasFilesRecursively(node))
            return;

        using var id = ImRaii.PushId($"Node_{node.Hash.Folder}_{node.Hash.File}_{node.Hash.Full}");

        var hasSubFolders = _pathList.GetNodesInFolder(node.Hash.Folder)
            .Any(n => n != node && n.IsDirectory && HasFilesRecursively(n));

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (_selectedFolderNode == node)
            flags |= ImGuiTreeNodeFlags.Selected;
        if (!hasSubFolders)
            flags |= ImGuiTreeNodeFlags.Leaf;

        using var expanded = ImRaii.TreeNode(node.Name, flags);

        if (ImGui.IsItemClicked())
        {
            _selectedFolderNode = node;
        }

        DrawContextMenu(node);

        if (!expanded)
            return;

        var children = _pathList.GetNodesInFolder(node.Hash.Folder)
            .Where(n => n != node && n.IsDirectory && HasFilesRecursively(n))
            .Sort();

        foreach (var child in children)
        {
            DrawNode(child);
        }
    }

    private void DrawListView(SqNode folderNode)
    {
        var children = _pathList.GetNodesInFolder(folderNode.Hash.Folder)
            .Where(n => n != folderNode && !n.IsDirectory)
            .Sort();

        foreach (var child in children)
        {
            using var id = ImRaii.PushId($"Node_{child.Hash.Folder}_{child.Hash.File}_{child.Hash.Full}");

            ImGui.Selectable(child.Name);

            DrawContextMenu(child);
        }
    }

    private void DrawThumbnailView(SqNode folderNode)
    {
        var children = _pathList.GetNodesInFolder(folderNode.Hash.Folder)
            .Where(n => n != folderNode && !n.IsDirectory)
            .Sort();

        var itemWidth = 100f * ImGuiHelpers.GlobalScale;
        var padding = ImGui.GetStyle().ItemSpacing.X;
        var windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;

        foreach (var child in children)
        {
            using var id = ImRaii.PushId($"Node_{child.Hash.Folder}_{child.Hash.File}_{child.Hash.Full}");

            ImGui.BeginGroup();

            var isTexture = child.Name.EndsWith(".tex") || child.Name.EndsWith(".atex");
            var textureDrawn = false;

            if (isTexture && _textureProvider.GetFromGame(child.Path).TryGetWrap(out var texWrap, out _))
            {
                var aspectRatio = (float)texWrap.Width / texWrap.Height;
                var drawHeight = itemWidth / aspectRatio;
                ImGui.Image(texWrap.Handle, new Vector2(itemWidth, drawHeight));
                textureDrawn = true;
            }

            if (!textureDrawn)
            {
                ImGui.Dummy(new Vector2(itemWidth, itemWidth)); // Placeholder
            }

            var textWidth = ImGui.CalcTextSize(child.Name).X;
            if (textWidth > itemWidth)
            {
                ImGui.TextWrapped(child.Name); // May break layout if too long
            }
            else
            {
                var textOffset = (itemWidth - textWidth) / 2;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + textOffset);
                ImGui.Text(child.Name);
            }

            ImGui.EndGroup();

            DrawContextMenu(child);

            var lastButtonX2 = ImGui.GetItemRectMax().X;
            var nextButtonX2 = lastButtonX2 + padding + itemWidth;
            if (nextButtonX2 < windowVisibleX2)
                ImGui.SameLine();
        }
    }

    private void DrawContextMenu(SqNode node)
    {
        using var popup = ImRaii.ContextPopupItem($"Context_{node.Hash.Folder}_{node.Hash.File}_{node.Hash.Full}");
        if (!popup)
            return;

        if (node.IsDirectory && ImGui.MenuItem("Export Folder"))
        {
            ExportFolder(node);
        }
        if (!node.IsDirectory && ImGui.MenuItem("Export File"))
        {
            ExportFile(node);
        }
    }

    private void ExportFile(SqNode node)
    {
        try
        {
            if (node.Path.StartsWith('~'))
            {
                // Can't export unknown paths simply via GetFile
                return;
            }

            var file = _dataManager.GetFile(node.Path);
            if (file != null)
            {
                var dir = Path.Combine(_exportDirectory, node.Path.Replace('/', '\\'));
                dir = Path.GetDirectoryName(dir);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var outPath = Path.Combine(_exportDirectory, node.Path.Replace('/', '\\'));
                File.WriteAllBytes(outPath, file.Data);
            }
        }
        catch
        {
            // ignore for now
        }
    }

    private void ExportFolder(SqNode folderNode)
    {
        var children = _pathList.GetNodesInFolder(folderNode.Hash.Folder)
            .Where(n => n != folderNode);

        foreach (var child in children)
        {
            if (child.IsDirectory)
                ExportFolder(child);
            else
                ExportFile(child);
        }
    }
}

public static class IEnumerableSqNodeExtensions
{
    public static IEnumerable<SqNode> Sort(this IEnumerable<SqNode> nodes)
    {
        return nodes
            .OrderBy(node => !node.IsDirectory)
            .ThenBy(node => node.Name.StartsWith('~'))
            .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase);
    }
}
