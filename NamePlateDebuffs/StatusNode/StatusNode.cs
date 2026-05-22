using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NamePlateDebuffs.StatusNode;

public unsafe class StatusNode
{
    private NamePlateDebuffsPlugin _plugin;

    public AtkResNode* RootNode { get; private set; }
    public AtkImageNode* IconNode { get; private set; }
    public AtkTextNode* DurationNode { get; private set; }
    public bool Visible { get; private set; }

    private uint CurrentIconId = NpdStatusInfo.Buff.IconId;
    private int PrevSecondsRemaining = 60;

    public StatusNode(NamePlateDebuffsPlugin p)
    {
        _plugin = p;
    }

    public void SetVisibility(bool enable)
    {
        RootNode->ToggleVisibility(enable);
    }

    public void SetStatus(uint iconId, int secondsRemaining, bool sourceIsLocalPlayer)
    {
        SetVisibility(true);

        if (iconId != CurrentIconId)
        {
            IconNode->LoadIconTexture(iconId, 0);
            CurrentIconId = iconId;
        }

        string timeString;
        if (secondsRemaining > 120)
        {
            if (secondsRemaining > 3600)
            {
                secondsRemaining /= 3600;
                timeString = secondsRemaining + "h";
            }
            else
            {
                secondsRemaining /= 60;
                timeString = secondsRemaining + ""; // tiny 'm' unicode
            }
        }
        else
        {
            timeString = "" + secondsRemaining;
        }

        bool isTextVisible = secondsRemaining > 0;
        DurationNode->AtkResNode.ToggleVisibility(isTextVisible);
        if (isTextVisible)
        {
            DurationNode->SetText(timeString);
            if (sourceIsLocalPlayer)
            {
                DurationNode->TextColor = _plugin.Config.SelfDurationTextColor.ToByteColor();
                DurationNode->EdgeColor = _plugin.Config.SelfDurationEdgeColor.ToByteColor();
            }
            else
            {
                DurationNode->TextColor = _plugin.Config.DurationTextColor.ToByteColor();
                DurationNode->EdgeColor = _plugin.Config.DurationEdgeColor.ToByteColor();
            }
        }
    }

    public void LoadConfig()
    {
        if (!Built()) return;

        var iconWidth = (ushort)_plugin.Config.IconSize.X;
        var iconHeight = (ushort)_plugin.Config.IconSize.Y;
        IconNode->AtkResNode.SetPositionShort((short)_plugin.Config.IconOffset.X, (short)_plugin.Config.IconOffset.Y);
        IconNode->AtkResNode.SetWidth(iconWidth);
        IconNode->AtkResNode.SetHeight(iconHeight);

        DurationNode->FontSize = (byte)_plugin.Config.FontSize;
        ushort outWidth = 0;
        ushort outHeight = 0;
        DurationNode->GetTextDrawSize(&outWidth, &outHeight);
        DurationNode->AtkResNode.SetPositionShort((short)_plugin.Config.DurationOffset.X, (short)(iconHeight + _plugin.Config.DurationOffset.Y));
        DurationNode->AtkResNode.SetWidth(iconWidth);
        DurationNode->AtkResNode.SetHeight(outHeight);
        RootNode->SetWidth(iconWidth);
    }

    public bool Built() => RootNode != null && IconNode != null && DurationNode != null;

    public bool BuildNodes(uint baseNodeId)
    {
        if (Built()) return true;

        var rootNode = CreateRootNode();
        if (rootNode == null) return false;
        RootNode = rootNode;

        var iconNode = CreateIconNode();
        if (iconNode == null)
        {
            DestroyNodes();
            return false;
        }
        IconNode = iconNode;

        var durationNode = CreateDurationNode();
        if (durationNode == null)
        {
            DestroyNodes();
            return false;
        }
        DurationNode = durationNode;

        RootNode->NodeId = baseNodeId;
        RootNode->ChildCount = 2;
        RootNode->ChildNode = (AtkResNode*)IconNode;

        IconNode->AtkResNode.NodeId = baseNodeId + 1;
        IconNode->AtkResNode.ParentNode = RootNode;
        IconNode->AtkResNode.PrevSiblingNode = (AtkResNode*)DurationNode;

        DurationNode->AtkResNode.NodeId = baseNodeId + 2;
        DurationNode->AtkResNode.ParentNode = RootNode;
        DurationNode->AtkResNode.NextSiblingNode = (AtkResNode*)IconNode;

        LoadConfig();

        return true;
    }

