using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static FFXIVClientStructs.FFXIV.Client.UI.UI3DModule;
using StatusSheet = Lumina.Excel.Sheets.Status;
using StatusInfo = Lumina.Excel.Sheets.Status;

namespace NamePlateDebuffs;

public unsafe class AddonNamePlateHooks : IDisposable
{
    private ExcelSheet<StatusSheet> StatusSheet;

    private const int MaxTestStatuses = 30 * 3;
    private readonly NamePlateDebuffsPlugin _plugin;
    private readonly Stopwatch _lastUpdateTimer;

    public AddonNamePlateHooks(NamePlateDebuffsPlugin p)
    {
        StatusSheet = Service.DataManager.GetExcelSheet<StatusSheet>()!;

        _plugin = p;

        _lastUpdateTimer = new Stopwatch();
        _lastUpdateTimer.Start();

        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "NamePlate", PreDrawHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "NamePlate", PreFinalizeHandler);
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "NamePlate", PreDrawHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "NamePlate", PreFinalizeHandler);
    }

    public void PreDrawHandler(AddonEvent type, AddonArgs args)
    {
        if (!_plugin.Config.Enabled || _plugin.InPvp)
        {
            if (_lastUpdateTimer.IsRunning)
            {
                _lastUpdateTimer.Stop();
                _lastUpdateTimer.Reset();
            }

            if (_plugin.StatusNodeManager.Built)
            {
                _plugin.StatusNodeManager.DestroyNodes();
                _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
            }

            return;
        }

        _lastUpdateTimer.Start();
        if (_lastUpdateTimer.ElapsedMilliseconds < _plugin.Config.UpdateIntervalMillis)
        {
            return;
        }
        _lastUpdateTimer.Restart();

        if (!_plugin.StatusNodeManager.Built)
        {
            _plugin.StatusNodeManager.SetNamePlateAddonPointer((AddonNamePlate*)args.Addon.Address);
            if (!_plugin.StatusNodeManager.BuildNodes())
                return;
        }

        var localPlayer = Service.ObjectTable.LocalPlayer;
        if (localPlayer is null)
        {
            _plugin.StatusNodeManager.ForEachGroup(group => group.SetVisibility(false, true));
            return;
        }
        var framework = Framework.Instance();
        var ui3DModule = framework->GetUIModule()->GetUI3DModule();
        var targetIndex = -1;
        if (ui3DModule->TargetObjectInfo != null)
        {
            targetIndex = ui3DModule->TargetObjectInfo->NamePlateIndex;
        }

        for (int i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
        {
            var objectInfo = ui3DModule->NamePlateObjectInfoPointers[i].Value;
            var npIndex = objectInfo->NamePlateIndex;
            UpdateNamePlate(objectInfo, targetIndex == npIndex);
        }
    }

    private void UpdateNamePlate(ObjectInfo* objectInfo, bool isTarget)
    {
        var npIndex = objectInfo->NamePlateIndex;
        // Disable depth priority for target's nameplate so it shows up in front of walls and other nameplates.
        _plugin.StatusNodeManager.SetDepthPriority(npIndex, !isTarget);

        NameplateKind kind = (NameplateKind)objectInfo->NamePlateObjectKind;
        bool nameplateIsLocalPlayer = objectInfo->GameObject->GetGameObjectId() == Service.ObjectTable.LocalPlayer?.GameObjectId;
        if (nameplateIsLocalPlayer)
        {
            kind = NameplateKind.LocalPlayer;
        }
        switch (kind)
        {
            case NameplateKind.LocalPlayer:
            case NameplateKind.Player:
            case NameplateKind.Enemy:
                _plugin.StatusNodeManager.ShowGroup(npIndex);
                break;
            default:
                _plugin.StatusNodeManager.HideGroup(npIndex);
                return;
        }

        foreach (var status in GetStatuses(objectInfo))
        {
            if (!ShouldShowStatus(kind, status))
            {
                continue;
            }

            if (!_plugin.StatusNodeManager.AddStatus(npIndex, kind, status))
            {
                break;
            }
        }
        _plugin.StatusNodeManager.HideUnusedNodes(npIndex);
    }

    private bool ShouldShowStatus(NameplateKind kind, NpdStatus status)
    {
        if (kind == NameplateKind.Enemy)
        {
            return _plugin.Config.ShowSelfDebuffsOnEnemies && status.SourceIsLocalPlayer && status.Info.Category is StatusCategory.Detrimental;
        }

        var visConfig = GetVisConfig(kind, status);
        if (!visConfig.Enabled) return false;
        if (visConfig.MaxTimeSeconds > 0 && status.SecondsRemaining > visConfig.MaxTimeSeconds) return false;
        if (visConfig.HidePermanent && status.Info.IsPermanent) return false;

        return true;
    }

    private StatusVisibilityConfig GetVisConfig(NameplateKind kind, NpdStatus status)
    {
        var cfg = _plugin.Config;
        bool srcSelf = status.SourceIsLocalPlayer;
        return (kind, status.Info.Category) switch
        {
            (NameplateKind.LocalPlayer, StatusCategory.Detrimental) => cfg.DebuffsOnSelf,
            (NameplateKind.LocalPlayer, StatusCategory.Beneficial)  => srcSelf ? cfg.YourBuffsOnSelf  : cfg.AllyBuffsOnSelf,
            (NameplateKind.LocalPlayer, StatusCategory.Special)     => cfg.SpecialOnSelf,
            (NameplateKind.Player,      StatusCategory.Detrimental) => cfg.DebuffsOnAllies,
            (NameplateKind.Player,      StatusCategory.Beneficial)  => srcSelf ? cfg.YourBuffsOnAllies : cfg.AllyBuffsOnAllies,
            (NameplateKind.Player,      StatusCategory.Special)     => cfg.SpecialOnAllies,
            _ => default,
        };
    }

    private IEnumerable<NpdStatus> GetStatuses(ObjectInfo* objectInfo)
    {
        var npIndex = objectInfo->NamePlateIndex;
        if (_plugin.ConfigWindow.IsOpen && _plugin.ShowTestStatuses)
        {
            return GetTestStatuses(npIndex);
        }
        var statuses = GetRealStatuses(objectInfo);
        if (ShowMoodles())
        {
            statuses.AddRange(GetMoodleStatuses(objectInfo));
        }
        if (!_plugin.Config.FillFromRight)
        {
            statuses.Reverse();
        }
        statuses.Sort((x, y) => x.Info.Priority.CompareTo(y.Info.Priority));

        return statuses;
    }

    private IEnumerable<NpdStatus> GetTestStatuses(int npIndex)
    {
        var localPlayerId = (uint)(Service.ObjectTable.LocalPlayer?.GameObjectId ?? 0);
        for (int i = 0; i < MaxTestStatuses; i++)
        {
            var sourceObjectId = i % 2 == 0 ? 0 : localPlayerId;
            yield return new()
            {
                Info = (i % 3) switch
                {
                    0 => NpdStatusInfo.Debuff,
                    1 => NpdStatusInfo.Buff,
                    _ => NpdStatusInfo.Special,
                },
                SecondsRemaining = npIndex + 1,
                SourceObjectId = sourceObjectId,
            };
        }
    }

    private List<NpdStatus> GetRealStatuses(ObjectInfo* objectInfo)
    {
        List<NpdStatus> statuses = new();
        StatusManager* targetStatus = ((BattleChara*)objectInfo->GameObject)->GetStatusManager();
        var statusArray = targetStatus->Status;

        for (int j = 0; j < statusArray.Length; j++)
        {
            Status status = statusArray[j];
            if (status.StatusId == 0) continue;
            StatusInfo info = StatusSheet.GetRow(status.StatusId);
            if (info.Icon == 0) continue;
            statuses.Add(new()
            {
                Info = NpdStatusInfo.Of(info),
                SecondsRemaining = info.IsPermanent ? 0 : status.RemainingTime,
                SourceObjectId = status.SourceObject.ObjectId,
                Stacks = info.MaxStacks > 0 ? (uint)(0xFF & status.Param) : 1,
            });
        }
        return statuses;
    }

    private IEnumerable<NpdStatus> GetMoodleStatuses(ObjectInfo* objectInfo)
    {
        return _plugin.MoodlesManager.GetMoodleStatuses((nint)objectInfo->GameObject, objectInfo->GameObject->GetGameObjectId());
    }

    private bool ShowMoodles()
    {
        var cfg = _plugin.Config;
        if (!_plugin.Config.ShowMoodles)
            return false;
        if (cfg.HideMoodlesInDuty
            && (Service.Condition[ConditionFlag.BoundByDuty]
                || Service.Condition[ConditionFlag.BoundByDuty56]
                || Service.ClientState.IsPvP))
            return false;
        if (cfg.HideMoodlesInCombat && Service.Condition[ConditionFlag.InCombat])
            return false;
        return true;
    }

    public void PreFinalizeHandler(AddonEvent type, AddonArgs args)
    {
        _plugin.StatusNodeManager.DestroyNodes();
        _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
    }
}
