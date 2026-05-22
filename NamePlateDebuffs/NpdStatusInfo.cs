using StatusInfo = Lumina.Excel.Sheets.Status;

namespace NamePlateDebuffs;

public readonly record struct NpdStatusInfo
{
    public NpdStatusInfo()
    {
    }

    public static NpdStatusInfo Buff = new() { IconId = 210205, Category = StatusCategory.Beneficial };
    public static NpdStatusInfo Debuff = new() { IconId = 215309, Category = StatusCategory.Detrimental };
    public static NpdStatusInfo Special = new() { IconId = 214758, Category = StatusCategory.Special };

    public uint IconId { get; init; }
    public StatusCategory Category { get; init; }
    public bool IsDispellable { get; init; }
    public bool IsPermanent { get; init; }
    public int Priority { get; init; }

    public static NpdStatusInfo Of(StatusInfo info)
    {
        return new() {
            IconId = info.Icon,
            Category = GetCategory(info),
            IsDispellable = info.CanDispel,
            IsPermanent = info.IsPermanent,
            Priority = info.PartyListPriority,
        };
    }

    private static StatusCategory GetCategory(StatusInfo info)
    {
        return info.CanIncreaseRewards == 1 ? StatusCategory.Special : (info.StatusCategory == 2 ? StatusCategory.Detrimental : StatusCategory.Beneficial);
    }
}
