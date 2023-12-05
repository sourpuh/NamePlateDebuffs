using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Havok;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Security.Principal;
using static NamePlateDebuffs.AddonNamePlateHooks;

using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;
using StatusInfo = Lumina.Excel.GeneratedSheets.Status;

namespace NamePlateDebuffs.StatusNode
{
    public unsafe class StatusNodeManager : IDisposable
    {
        private NamePlateDebuffsPlugin _plugin;

        private AddonNamePlate* namePlateAddon;

        private StatusNodeGroup[] NodeGroups;

        private ExcelSheet<StatusInfo>? StatusSheet;

        private const int NamePlateCount = 50;
        private const uint StartingNodeId = 50000;

        public bool Built { get; private set; }

        internal StatusNodeManager(NamePlateDebuffsPlugin p)
        {
            _plugin = p; 

            NodeGroups = new StatusNodeGroup[NamePlateCount];

            StatusSheet = Service.DataManager.GetExcelSheet<StatusInfo>();
        }

        public void Dispose()
        {
            DestroyNodes();
        }

        public void SetNamePlateAddonPointer(AddonNamePlate* addon)
        {
            namePlateAddon = addon;
        }

        public void ForEachGroup(Action<StatusNodeGroup> func)
        {
            foreach(StatusNodeGroup group in NodeGroups)
                if (group != null)
                    func(group);
        }

        public void ForEachNode(Action<StatusNode> func)
        {
            foreach (StatusNodeGroup group in NodeGroups)
                group?.ForEachNode(func);
        }

        public void ResetGroupVisibility(int index, bool enable, bool setChildren = false)
        {
            StatusNodeGroup group = NodeGroups[index];

            group?.ResetVisibility(enable, setChildren);
        }

        // Return true if status was added or ignored, false if full.
        public bool AddStatus(int groupIndex, NameplateKind kind, Status status, bool sourceIsLocalPlayer, bool targetIsLocalPlayer)
        {
            StatusNodeGroup group = NodeGroups[groupIndex];

            if (group.IsFull()) return false;

            StatusInfo info = StatusSheet?.GetRow(status.StatusID);
            if (info == null) return true;
            if (_plugin.Config.HidePermanentBuffs && info.IsPermanent) return true;

            StatusCategory category = (StatusCategory) info.StatusCategory;
            switch (kind)
            {
                case NameplateKind.Enemy:
                    if (_plugin.Config.ShowSelfDebuffsOnEnemies && sourceIsLocalPlayer) break;
                    return true;
                case NameplateKind.Player:
                    if (_plugin.Config.ShowSelfBuffsOnSelf && targetIsLocalPlayer && sourceIsLocalPlayer && category == StatusCategory.Beneficial) break;
                    if (_plugin.Config.ShowSelfBuffsOnAllies && !targetIsLocalPlayer && sourceIsLocalPlayer && category == StatusCategory.Beneficial) break;
                    if (_plugin.Config.ShowDebuffsOnSelf && targetIsLocalPlayer && category == StatusCategory.Detrimental) break;
                    if (_plugin.Config.ShowDebuffsOnAllies && !targetIsLocalPlayer && category == StatusCategory.Detrimental) break;
                    return true;
                default: return true;
            }

            int iconId = (int) info.Icon;
            // Some statuses have fake stack counts and need to be clamped to safe values.
            // For example, Bloodwhetting has StackCount 144 with MaxStacks 0.
            int stackCount = Math.Clamp(status.StackCount, (byte)0, info.MaxStacks);
            if (stackCount > 0)
                iconId += stackCount - 1;

            group.AddStatus(iconId, (int) status.RemainingTime);
            return true;
        }

        public void HideUnusedStatus(int groupIndex)
        {
            StatusNodeGroup group = NodeGroups[groupIndex];
            group?.HideUnusedStatus();
        }

        public void SetDepthPriority(int groupIndex, bool enable)
        {
            StatusNodeGroup group = NodeGroups[groupIndex];

            if (group == null)
                return;

            group.RootNode->SetUseDepthBasedPriority(enable);

            group.ForEachNode(node =>
            {
                node.RootNode->SetUseDepthBasedPriority(enable);
                node.DurationNode->AtkResNode.SetUseDepthBasedPriority(enable);
                node.IconNode->AtkResNode.SetUseDepthBasedPriority(enable);
            });
        }

        public void LoadConfig()
        {
            ForEachNode(node => node.LoadConfig());
            ForEachGroup(group => group.LoadConfig());
        }

        public bool BuildNodes(bool rebuild = false)
        {
            if (namePlateAddon == null) return false;
            if (Built && !rebuild) return true;
            if (rebuild) DestroyNodes();
 
            for(byte i = 0; i < NamePlateCount; i++)
            {
                StatusNodeGroup nodeGroup = new StatusNodeGroup(_plugin);
                var npObj = &namePlateAddon->NamePlateObjectArray[i];
                if (!nodeGroup.BuildNodes(StartingNodeId))
                {
                    DestroyNodes();
                    return false;
                }
                var npComponent = npObj->RootNode->Component;

                var lastChild = npComponent->UldManager.RootNode;
                while (lastChild->PrevSiblingNode != null) lastChild = lastChild->PrevSiblingNode;

                lastChild->PrevSiblingNode = nodeGroup.RootNode;
                nodeGroup.RootNode->NextSiblingNode = lastChild;
                nodeGroup.RootNode->ParentNode = (AtkResNode*) npObj->RootNode;

                npComponent->UldManager.UpdateDrawNodeList();

                NodeGroups[i] = nodeGroup;
            }

            Built = true;

            return true;
        }

        public void DestroyNodes()
        {
            if (namePlateAddon == null) return;

            for(byte i = 0; i < NamePlateCount; i++)
            {
                var npObj = &namePlateAddon->NamePlateObjectArray[i];
                var npComponent = npObj->RootNode->Component;

                if (NodeGroups[i] != null)
                {
                    var lastDefaultNode = NodeGroups[i].RootNode->NextSiblingNode;
                    lastDefaultNode->PrevSiblingNode = null;
                    NodeGroups[i].DestroyNodes();
                }
                NodeGroups[i] = null;

                npComponent->UldManager.UpdateDrawNodeList();
            }

            Built = false;
        }
    }
}
