using System.Buffers.Binary;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace HaselDebug.Extensions;

public static class ResourceHandleExtensions
{
    extension(ref ResourceHandle resourceHandle)
    {
        public string FileTypeString => Encoding.ASCII.GetString(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(resourceHandle.FileType))).Trim('\0');
    }
}
