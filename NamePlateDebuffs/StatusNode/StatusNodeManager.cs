using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace NamePlateDebuffs.StatusNode;

public unsafe class StatusNodeManager : IDisposable
{
    private NamePlateDebuffsPlugin _plugin;

    private AddonNamePlate* namePlateAddon;

    private StatusNodeGroup?[] NodeGroups;

    private const int NamePlateCount = 50;
    private const uint StartingNodeId = 50000;

    public bool Built { get; private set; }

    internal StatusNodeManager(NamePlateDebuffsPlugin p)
    {
        _plugin = p;
        NodeGroups = new StatusNodeGroup[NamePlateCount];
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

    // Return true if status was added or ignored, false if full.
    public bool AddStatus(int groupIndex, NameplateKind kind, NpdStatus status)
    {
        StatusNodeGroup? group = NodeGroups[groupIndex];
        if (group is null || group.IsFull()) return false;
        group.AddStatus(status.IconId, status.RoundedSecondsRemaining, status.SourceIsLocalPlayer);
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
        ForEachNode(node => node.LoadConfig());
        ForEachGroup(group => group.LoadConfig());
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
            var npComponent = npObj->RootComponentNode->Component;

            var lastChild = npComponent->UldManager.RootNode;
            while (lastChild->PrevSiblingNode != null) lastChild = lastChild->PrevSiblingNode;

            lastChild->PrevSiblingNode = nodeGroup.RootNode;
            nodeGroup.RootNode->NextSiblingNode = lastChild;
            nodeGroup.RootNode->ParentNode = (AtkResNode*)npObj->RootComponentNode;

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
            var npComponent = npObj->RootComponentNode->Component;

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
