﻿using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace NamePlateDebuffs.StatusNode;

public unsafe class StatusNodeGroup
{
    private NamePlateDebuffsPlugin _plugin;

    public AtkResNode* RootNode { get; private set; }
    public StatusNode[] StatusNodes { get; private set; }
    public static ushort NodePerGroupCount = 8;
    private int _statusCount = 0;

    public StatusNodeGroup(NamePlateDebuffsPlugin p)
    {
        _plugin = p;

        StatusNodes = new StatusNode[NodePerGroupCount];
        for (int i = 0; i < NodePerGroupCount; i++)
            StatusNodes[i] = new StatusNode(_plugin);
    }

    public bool Built()
    {
        if (RootNode == null) return false;
        foreach (StatusNode node in StatusNodes)
            if (!node.Built()) return false;

        return true;
    }

    public bool BuildNodes(uint baseNodeId)
    {
        if (Built()) return true;

        var rootNode = CreateRootNode();
        if (rootNode == null) return false;
        RootNode = rootNode;
        RootNode->NodeId = baseNodeId;

        for (uint i = 0; i < NodePerGroupCount; i++)
        {
            if (!StatusNodes[i].BuildNodes(baseNodeId + 1 + i * 3))
            {
                DestroyNodes();
                return false;
            }
        }

        RootNode->ChildCount = (ushort)(NodePerGroupCount * 3);
        RootNode->ChildNode = StatusNodes[0].RootNode;
        StatusNodes[0].RootNode->ParentNode = RootNode;

        var lastNode = StatusNodes[0].RootNode;
        for (uint i = 1; i < NodePerGroupCount; i++)
        {
            var currNode = StatusNodes[i].RootNode;
            lastNode->PrevSiblingNode = currNode;
            currNode->NextSiblingNode = lastNode;
            currNode->ParentNode = RootNode;
            lastNode = currNode;
        }

        LoadConfig();
        SetupVisibility();

        return true;
    }

    public void DestroyNodes()
    {
        foreach (StatusNode node in StatusNodes)
        {
            node.DestroyNodes();
        }
        if (RootNode != null)
        {
            RootNode->Destroy(true);
            RootNode = null;
        }
    }

    public void ForEachNode(Action<StatusNode> func)
    {
        foreach (StatusNode node in StatusNodes)
            if (node != null)
                func(node);
    }

    public void LoadConfig()
    {
        RootNode->SetPositionShort((short)_plugin.Config.GroupX, (short)_plugin.Config.GroupY);
        RootNode->SetScale(_plugin.Config.Scale, _plugin.Config.Scale);
        RootNode->SetWidth((ushort)(StatusNodes[0].RootNode->Width * NodePerGroupCount + _plugin.Config.NodeSpacing * (NodePerGroupCount - 1)));
        RootNode->SetHeight(StatusNodes[0].RootNode->Height);

        for (int i = 0; i < NodePerGroupCount; i++)
        {
            if (StatusNodes[i] != null)
            {
                StatusNodes[i].RootNode->SetPositionShort((short)((_plugin.Config.FillFromRight ? 3 - i : i) * (StatusNodes[0].RootNode->Width + _plugin.Config.NodeSpacing)), 0);
            }
        }
    }

    public void SetVisibility(bool enable, bool setChildren)
    {
        _statusCount = 0;
        RootNode->ToggleVisibility(enable);

        if (setChildren)
        {
            ForEachNode(node => node.SetVisibility(enable));
        }
    }

    public bool IsFull()
    {
        return _statusCount >= NodePerGroupCount;
    }

    public void AddStatus(uint id, int timer)
    {
        if (IsFull())
            return;

        StatusNodes[_statusCount].SetStatus(id, timer);
        _statusCount++;
    }

    public void HideUnusedNodes()
    {
        for (int i = NodePerGroupCount - 1; i > _statusCount - 1; i--)
        {
            StatusNodes[i].SetVisibility(false);
        }
    }

    public void SetupVisibility()
    {
        foreach (StatusNode node in StatusNodes)
        {
            node.IconNode->AtkResNode.ToggleVisibility(true);
            node.DurationNode->AtkResNode.ToggleVisibility(true);
            node.RootNode->ToggleVisibility(false);
        }

        RootNode->ToggleVisibility(false);
    }

    private AtkResNode* CreateRootNode()
    {
        var newResNode = (AtkResNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkResNode), 8);
        if (newResNode == null)
        {
            Service.Log.Debug("Failed to allocate memory for res node");
            return null;
        }
        IMemorySpace.Memset(newResNode, 0, (ulong)sizeof(AtkResNode));
        newResNode->Ctor();

        newResNode->Type = NodeType.Res;
        newResNode->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
        newResNode->DrawFlags = 0;

        return newResNode;
    }
}