    public void DestroyNodes()
    {
        if (IconNode != null)
        {
            IconNode->UnloadTexture();
            IMemorySpace.Free(IconNode->PartsList->Parts->UldAsset, (ulong)sizeof(AtkUldAsset));
            IMemorySpace.Free(IconNode->PartsList->Parts, (ulong)sizeof(AtkUldPart));
            IMemorySpace.Free(IconNode->PartsList, (ulong)sizeof(AtkUldPartsList));
            IconNode->PartsList = null;
            IconNode->AtkResNode.Destroy(true);
            IconNode = null;
        }
        if (DurationNode != null)
        {
            DurationNode->AtkResNode.Destroy(true);
            DurationNode = null;
        }
        if (RootNode != null)
        {
            RootNode->Destroy(true);
            RootNode = null;
        }
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

    private AtkImageNode* CreateIconNode()
    {
        var newImageNode = IMemorySpace.GetUISpace()->Create<AtkImageNode>();
        if (newImageNode == null)
        {
            Service.Log.Debug("Failed to allocate memory for image node");
            return null;
        }
        IMemorySpace.Memset(newImageNode, 0, (ulong)sizeof(AtkImageNode));
        newImageNode->Ctor();

        newImageNode->AtkResNode.Type = NodeType.Image;
        newImageNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop | NodeFlags.UseDepthBasedPriority;
        newImageNode->AtkResNode.DrawFlags = 0;

        newImageNode->WrapMode = 1;
        newImageNode->Flags |= ImageNodeFlags.AutoFit;

        var partsList = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPartsList), 8);
        if (partsList == null)
        {
            Service.Log.Debug("Failed to allocate memory for parts list");
            newImageNode->AtkResNode.Destroy(true);
            return null;
        }

        partsList->Id = 0;
        partsList->PartCount = 1;

        var part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart), 8);
        if (part == null)
        {
            Service.Log.Debug("Failed to allocate memory for part");
            IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
            newImageNode->AtkResNode.Destroy(true);
        }

        part->U = 0;
        part->V = 0;
        part->Width = 24;
        part->Height = 32;

        partsList->Parts = part;

        var asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8);
        if (asset == null)
        {
            Service.Log.Debug("Failed to allocate memory for asset");
            IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
            IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
            newImageNode->AtkResNode.Destroy(true);
        }

        asset->Id = 0;
        asset->AtkTexture.Ctor();

        part->UldAsset = asset;

        newImageNode->PartsList = partsList;

        newImageNode->LoadIconTexture(CurrentIconId, 0);

        return newImageNode;
    }

    private AtkTextNode* CreateDurationNode()
    {
        var newTextNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
        if (newTextNode == null)
        {
            Service.Log.Debug("Failed to allocate memory for text node");
            return null;
        }
        IMemorySpace.Memset(newTextNode, 0, (ulong)sizeof(AtkTextNode));
        newTextNode->Ctor();

        newTextNode->AtkResNode.Type = NodeType.Text;
        newTextNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop | NodeFlags.UseDepthBasedPriority;
        newTextNode->AtkResNode.DrawFlags = 12;
        newTextNode->AtkResNode.SetWidth(24);
        newTextNode->AtkResNode.SetHeight(17);

        newTextNode->LineSpacing = 12;
        newTextNode->AlignmentFontType = 4;
        newTextNode->FontSize = 12;
        newTextNode->TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Edge;

        newTextNode->SetNumber(20);

        return newTextNode;
    }
}
