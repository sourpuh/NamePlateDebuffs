using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;

namespace NamePlateDebuffs;

public sealed class MoodlesManager
{
    private const string VersionName = "Moodles.Version";

    private const string GetByPtrName = "Moodles.GetStatusManagerByPtrV2";
    private const int MinimumVersion = 4;

    private readonly ICallGateSubscriber<int> _version;
    private readonly ICallGateSubscriber<nint, string> _getByPtr;

    public MoodlesManager(IDalamudPluginInterface pi)
    {
        _version = pi.GetIpcSubscriber<int>(VersionName);
        _getByPtr = pi.GetIpcSubscriber<nint, string>(GetByPtrName);
    }

    public bool Available
    {
        get
        {
            try { return _version.InvokeFunc() >= MinimumVersion; }
            catch { return false; }
        }
    }

    public List<NpdStatus> GetMoodleStatuses(nint charaPtr, ulong objId)
    {
        var results = new List<NpdStatus>();
        if (charaPtr == 0) return results;

        string data;
        try
        {
            data = _getByPtr.InvokeFunc(charaPtr);
        }
        catch
        {
            return results;
        }
        if (string.IsNullOrEmpty(data)) return results;

        List<MoodlesStatus> statuses;
        try
        {
            statuses = MoodlesStatusReader.Parse(Convert.FromBase64String(data));
        }
        catch
        {
            return results;
        }
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var status in statuses)
        {
            if (status.IconID <= 0) continue;
            results.Add(ToStatus(status, now));
        }
        return results;
    }

    private static NpdStatus ToStatus(MoodlesStatus status, long now)
    {
        uint sourceObjectId = 0;

        var lp = Service.ObjectTable.LocalPlayer;
        var lpName = lp?.Name.ToString() + "@" + lp?.HomeWorld.ValueNullable?.Name.ToString();
        if (lpName == status.Applier)
        {
            sourceObjectId = (uint) lp!.GameObjectId;
        }

        return new()
        {
            Info = new NpdStatusInfo
            {
                IconId = (uint)status.IconID,
                Category = (StatusCategory)(status.Type + 1),
                IsPermanent = status.IsPermanent,
                Priority = int.MinValue,
            },
            Stacks = (uint)Math.Max(1, status.Stacks),
            SecondsRemaining = status.IsPermanent ? 0 : MathF.Max(0, (status.ExpiresAt - now) / 1000f),
            SourceObjectId = sourceObjectId,
        };
    }
}
