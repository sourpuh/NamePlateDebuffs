using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using System;

using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;
using StatusSheet = Lumina.Excel.GeneratedSheets.Status;

namespace NamePlateDebuffs.StatusNode;

public unsafe class StatusNodeManager : IDisposable
{
    private NamePlateDebuffsPlugin _plugin;

    private AddonNamePlate* namePlateAddon;

    private StatusNodeGroup?[] NodeGroups;

    private ExcelSheet<StatusSheet>? StatusSheet;

    private const int NamePlateCount = 50;
    private const uint StartingNodeId = 50000;

    public bool Built { get; private set; }

    internal StatusNodeManager(NamePlateDebuffsPlugin p)
    {
        _plugin = p;

        NodeGroups = new StatusNodeGroup[NamePlateCount];

        StatusSheet = Service.DataManager.GetExcelSheet<StatusSheet>()!;
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
        foreach (StatusNodeGroup? group in NodeGroups)
            if (group is not null)
                func(group);
    }

    public void ForEachNode(Action<StatusNode> func)
    {
        foreach (StatusNodeGroup? group in NodeGroups)
            group?.ForEachNode(func);
    }
    public void ForEachNodeInGroup(int groupIndex, Action<StatusNode> func)
    {
        NodeGroups[groupIndex]?.ForEachNode(func);
    }
    public void ShowGroup(int groupIndex)
    {
        SetGroupVisibility(groupIndex, true, false);
    }
    public void HideGroup(int groupIndex)
    {
        SetGroupVisibility(groupIndex, false, true);
    }
    private void SetGroupVisibility(int index, bool enable, bool setChildren = false)
    {
        StatusNodeGroup? group = NodeGroups[index];

        group?.SetVisibility(enable, setChildren);
    }

    private bool ShouldIgnoreStatus(NameplateKind kind, StatusSheet info, bool sourceIsLocalPlayer, bool nameplateIsLocalPlayer)
    {
        StatusCategory category = (StatusCategory)info.StatusCategory;
        switch (kind)
        {
            case NameplateKind.Enemy:
                if (_plugin.Config.ShowSelfDebuffsOnEnemies && sourceIsLocalPlayer) return false;
                break;
            case NameplateKind.Player:
                if (_plugin.Config.ShowDebuffsOnSelf && nameplateIsLocalPlayer && category == StatusCategory.Detrimental) return false;
                if (_plugin.Config.ShowDebuffsOnOthers && !nameplateIsLocalPlayer && category == StatusCategory.Detrimental) return false;
                break;
        }
        return true;
    }

    // Return true if status was added or ignored, false if full.
    public bool AddStatus(int groupIndex, NameplateKind kind, Status status, bool sourceIsLocalPlayer, bool nameplateIsLocalPlayer)
    {
        StatusNodeGroup? group = NodeGroups[groupIndex];

        if (group is null) return true;
        if (group.IsFull()) return false;

        StatusSheet? info = StatusSheet?.GetRow(status.StatusId);
        if (info is null) return true;
        if (_plugin.Config.HidePermanentStatuses && info.IsPermanent) return true;
        if (ShouldIgnoreStatus(kind, info, sourceIsLocalPlayer, nameplateIsLocalPlayer)) return true;

        uint iconId = info.Icon;
        // Some statuses have fake stack counts and need to be clamped to safe values.
        // For example, Bloodwhetting has StackCount 144 with MaxStacks 0.
        uint stackCount = Math.Clamp(status.StackCount, (byte)0, info.MaxStacks);
        if (stackCount > 0)
            iconId += stackCount - 1;

        group.AddStatus(iconId, (int)status.RemainingTime);
        return true;
    }

    public void HideUnusedNodes(int groupIndex)
    {
        StatusNodeGroup? group = NodeGroups[groupIndex];
        group?.HideUnusedNodes();
    }

    public void SetDepthPriority(int groupIndex, bool enable)
    {
        StatusNodeGroup? group = NodeGroups[groupIndex];

        if (group is null)
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
        if (StatusNodeGroup.NodePerGroupCount != _plugin.Config.MaximumStatuses)
        {
            StatusNodeGroup.NodePerGroupCount = (ushort)_plugin.Config.MaximumStatuses;
            BuildNodes(/*rebuild=*/true);
        }
        ForEachGroup(group => group.LoadConfig());
        ForEachNode(node => node.LoadConfig());
    }

    public bool BuildNodes(bool rebuild = false)
    {
        if (namePlateAddon == null) return false;
        if (Built && !rebuild) return true;
        if (rebuild) DestroyNodes();

        for (byte i = 0; i < NamePlateCount; i++)
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
            nodeGroup.RootNode->ParentNode = (AtkResNode*)npObj->RootNode;

            npComponent->UldManager.UpdateDrawNodeList();

            NodeGroups[i] = nodeGroup;
        }

        Built = true;

        return true;
    }

    public void DestroyNodes()
    {
        if (namePlateAddon == null) return;

        for (byte i = 0; i < NamePlateCount; i++)
        {
            var npObj = &namePlateAddon->NamePlateObjectArray[i];
            var npComponent = npObj->RootNode->Component;

            if (NodeGroups[i] is not null)
            {
                var lastDefaultNode = NodeGroups[i]!.RootNode->NextSiblingNode;
                lastDefaultNode->PrevSiblingNode = null;
                NodeGroups[i]!.DestroyNodes();
            }
            NodeGroups[i] = null;

            npComponent->UldManager.UpdateDrawNodeList();
        }

        Built = false;
    }
}
