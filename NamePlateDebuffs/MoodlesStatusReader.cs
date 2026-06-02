using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NamePlateDebuffs;

internal static class MoodlesStatusReader
{
    private const int ExpectedMemberCount = 14;

    public static List<MoodlesStatus> Parse(byte[] data)
    {
        var result = new List<MoodlesStatus>();
        using var reader = new BinaryReader(new MemoryStream(data, writable: false));
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            int memberCount = reader.ReadByte();
            if (memberCount != ExpectedMemberCount)
                continue;

            // https://github.com/kawaii/Moodles/blob/main/Moodles/Data/MyStatus.cs
            Skip(reader, 16);                    // 0  GUID
            int iconId = reader.ReadInt32();     // 1  IconID
            SkipString(reader);                  // 2  Title
            SkipString(reader);                  // 3  Description
            SkipString(reader);                  // 4  CustomFXPath
            long expiresAt = reader.ReadInt64(); // 5  ExpiresAt
            int type = reader.ReadInt32();       // 6  Type
            Skip(reader, 4);                     // 7  Modifiers
            int stacks = reader.ReadInt32();     // 8  Stacks
            Skip(reader, 4);                     // 9  StackSteps
            Skip(reader, 16);                    // 10 ChainedStatus
            Skip(reader, 4);                     // 11 ChainTrigger
            string applier = ReadString(reader); // 12 Applier
            SkipString(reader);                  // 13 Dispeller

            result.Add(new MoodlesStatus(iconId, expiresAt, type, stacks, applier));
        }

        return result;
    }

    private static void Skip(BinaryReader reader, int bytes)
        => reader.BaseStream.Seek(bytes, SeekOrigin.Current);

    private static string ReadString(BinaryReader reader)
    {
        int byteLen = StringByteLength(reader);
        return byteLen == 0 ? "" : Encoding.Unicode.GetString(reader.ReadBytes(byteLen));
    }

    private static void SkipString(BinaryReader reader)
        => Skip(reader, StringByteLength(reader));

    // Reads the string header and returns the number of UTF-16 payload bytes that follow.
    private static int StringByteLength(BinaryReader reader)
    {
        int len = reader.ReadInt32();
        if (len <= 0) return 0;
        return len * 2;
    }
}
