using System;

namespace NamePlateDebuffs;

[Serializable]
public struct StatusVisibilityConfig
{
    public bool Enabled;
    public uint MaxTimeSeconds;
    public bool HidePermanent;
}
