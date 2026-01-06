namespace ResourcePackRepairer;

internal static partial class Program
{
    internal static void Main()
    {
        string nIn = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
        string nOut = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
        using FileStream fIn = File.Open(nIn, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream fOut = File.Open(nOut, FileMode.Create, FileAccess.Write, FileShare.Read);
        ZipHelpers.FixLocalFileHeaders(fIn, fOut);
    }
}