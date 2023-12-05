using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using NameplateStatus.StatusNode;
using System;
using System.Diagnostics;
using System.Linq;

namespace NameplateStatus
{
    public unsafe class AddonNamePlateHooks : IDisposable
    {
        const int MAX_STATUSES = 30;
        readonly int[] STATUS_BLACKLIST =
        {
            48, // Food buff
        };

        public enum NameplateKind : byte
        {
            Player = 0,
            FriendlyNPC = 1,
            Enemy = 3,
            PlayerPet = 4,
        }

        private readonly NameplateStatusPlugin _plugin;

        private delegate void AddonNamePlateFinalizePrototype(AddonNamePlate* thisPtr);
        private Hook<AddonNamePlateFinalizePrototype> _hookAddonNamePlateFinalize;

        private delegate void AddonNamePlateDrawPrototype(AddonNamePlate* thisPtr);
        private Hook<AddonNamePlateDrawPrototype> _hookAddonNamePlateDraw;

        private readonly Stopwatch _timer;
        private long _elapsed;

        private UI3DModule.ObjectInfo* _lastTarget;

        public AddonNamePlateHooks(NameplateStatusPlugin p)
        {
            _plugin = p;

            _timer = new Stopwatch();
            _elapsed = 0;
        }

        public void Initialize()
        {
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
                if (_timer.IsRunning)
                {
                    _timer.Stop();
                    _timer.Reset();
                    _elapsed = 0;
                }

                if (_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.DestroyNodes();
                    _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
                }

                _hookAddonNamePlateDraw.Original(thisPtr);
                return;
            }

            _elapsed += _timer.ElapsedMilliseconds;
            _timer.Restart();

            if (_elapsed >= _plugin.Config.UpdateInterval)
            {
                if (!_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.SetNamePlateAddonPointer(thisPtr);
                    if (!_plugin.StatusNodeManager.BuildNodes())
                        return;
                }

                var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var ui3DModule = framework->GetUiModule()->GetUI3DModule();

                for (int i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
                {
                    var objectInfo = ((UI3DModule.ObjectInfo**)ui3DModule->NamePlateObjectInfoPointerArray)[i];

                    // As of at least 6.28 this field has the same value as NamePlateObjectKind: 
                    //var npIndex = objectInfo->NamePlateIndex;

                    // Temporary work around until Dalamud 7.2.0 is pushed to xl, see https://github.com/goatcorp/Dalamud/commit/a173c5dac54fa4b55f5c5066bdcd93cbbcd74ed9
                    //var npIndex = objectInfo->Unk_4F;
                    var npIndex = *(&objectInfo->NamePlateObjectKind + 2); // Same as Unk_4F, but after 7.2.0 the Unk_4F field will be going away
                    NameplateKind kind = (NameplateKind) objectInfo->NamePlateObjectKind;

                    switch (kind)
                    {
                        case NameplateKind.Player:
                        case NameplateKind.Enemy:
                            _plugin.StatusNodeManager.ResetGroupVisibility(npIndex, true, false);
                            break;
                        default:
                            _plugin.StatusNodeManager.ResetGroupVisibility(npIndex, false, true);
                            continue;
                    }

                    if (_plugin.UI.IsConfigOpen)
                    {
                        _plugin.StatusNodeManager.ForEachNode(node => node.SetStatus(StatusNode.StatusNode.DefaultIconId, 20));
                    }
                    else
                    {
                        uint? localPlayerId = Service.ClientState.LocalPlayer?.ObjectId;
                        if (localPlayerId is null)
                        {
                            _plugin.StatusNodeManager.HideUnusedStatus(npIndex);
                            continue;
                        }
                        bool targetIsLocalPlayer = objectInfo->GameObject->ObjectID == localPlayerId;
                        StatusManager targetStatus = ((BattleChara*)objectInfo->GameObject)->GetStatusManager[0];

                        var statusArray = (Status*)targetStatus.Status;

                        for (int j = 0; j < MAX_STATUSES; j++)
                        {
                            Status status = statusArray[j];
                            if (status.StatusID == 0) continue;
                            if (STATUS_BLACKLIST.Contains(status.StatusID)) continue;

                            bool sourceIsLocalPlayer = status.SourceID == localPlayerId;

                            if(!_plugin.StatusNodeManager.AddStatus(npIndex, kind, status, sourceIsLocalPlayer, targetIsLocalPlayer))
                            {
                                break;
                            }
                        }

                        _plugin.StatusNodeManager.HideUnusedStatus(npIndex);
                    }

                    if (objectInfo == ui3DModule->TargetObjectInfo && objectInfo != _lastTarget)
                    {
                        _plugin.StatusNodeManager.SetDepthPriority(npIndex, false);
                        if (_lastTarget != null)
                            _plugin.StatusNodeManager.SetDepthPriority(_lastTarget->NamePlateIndex, true);
                        _lastTarget = objectInfo;
                    }
                }

                _elapsed = 0;
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
