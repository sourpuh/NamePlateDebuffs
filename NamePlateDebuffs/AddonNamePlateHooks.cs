using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Diagnostics;

namespace NamePlateDebuffs
{
    public unsafe class AddonNamePlateHooks : IDisposable
    {
        private readonly NamePlateDebuffsPlugin _plugin;

        private delegate void AddonNamePlateFinalizePrototype(AddonNamePlate* thisPtr);
        private Hook<AddonNamePlateFinalizePrototype> _hookAddonNamePlateFinalize;

        private delegate void AddonNamePlateDrawPrototype(AddonNamePlate* thisPtr);
        private Hook<AddonNamePlateDrawPrototype> _hookAddonNamePlateDraw;

        private readonly Stopwatch _lastUpdateTimer;

        public AddonNamePlateHooks(NamePlateDebuffsPlugin p)
        {
            _plugin = p;

            _lastUpdateTimer = new Stopwatch();
            _lastUpdateTimer.Start();

            _hookAddonNamePlateFinalize = Service.Hook.HookFromAddress<AddonNamePlateFinalizePrototype>(_plugin.Address.AddonNamePlateFinalizeAddress, AddonNamePlateFinalizeDetour);
            _hookAddonNamePlateDraw = Service.Hook.HookFromAddress<AddonNamePlateDrawPrototype>(_plugin.Address.AddonNamePlateDrawAddress, AddonNamePlateDrawDetour);

            _hookAddonNamePlateFinalize.Enable();
            _hookAddonNamePlateDraw.Enable();
        }

        public void Dispose()
        {
            _hookAddonNamePlateFinalize.Dispose();
            _hookAddonNamePlateDraw.Dispose();
        }

        public void AddonNamePlateDrawDetour(AddonNamePlate* thisPtr)
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

                _hookAddonNamePlateDraw.Original(thisPtr);
                return;
            }

            if (_lastUpdateTimer.ElapsedMilliseconds >= _plugin.Config.UpdateIntervalMillis)
            {
                _lastUpdateTimer.Restart();

                if (!_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.SetNamePlateAddonPointer(thisPtr);
                    if (!_plugin.StatusNodeManager.BuildNodes())
                        return;
                }

                uint? localPlayerId = Service.ClientState.LocalPlayer?.ObjectId;
                if (localPlayerId is null)
                {
                    _plugin.StatusNodeManager.ForEachGroup(group => group.SetVisibility(false, true));
                    _hookAddonNamePlateDraw.Original(thisPtr);
                    return;
                }

                var framework = Framework.Instance();
                var ui3DModule = framework->GetUiModule()->GetUI3DModule();
                var targetObjectInfo = ui3DModule->TargetObjectInfo;

                for (int i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
                {
                    var objectInfo = ((UI3DModule.ObjectInfo**)ui3DModule->NamePlateObjectInfoPointerArray)[i];
                    bool nameplateIsLocalPlayer = objectInfo->GameObject->ObjectID == localPlayerId;
                    var npIndex = objectInfo->NamePlateIndex;

                    NameplateKind kind = (NameplateKind)objectInfo->NamePlateObjectKind;
                    switch (kind)
                    {
                        case NameplateKind.Player:
                            if (nameplateIsLocalPlayer) break;
                            goto default;
                        case NameplateKind.Enemy:
                            break;
                        default:
                            _plugin.StatusNodeManager.SetGroupVisibility(npIndex, false, true);
                            continue;
                    }

                    // Disable depth priority for target's nameplate so it shows up in front of walls and other nameplates.
                    bool nameplateIsTarget = targetObjectInfo == objectInfo;
                    _plugin.StatusNodeManager.SetDepthPriority(npIndex, !nameplateIsTarget);

                    _plugin.StatusNodeManager.SetGroupVisibility(npIndex, true, false);

                    if (_plugin.ConfigWindow.IsOpen)
                    {
                        _plugin.StatusNodeManager.ForEachNode(node => node.SetStatus(StatusNode.StatusNode.DefaultDebuffId, 20));
                    }
                    else
                    {
                        if (nameplateIsLocalPlayer)
                        {
                            _plugin.StatusNodeManager.HideUnusedStatus(npIndex, 0);
                            continue;
                        }

                        StatusManager targetStatus = ((BattleChara*)objectInfo->GameObject)->GetStatusManager[0];

                        var statusArray = (Status*)targetStatus.Status;

                        int count = 0;

                        for (int j = 0; j < 30; j++)
                        {
                            Status status = statusArray[j];
                            if (status.StatusID == 0) continue;
                            if (status.SourceID != localPlayerId) continue;

                            _plugin.StatusNodeManager.SetStatus(npIndex, count, status.StatusID, (int)status.RemainingTime);
                            count++;

                            if (count == 4)
                                break;
                        }

                        _plugin.StatusNodeManager.HideUnusedStatus(npIndex, count);
                    }
                }
            }

            _hookAddonNamePlateDraw.Original(thisPtr);
        }

        public void AddonNamePlateFinalizeDetour(AddonNamePlate* thisPtr)
        {
            _plugin.StatusNodeManager.DestroyNodes();
            _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
            _hookAddonNamePlateFinalize.Original(thisPtr);
        }
    }
}
