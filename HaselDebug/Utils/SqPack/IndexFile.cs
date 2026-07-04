using System.Globalization;
using System.IO;
using System.Text;

namespace HaselDebug.Utils.SqPack;

public struct VersionInfo
{
    public byte PlatformId;
    public uint FileSize;
    public uint Version;
    public uint Type;
    public uint Date;
    public uint Time;
    public uint RegionId;
    public uint LanguageId;
    public byte[] UnknownData;
    public byte[] VersionInfoHash;

    public static VersionInfo Read(BinaryReader2 br)
    {
        var ret = new VersionInfo();
        var magic = br.ReadBytes(6);
        if (Encoding.ASCII.GetString(magic) != "SqPack")
            throw new Exception("Not an SqPack file");
        br.BaseStream.Seek(2, SeekOrigin.Current);
        ret.PlatformId = br.ReadByte();
        br.BaseStream.Seek(3, SeekOrigin.Current);
        ret.FileSize = br.ReadUInt32();
        ret.Version = br.ReadUInt32();
        ret.Type = br.ReadUInt32();
        ret.Date = br.ReadUInt32();
        ret.Time = br.ReadUInt32();
        ret.RegionId = br.ReadUInt32();
        ret.LanguageId = br.ReadUInt32();
        ret.UnknownData = br.ReadBytes(920);
        ret.VersionInfoHash = br.ReadBytes(20);
        br.BaseStream.Seek(44, SeekOrigin.Current);
        return ret;
    }
}

public struct FileInfo
{
    public uint Size;
    public uint Version;
    public uint IndexDataOffset;
    public uint IndexDataSize;
    public byte[] IndexDataHash;
    public uint DataFileCount;
    public uint CollisionOffset;
    public uint CollisionSize;
    public byte[] CollisionDataHash;
    public uint EmptyBlockOffset;
    public uint EmptyBlockSize;
    public byte[] EmptyBlockHash;
    public uint DirectoryOffset;
    public uint DirectorySize;
    public byte[] DirectoryHash;
    public uint IndexType;
    public byte[] UnknownData;
    public byte[] FileInfoHash;

    public static FileInfo Read(BinaryReader2 br)
    {
        var ret = new FileInfo();
        ret.Size = br.ReadUInt32();
        ret.Version = br.ReadUInt32();
        ret.IndexDataOffset = br.ReadUInt32();
        ret.IndexDataSize = br.ReadUInt32();
        ret.IndexDataHash = br.ReadBytes(20);
        br.BaseStream.Seek(44, SeekOrigin.Current);
        ret.DataFileCount = br.ReadUInt32();
        ret.CollisionOffset = br.ReadUInt32();
        ret.CollisionSize = br.ReadUInt32();
        ret.CollisionDataHash = br.ReadBytes(20);
        br.BaseStream.Seek(44, SeekOrigin.Current);
        ret.EmptyBlockOffset = br.ReadUInt32();
        ret.EmptyBlockSize = br.ReadUInt32();
        ret.EmptyBlockHash = br.ReadBytes(20);
        br.BaseStream.Seek(44, SeekOrigin.Current);
        ret.DirectoryOffset = br.ReadUInt32();
        ret.DirectorySize = br.ReadUInt32();
        ret.DirectoryHash = br.ReadBytes(20);
        br.BaseStream.Seek(44, SeekOrigin.Current);
        ret.IndexType = br.ReadUInt32();
        ret.UnknownData = br.ReadBytes(656);
        ret.FileInfoHash = br.ReadBytes(20);
        br.BaseStream.Seek(44, SeekOrigin.Current);
        return ret;
    }
}

public struct Hash32
{
    private bool IsBE;

    public bool IsSynonym => IsBE ? (Data & (0b1 << 31)) == unchecked((uint)(0b1 << 31)) : (Data & 0b1) == 0b1;
    public byte DataFileId => (byte)((Data & 0b1110) >> 1); // these are inaccurate and not used for BE
    public ulong Offset => (ulong)(Data & ~0xF) * 0x08;

    public ulong FileIdentifier => IsBE ? Data & ~0x80_00_00_00 : (ulong)(Data & ~0b1);

    public uint Hash;
    public uint Data;

    public static Hash32 Read(BinaryReader2 br)
    {
        return new Hash32()
        {
            IsBE = br.ByteOrder == ByteOrder.BigEndian,
            Hash = br.ReadUInt32(),
            Data = br.ReadUInt32(),
        };
    }
}

public struct Hash64
{
    private bool IsBE;

    public ulong Hash;
    public uint Data;
    private uint Padding;

    public uint FileHash => (uint)(Hash & 0xFFFFFFFF);
    public uint FolderHash => (uint)((Hash & 0xFFFFFFFF00000000) >> 32);

    public bool IsSynonym => IsBE ? (Data & (0b1 << 31)) == unchecked((uint)(0b1 << 31)) : (Data & 0b1) == 0b1;
    public byte DataFileId => (byte)((Data & 0b1110) >> 1);
    public ulong Offset => (ulong)(Data & ~0xF) * 0x08;

    public ulong FileIdentifier => IsBE ? Data & ~0x80_00_00_00 : (ulong)(Data & ~0b1);

    public static Hash64 Read(BinaryReader2 br)
    {
        return new Hash64()
        {
            IsBE = br.ByteOrder == ByteOrder.BigEndian,
            Hash = br.ReadUInt64(),
            Data = br.ReadUInt32(),
            Padding = br.ReadUInt32(),
        };
    }
}

public struct Collision32
{
    private bool IsBE;

    public uint Hash;
    public uint Unknown;
    public uint Data;
    public uint Index;
    public string Path;

