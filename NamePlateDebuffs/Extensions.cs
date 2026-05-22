using FFXIVClientStructs.FFXIV.Client.Graphics;
using System.Numerics;

namespace NamePlateDebuffs;

internal static class Extensions
{
    public static ByteColor ToByteColor(this Vector4 v)
    {
        v *= 255f;
        ByteColor c = new()
        {
            R = (byte)v.X,
            G = (byte)v.Y,
            B = (byte)v.Z,
            A = (byte)v.W
        };
        return c;
    }
}
