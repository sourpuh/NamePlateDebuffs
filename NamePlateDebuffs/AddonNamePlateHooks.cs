using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Diagnostics;

namespace NamePlateDebuffs;

public unsafe class AddonNamePlateHooks : IDisposable
{
    const int MaxStatusesPerGameObject = 30;
    private readonly NamePlateDebuffsPlugin _plugin;
    private readonly Stopwatch _lastUpdateTimer;
    private bool _disposed;

    public AddonNamePlateHooks(NamePlateDebuffsPlugin p)
    {
        _plugin = p;

        _lastUpdateTimer = new Stopwatch();
        _lastUpdateTimer.Start();

        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "NamePlate", PreDrawHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "NamePlate", PreFinalizeHandler);
        _disposed = false;
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "NamePlate", PreDrawHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "NamePlate", PreFinalizeHandler);
        _disposed = true;
    }

    public void PreDrawHandler(AddonEvent type, AddonArgs args)
    {
        if (_disposed)
        {
            return;
        }

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
            _plugin.StatusNodeManager.SetNamePlateAddonPointer((AddonNamePlate*)args.Addon);
            if (!_plugin.StatusNodeManager.BuildNodes())
                return;
        }

        uint? localPlayerId = Service.ClientState.LocalPlayer?.ObjectId;
        if (localPlayerId is null)
        {
            _plugin.StatusNodeManager.ForEachGroup(group => group.SetVisibility(false, true));
            return;
        }
        var framework = Framework.Instance();
        var ui3DModule = framework->GetUiModule()->GetUI3DModule();
        var targetObjectInfo = ui3DModule->TargetObjectInfo;

        for (int i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
        {
            var objectInfo = ((UI3DModule.ObjectInfo**)ui3DModule->NamePlateObjectInfoPointerArray)[i];
            var npIndex = objectInfo->NamePlateIndex;
            UpdateNamePlate(objectInfo, objectInfo == targetObjectInfo);
        }
    }

    private void UpdateNamePlate(UI3DModule.ObjectInfo* objectInfo, bool isTarget)
    {
        var npIndex = objectInfo->NamePlateIndex;
        // Disable depth priority for target's nameplate so it shows up in front of walls and other nameplates.
        _plugin.StatusNodeManager.SetDepthPriority(npIndex, !isTarget);

        NameplateKind kind = (NameplateKind)objectInfo->NamePlateObjectKind;
        switch (kind)
        {
            case NameplateKind.Player:
            case NameplateKind.Enemy:
                _plugin.StatusNodeManager.ShowGroup(npIndex);
                break;
            default:
                _plugin.StatusNodeManager.HideGroup(npIndex);
                return;
        }

        if (_plugin.ConfigWindow.IsOpen)
        {
            _plugin.StatusNodeManager.ForEachNodeInGroup(npIndex, node => node.SetStatus(StatusNode.StatusNode.DefaultDebuffId, 1 + npIndex));
            return;
        }

        uint? localPlayerId = Service.ClientState.LocalPlayer?.ObjectId;
        if (localPlayerId is not null)
        {
            bool nameplateIsLocalPlayer = objectInfo->GameObject->ObjectID == localPlayerId;
            StatusManager targetStatus = ((BattleChara*)objectInfo->GameObject)->GetStatusManager[0];

            var statusArray = (Status*)targetStatus.Status;

            for (int j = 0; j < MaxStatusesPerGameObject; j++)
            {
                Status status = statusArray[j];
                if (status.StatusID == 0) continue;

                bool sourceIsLocalPlayer = status.SourceID == localPlayerId;
                if (!_plugin.StatusNodeManager.AddStatus(npIndex, kind, status, sourceIsLocalPlayer, nameplateIsLocalPlayer))
                {
                    break;
                }
            }
        }
        _plugin.StatusNodeManager.HideUnusedNodes(npIndex);
    }

    public void PreFinalizeHandler(AddonEvent type, AddonArgs args)
    {
        _plugin.StatusNodeManager.DestroyNodes();
        _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
    }
}
