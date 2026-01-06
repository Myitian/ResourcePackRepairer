using System.Buffers;
using System.IO.Compression;
using System.IO.Hashing;

namespace ResourcePackRepairer;

public static class ZipHelpers
{
    public static void FixLocalFileHeaders(Stream source, Stream destination, bool ignoreDiskNumber = true)
    {
        // Read EOCD from source
        if (!EndOfCentralDirectory.FindFromStream(source, out EndOfCentralDirectory eocd))
            throw new InvalidDataException("Cannot find EOCD!");
        if (ignoreDiskNumber)
        {
            eocd.DiskNumber = 0;
            eocd.StartDiskNumber = 0;
        }
        else if (eocd.DiskNumber != 0 || eocd.StartDiskNumber != 0)
            throw new NotSupportedException("Spanned ZIP is not supported!");
        byte[] comment = source.ReadBytes(eocd.CommentLength);

        source.Position = eocd.DirectoryOffset;
        List<(CentralDirectoryHeader, byte[])> cdhs = [];
        while (source.Position < eocd.DirectoryOffset + eocd.DirectorySize)
        {
            // Read CDH from source
            long pos = source.Position;
            if (!source.StartsWith(CentralDirectoryHeader.Signature))
                throw new InvalidDataException($"Cannot find CDH signature at {pos}!");
            CentralDirectoryHeader cdh = IDataStruct.ReadExactlyFromStream<CentralDirectoryHeader>(source);
            if (ignoreDiskNumber)
                cdh.StartDiskNumber = 0;
            else if (cdh.StartDiskNumber != 0)
                throw new NotSupportedException("Spanned ZIP is not supported!");
            byte[] dynLengthContent = source.ReadBytes(cdh.FileNameLength + cdh.ExtraFieldLength + cdh.CommentLength);

            // Read LFH from source
            pos = source.Position;
            source.Position = cdh.LocalHeaderOffset;
            if (!source.StartsWith(LocalFileHeader.Signature))
                throw new InvalidDataException($"Cannot find LFH signature at {pos}!");
            LocalFileHeader lfh = IDataStruct.ReadExactlyFromStream<LocalFileHeader>(source);
            long payloadStart = source.Position + lfh.FileNameLength + lfh.ExtraFieldLength;

            // Try decompress to get correct CRC32 and length
            source.Position = payloadStart;
            switch (cdh.CompressionMethod)
            {
                case 0:
                    using (LengthLimitedStream lls = new(source, cdh.CompressedSize))
                        CheckStream(lls, out cdh.CRC32, out cdh.UncompressedSize);
                    break;
                case 8:
                    using (LengthLimitedStream lls = new(source, cdh.CompressedSize))
                    using (DeflateStream ds = new(lls, CompressionMode.Decompress))
                        CheckStream(ds, out cdh.CRC32, out cdh.UncompressedSize);
                    break;
            }

            // Save CDH to list
            cdh.LocalHeaderOffset = GetUInt32OrThrow(destination.Length);
            cdhs.Add((cdh, dynLengthContent));

            // Write LFH to destination
            lfh = new(cdh);
            destination.Write(LocalFileHeader.Signature);
            IDataStruct.WriteToStream(destination, lfh);
            destination.Write(dynLengthContent, 0, lfh.FileNameLength + lfh.ExtraFieldLength);

            // Copy compressed data from source to destination
            source.Position = payloadStart;
            source.LengthCopy(destination, lfh.CompressedSize);
            source.Position = pos;
        }
        uint cdhStartPos = GetUInt32OrThrow(destination.Length);
        foreach ((CentralDirectoryHeader cdh, byte[] dynLengthContent) in cdhs)
        {
            // Write CDH to destination
            destination.Write(CentralDirectoryHeader.Signature);
            IDataStruct.WriteToStream(destination, cdh);
            destination.Write(dynLengthContent, 0, dynLengthContent.Length);
        }

        // Write EOCD to destination
        eocd.DirectoryOffset = cdhStartPos;
        eocd.DirectorySize = GetUInt32OrThrow(destination.Length - cdhStartPos);
        destination.Write(EndOfCentralDirectory.Signature);
        IDataStruct.WriteToStream(destination, eocd);
        destination.Write(comment, 0, comment.Length);


        static uint GetUInt32OrThrow(long value)
        {
            return value <= uint.MaxValue ? (uint)value
                : throw new NotSupportedException("Destination offset is too large!");
        }
    }

    public static void CheckStream(Stream stream, out uint crc32, out uint length)
    {
        const int BufferSize = 65536;

        byte[] array = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            Crc32 crc = new();
            ulong length64 = 0;
            while (true)
            {
                int read = stream.Read(array, 0, array.Length);
                if (read <= 0)
                    break;
                crc.Append(array.AsSpan(0, read));
                length64 += (uint)read;
            }
            crc32 = crc.GetCurrentHashAsUInt32();
            length = uint.CreateSaturating(length64);
            // I've never seen a Minecraft resource pack with a file size of 4 GiB, so I'll write it like that for now.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}