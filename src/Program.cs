using ResourcePackRepairer.PNG;
using ResourcePackRepairer.ZIP;

namespace ResourcePackRepairer;

internal static partial class Program
{
    internal static void Main()
    {
        Console.WriteLine("Mode[0:zip/1:png]");
        ReadOnlySpan<char> mode = Console.ReadLine().AsSpan().Trim();

        Console.WriteLine("Input");
        string nIn = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
        using FileStream fIn = File.Open(nIn, FileMode.Open, FileAccess.Read, FileShare.Read);

        Console.WriteLine("Output");
        string nOut = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
        using FileStream fOut = File.Open(nOut, FileMode.Create, FileAccess.Write, FileShare.Read);

        switch (mode)
        {
            case "0":
                ZIPRepairer.Repair(fIn, fOut);
                break;
            case "1":
                PNGRepairer.Repair(fIn, fOut);
                break;
            default:
                Console.WriteLine("Unknown mode!");
                break;
        }
    }
    internal static ushort CreateSaturatingU16(this uint number, ref bool overflowed)
    {
        if (number >= ushort.MaxValue)
        {
            overflowed = true;
            return ushort.MaxValue;
        }
        return (ushort)number;
    }
    internal static ushort CreateSaturatingU16(this ulong number, ref bool overflowed)
    {
        if (number >= ushort.MaxValue)
        {
            overflowed = true;
            return ushort.MaxValue;
        }
        return (ushort)number;
    }
    internal static uint CreateSaturatingU32(this ulong number, ref bool overflowed)
    {
        if (number >= uint.MaxValue)
        {
            overflowed = true;
            return uint.MaxValue;
        }
        return (uint)number;
    }
}