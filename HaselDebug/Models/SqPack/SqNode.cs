using Lumina.Data;

namespace HaselDebug.Models.SqPack;

public record SqNode
{
    public SqNode(string path, SqHash hash)
    {
        Hash = hash;
        IsDirectory = hash.File == 0;
        Name = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name))
        {
            Name = path; // Root or similar
        }
        Path = path;
    }

    public SqHash Hash { get; }
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsDirectory { get; }
    public bool HasMetdata { get; set; }
    public FileResource? FileResource { get; set; }
}
