using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace HaselDebug.Utils.SqPack;

public enum ByteOrder
{
    LittleEndian,
    BigEndian
}

public class BinaryReader2 : BinaryReader
{
    public ByteOrder ByteOrder { get; } = ByteOrder.LittleEndian;

    public BinaryReader2(Stream input, ByteOrder byteOrder) : base(input)
    {
        ByteOrder = byteOrder;
    }

    public BinaryReader2(Stream input, Encoding encoding) : base(input, encoding) { }
    public BinaryReader2(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

    public override short ReadInt16()
    {
        if (ByteOrder == ByteOrder.LittleEndian)
            return base.ReadInt16();
        return BinaryPrimitives.ReverseEndianness(base.ReadInt16());
    }

    public override int ReadInt32()
    {
        if (ByteOrder == ByteOrder.LittleEndian)
            return base.ReadInt32();
        return BinaryPrimitives.ReverseEndianness(base.ReadInt32());
    }

    public override long ReadInt64()
    {
        if (ByteOrder == ByteOrder.LittleEndian)
            return base.ReadInt64();
        return BinaryPrimitives.ReverseEndianness(base.ReadInt64());
    }

    public override ushort ReadUInt16()
    {
        if (ByteOrder == ByteOrder.LittleEndian)
            return base.ReadUInt16();
        return BinaryPrimitives.ReverseEndianness(base.ReadUInt16());
    }

    public override uint ReadUInt32()
    {
        if (ByteOrder == ByteOrder.LittleEndian)
            return base.ReadUInt32();
        return BinaryPrimitives.ReverseEndianness(base.ReadUInt32());
    }

    public override ulong ReadUInt64()
    {
        if (ByteOrder == ByteOrder.LittleEndian)
            return base.ReadUInt64();
        return BinaryPrimitives.ReverseEndianness(base.ReadUInt64());
    }
}
