using Memory; // Memory.dll namespace
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

public class ItemDescScanner
{
    public static async Task<List<IntPtr>> MemoryScanner(Mem m, long targetVTable)
    {
        List<IntPtr> foundAddresses = new List<IntPtr>();

        string pattern = GetBytes(targetVTable);

        IEnumerable<long> results = await m.AoBScan(pattern, true, false, "");

        foreach (long addr in results)
        {
            foundAddresses.Add(new IntPtr(addr));
        }

        Debug.WriteLine($"[Debug] AoB scan complete. Total matches found: {foundAddresses.Count}");
        return foundAddresses;
    }
    public static async Task<List<IntPtr>> MemoryScannerString(Mem m, string target)
    {
        List<IntPtr> foundAddresses = new List<IntPtr>();


        IEnumerable<long> results = await m.AoBScan(target, true, false, true);

        foreach (long addr in results)
        {
            foundAddresses.Add(new IntPtr(addr));
        }

        Debug.WriteLine($"[Debug] AoB scan complete. Total matches found: {foundAddresses.Count}");
        return foundAddresses;
    }

    public static string GetBytes(long target)
    {

        byte[] targetBytes = BitConverter.GetBytes(target);
        string pattern = string.Join(" ", Array.ConvertAll(targetBytes, b => b.ToString("X2")));

        return pattern;
    }
}