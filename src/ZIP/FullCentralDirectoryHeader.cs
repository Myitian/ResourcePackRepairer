namespace ResourcePackRepairer.ZIP;

public struct FullCentralDirectoryHeader
{
    public CentralDirectoryHeader CDH;
    public byte[] FileName;
    public ExtraFieldCollection ExtraFields;
    public byte[] Comment;

    public static FullCentralDirectoryHeader ReadFromStream(Stream stream)
    {
        CentralDirectoryHeader cdh = IDataStruct.ReadExactlyFromStream<CentralDirectoryHeader>(stream);
        return new()
        {
            CDH = cdh,
            FileName = stream.ReadBytes(cdh.FileNameLength),
            ExtraFields = new(stream.ReadBytes(cdh.ExtraFieldLength)),
            Comment = stream.ReadBytes(cdh.CommentLength)
        };
    }
    public void ApplyChanges()
    {
        CDH.FileNameLength = checked((ushort)FileName.Length);
        CDH.CommentLength = checked((ushort)Comment.Length);
        if (!ExtraFields.TryGetLengthInBytes(out CDH.ExtraFieldLength))
            throw new NotSupportedException();
    }
    public void WriteToStream(Stream stream)
    {
        ApplyChanges();
        stream.Write(CentralDirectoryHeader.Signature);
        IDataStruct.WriteToStream(stream, CDH);
        stream.Write(FileName, 0, FileName.Length);
        ExtraFields.WriteToStream(stream);
        stream.Write(Comment, 0, Comment.Length);
    }
}