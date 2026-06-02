using System;

namespace NamePlateDebuffs;

public readonly record struct NpdStatus
{
    public NpdStatus()
    {
    }

    public NpdStatusInfo Info { get; init; } = NpdStatusInfo.Debuff;
    public float SecondsRemaining { get; init; }
    public uint SourceObjectId { get; init; }
    internal uint Stacks { get; init; } = 1;

    public uint IconId => Info.IconId + Stacks - 1;
    public int RoundedSecondsRemaining => (int)MathF.Abs(MathF.Round(SecondsRemaining));
    public bool SourceIsLocalPlayer => SourceObjectId == Service.ObjectTable.LocalPlayer?.GameObjectId;
}
