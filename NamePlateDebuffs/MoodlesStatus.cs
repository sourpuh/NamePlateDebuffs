using MemoryPack;
using System;

namespace NamePlateDebuffs;

[MemoryPackable]
public partial class MoodlesStatus
{
    [MemoryPackOrder(0)] public Guid GUID;
    [MemoryPackOrder(1)] public int IconID;
    [MemoryPackOrder(2)] public string Title = "";
    [MemoryPackOrder(3)] public string Description = "";
    [MemoryPackOrder(4)] public string CustomFXPath = "";
    [MemoryPackOrder(5)] public long ExpiresAt;
    [MemoryPackOrder(6)] public int Type;
    [MemoryPackOrder(7)] public uint Modifiers;
    [MemoryPackOrder(8)] public int Stacks = 1;
    [MemoryPackOrder(9)] public int StackSteps;
    [MemoryPackOrder(10)] public Guid ChainedStatus;
    [MemoryPackOrder(11)] public int ChainTrigger;
    [MemoryPackOrder(12)] public string Applier = "";
    [MemoryPackOrder(13)] public string Dispeller = "";

    [MemoryPackIgnore] public bool IsPermanent => ExpiresAt == long.MaxValue;
}
