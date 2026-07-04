using System.IO;
using System.IO.Compression;

namespace HaselDebug.Utils.SqPack;

public static class FileUtils
{
    public static int CountLines(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified file was not found.", filePath);

        Span<byte> buffer = stackalloc byte[64 * 1024];
        var lineCount = 0;

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: buffer.Length);
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);

        int bytesRead;
        while ((bytesRead = gzip.Read(buffer)) > 0)
            lineCount += buffer[..bytesRead].Count((byte)'\n');

        return lineCount;
    }
}
