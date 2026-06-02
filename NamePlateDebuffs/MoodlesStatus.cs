namespace NamePlateDebuffs;

public readonly record struct MoodlesStatus(int IconID, long ExpiresAt, int Type, int Stacks, string Applier)
{
    public bool IsPermanent => ExpiresAt == long.MaxValue;
}