    public byte DataFileId => (byte)((Data & 0b1110) >> 1);
    public long Offset => (Data & ~0xF) * 0x08;

    public ulong FileIdentifier => IsBE ? Data & ~0x80_00_00_00 : (ulong)(Data & ~0b1);

    public static Collision32 Read(BinaryReader2 br)
    {
        var read = new Collision32();
        read.IsBE = br.ByteOrder == ByteOrder.BigEndian;
        read.Hash = br.ReadUInt32();
        read.Unknown = br.ReadUInt32();
        read.Data = br.ReadUInt32();
        read.Index = br.ReadUInt32();
        var tmp = br.ReadBytes(240);
        read.Path = Encoding.ASCII.GetString(tmp).Split('\0')[0];
        return read;
    }
}

public struct Collision64
{
    private bool IsBE;

    public ulong Hash;
    public uint Data;
    public uint Index;
    public string Path;

    public uint FileHash => (uint)(Hash & 0xFFFFFFFF);
    public uint FolderHash => (uint)((Hash & 0xFFFFFFFF00000000) >> 32);

    public byte DataFileId => (byte)((Data & 0b1110) >> 1);
    public long Offset => (Data & ~0xF) * 0x08;

    public ulong FileIdentifier => IsBE ? Data & ~0x80_00_00_00 : (ulong)(Data & ~0b1);

    public static Collision64 Read(BinaryReader2 br)
    {
        var read = new Collision64();
        read.IsBE = br.ByteOrder == ByteOrder.BigEndian;
        read.Hash = br.ReadUInt64();
        read.Data = br.ReadUInt32();
        read.Index = br.ReadUInt32();
        var tmp = br.ReadBytes(240);
        read.Path = Encoding.ASCII.GetString(tmp).Split('\0')[0];
        return read;
    }
}

public class IndexFile
{
    public uint IndexId;
    public string IndexIdentifier => $"{IndexId:X6}";

    public bool IsIndex2 => FileInfo.IndexType == 2;

    public VersionInfo VersionInfo { get; private set; }
    public FileInfo FileInfo { get; private set; }

    public Dictionary<uint, Hash32>? Hashes32 { get; private set; }
    public Dictionary<ulong, Hash64>? Hashes64 { get; private set; }

    public List<Collision32>? Collisions32 { get; private set; }
    public List<Collision64>? Collisions64 { get; private set; }

    private static byte CheckPlatform(MemoryStream ms)
    {
        ms.Position = 0;
        ms.Seek(8, SeekOrigin.Begin);
        var plat = ms.ReadByte();
        ms.Seek(0, SeekOrigin.Begin);
        return (byte)plat;
    }

    public static IndexFile Read(string path)
    {
        var data = File.ReadAllBytes(path);
        using var ms = new MemoryStream(data);
        var platform = CheckPlatform(ms);
        using var br = new BinaryReader2(ms, platform == 1 ? ByteOrder.BigEndian : ByteOrder.LittleEndian);
        var ind = new IndexFile();

        var tmpPath = Path.GetFileName(path);
        var indexIdStr = tmpPath[..(tmpPath.IndexOf('.'))];
        ind.IndexId = uint.Parse(indexIdStr, NumberStyles.HexNumber);

        try
        {
            ind.VersionInfo = VersionInfo.Read(br);
        }
        catch (Exception)
        {
            ind.VersionInfo = new VersionInfo();
            ind.FileInfo = new FileInfo();
            ind.Collisions32 = new List<Collision32>();
            ind.Collisions64 = new List<Collision64>();
            ind.Hashes32 = new Dictionary<uint, Hash32>();
            ind.Hashes64 = new Dictionary<ulong, Hash64>();
            return ind;
        }

        ind.FileInfo = FileInfo.Read(br);

        if (ind.FileInfo.IndexType == 0)
        {
            ind.Hashes64 = new Dictionary<ulong, Hash64>();
            ind.Collisions64 = new List<Collision64>();

            var hashCount = ind.FileInfo.IndexDataSize / 16;
            var collisionsCount = ind.FileInfo.CollisionSize / 256;

            br.BaseStream.Seek(ind.FileInfo.IndexDataOffset, SeekOrigin.Begin);
            for (var i = 0; i < hashCount; i++)
            {
                var element = Hash64.Read(br);
                if (element.Hash == ulong.MaxValue) continue;
                ind.Hashes64[element.Hash] = element;
            }

            br.BaseStream.Seek(ind.FileInfo.CollisionOffset, SeekOrigin.Begin);
            for (var i = 0; i < collisionsCount; i++)
            {
                var element = Collision64.Read(br);
                if (element.Hash == ulong.MaxValue) break;
                ind.Collisions64.Add(element);
            }
        }
        else if (ind.FileInfo.IndexType == 2)
        {
            ind.Hashes32 = new Dictionary<uint, Hash32>();
            ind.Collisions32 = new List<Collision32>();

            var hashCount = ind.FileInfo.IndexDataSize / 8;
            var collisionsCount = ind.FileInfo.CollisionSize / 256;

            br.BaseStream.Seek(ind.FileInfo.IndexDataOffset, SeekOrigin.Begin);
            for (var i = 0; i < hashCount; i++)
            {
                var element = Hash32.Read(br);
                if (element.Hash == uint.MaxValue) continue;
                ind.Hashes32[element.Hash] = element;
            }

            br.BaseStream.Seek(ind.FileInfo.CollisionOffset, SeekOrigin.Begin);
            for (var i = 0; i < collisionsCount; i++)
            {
                var element = Collision32.Read(br);
                if (element.Hash == uint.MaxValue) break;
                ind.Collisions32.Add(element);
            }
        }
        else
        {
            throw new NotImplementedException("dunno");
        }
        return ind;
    }
}
