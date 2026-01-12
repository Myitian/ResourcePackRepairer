using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EndOfCentralDirectory : IDataStruct
{
    public static ReadOnlySpan<byte> Signature => "PK\x5\x6"u8;
    public ushort DiskNumber;
    public ushort StartDiskNumber;
    public ushort EntriesOnThisDisk;
    public ushort TotalEntries;
    public uint DirectorySize;
    public uint DirectoryOffset;
    public ushort CommentLength;

    public static EndOfCentralDirectory CreateFromEOCD64(EndOfCentralDirectory64 eocd64, out bool overflowed)
    {
        overflowed = false;
        return new()
        {
            DiskNumber = eocd64.DiskNumber.CreateSaturatingU16(ref overflowed),
            StartDiskNumber = eocd64.StartDiskNumber.CreateSaturatingU16(ref overflowed),
            EntriesOnThisDisk = eocd64.EntriesOnThisDisk.CreateSaturatingU16(ref overflowed),
            TotalEntries = eocd64.TotalEntries.CreateSaturatingU16(ref overflowed),
            DirectorySize = eocd64.DirectorySize.CreateSaturatingU32(ref overflowed),
            DirectoryOffset = eocd64.DirectoryOffset.CreateSaturatingU32(ref overflowed)
        };
    }

    public void ReverseEndianness()
    {
        DiskNumber = BinaryPrimitives.ReverseEndianness(DiskNumber);
        StartDiskNumber = BinaryPrimitives.ReverseEndianness(StartDiskNumber);
        EntriesOnThisDisk = BinaryPrimitives.ReverseEndianness(EntriesOnThisDisk);
        TotalEntries = BinaryPrimitives.ReverseEndianness(TotalEntries);
        DirectorySize = BinaryPrimitives.ReverseEndianness(DirectorySize);
        DirectoryOffset = BinaryPrimitives.ReverseEndianness(DirectoryOffset);
        CommentLength = BinaryPrimitives.ReverseEndianness(CommentLength);
    }
    public override readonly string ToString()
    {
        return $"""
            DiskNumber       : {DiskNumber}
            StartDiskNumber  : {StartDiskNumber}
            EntriesOnThisDisk: {EntriesOnThisDisk}
            TotalEntries     : {TotalEntries}
            DirectorySize    : {DirectorySize}
            DirectoryOffset  : {DirectoryOffset}
            CommentLength    : {CommentLength}
            """;
    }
    public static bool FindFromStream(Stream stream, out EndOfCentralDirectory header, out EndOfCentralDirectory64Locator? locator)
    {
        locator = null;
        stream.Seek(0, SeekOrigin.End);
        while (stream.ReadBackwardsUntilFind4ByteSeq(Signature))
        {
            long pos = stream.Position;
            if (IDataStruct.TryReadFromStream(stream, out header)
                && stream.Position + header.CommentLength <= stream.Length)
            {
                int offset = Unsafe.SizeOf<EndOfCentralDirectory64Locator>() + 2 * sizeof(uint);
                if (pos < offset)
                    return true;

                long posComment = stream.Position;
                stream.Position = pos - offset;
                if (stream.StartsWith(EndOfCentralDirectory64Locator.Signature)
                    && IDataStruct.TryReadFromStream(stream, out EndOfCentralDirectory64Locator loc))
                    locator = loc;
                stream.Position = posComment;
                return true;
            }
        }
        header = default;
        return false;
    }
}