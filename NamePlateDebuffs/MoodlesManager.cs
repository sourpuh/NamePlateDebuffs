using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

using MoodleKey = (ulong ObjId, System.Guid Guid);
using MoodlesStatusInfo = (
    int Version,
    System.Guid Guid,
    int IconId,
    string Title,
    string Description,
    string CustomVFXPath,
    long ExpireTicks,
    int Type,
    int Stacks,
    int StackSteps,
    uint Modifiers,
    System.Guid ChainedStatus,
    int ChainTrigger,
    string Applier,
    string Dispeller,
    bool Permanent
);

namespace NamePlateDebuffs;

public sealed class MoodlesManager
{
    private readonly record struct Snapshot(long FirstSeenAt, long DurationMs);

    private const string VersionName = "Moodles.Version";
    private const string GetByPtrName = "Moodles.GetStatusManagerInfoByPtrV2";
    private const int MinimumVersion = 4;

    private readonly ICallGateSubscriber<int> _version;
    private readonly ICallGateSubscriber<nint, object> _getByPtr;

    private readonly Dictionary<MoodleKey, Snapshot> _snapshots = new();
    private readonly HashSet<MoodleKey> _staleKeys = new();

    // Moodles expire time float to int uses ceiling() while the game UI uses round().
    // Half a second bias to undo the ceiling effect.
    private const long ExpireBiasMs = 500;

    public MoodlesManager(IDalamudPluginInterface pi)
    {
        _version = pi.GetIpcSubscriber<int>(VersionName);
        _getByPtr = pi.GetIpcSubscriber<nint, object>(GetByPtrName);
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

        IList? list;
        try { list = _getByPtr.InvokeFunc(charaPtr) as IList; }
        catch { return results; }
        if (list is null) return results;

        _staleKeys.Clear();
        foreach (var key in _snapshots.Keys)
            if (key.ObjId == objId) _staleKeys.Add(key);

        foreach (var item in list)
        {
            if (item is not JObject jo) continue;
            if (!TryParse(jo, out var entry)) continue;
            _staleKeys.Remove((objId, entry.Guid));
            results.Add(ToStatus(entry, RemainingSeconds(objId, entry)));
        }

        foreach (var key in _staleKeys) _snapshots.Remove(key);
        return results;
    }

    public static NpdStatus ToStatus(MoodleEntry entry, float secondsRemaining)
    {
        return new()
        {
            Info = new NpdStatusInfo
            {
                IconId = entry.BaseIconId,
                Category = entry.Category,
                IsPermanent = entry.IsPermanent,
                Priority = int.MinValue,
            },
            Stacks = (uint)entry.Stacks,
            SecondsRemaining = secondsRemaining,
        };
    }

    private float RemainingSeconds(ulong objId, MoodleEntry entry)
    {
        if (entry.IsPermanent) return -1;

        var key = (objId, entry.Guid);
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (_snapshots.TryGetValue(key, out var snapshot))
        {
            long endAt = snapshot.FirstSeenAt + snapshot.DurationMs;
            if (endAt > now) return (endAt - now + ExpireBiasMs) / 1000f;
        }

        // Snapshot expired but the moodle is still in the IPC list.
        // Moodles refreshed it without us seeing a gap. Restart the timer.
        _snapshots[key] = new Snapshot(now, entry.ExpireTicksMs);
        return (entry.ExpireTicksMs + ExpireBiasMs) / 1000f;
    }

    private static bool TryParse(JObject jo, out MoodleEntry entry)
    {
        entry = default;
        try
        {
            var t = jo.ToObject<MoodlesStatusInfo>();
            if (t.IconId <= 0) return false;
            entry = new MoodleEntry(t.Guid, (uint)t.IconId, Math.Max(1, t.Stacks), t.Type, t.ExpireTicks);
            return true;
        }
        catch { return false; }
    }
}

public readonly record struct MoodleEntry(Guid Guid, uint BaseIconId, int Stacks, int StatusType, long ExpireTicksMs)
{
    public StatusCategory Category => (StatusCategory)(StatusType + 1);
    public bool IsPermanent => ExpireTicksMs == -1;
}
