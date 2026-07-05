using Lumina;

namespace HaselDebug.Models.SqPack;

public record SqNode
{
    public SqNode(string path, SqHash hash)
    {
        Hash = hash;
        Path = path;
        Ext = System.IO.Path.GetExtension(path);
        IsUnknown = path.StartsWith('~');
    }

    private bool _isMetaDataRead;

    public SqHash Hash { get; }
    public string Path { get; set; }
    public string Ext { get; set; }
    public bool IsUnknown { get; }
    public long? Size { get; set; }
    public string? SizeString { get; set; }

    public void UpdateFileMetaData(GameData gameData)
    {
        if (_isMetaDataRead)
            return;

        if (gameData.GetFileMetadata(Path) is not { } fileMetaData)
        {
            _isMetaDataRead = true;
            return;
        }

        Size = fileMetaData.RawFileSize;
        SizeString = FileUtils.GetHumanReadableSize(fileMetaData.RawFileSize);

        _isMetaDataRead = true;
    }
}
