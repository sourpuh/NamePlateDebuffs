using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace NamePlateDebuffs;

[Serializable]
public class NamePlateDebuffsPluginConfig : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // General
    public bool Enabled;
    public bool ShowSelfDebuffsOnEnemies;
    public bool ShowDebuffsOnSelf;
    public bool ShowDebuffsOnOthers;
    public bool HidePermanentStatuses;
    public int UpdateIntervalMillis;

    // NodeGroup
    public int MaximumStatuses;
    public int GroupX;
    public int GroupY;
    public int NodeSpacing;
    public float Scale;
    public bool FillFromRight;

    // Node
    public int IconX;
    public int IconY;
    public int IconWidth;
    public int IconHeight;
    public int DurationX;
    public int DurationY;
    public int FontSize;
    public int DurationPadding;
    public Vector4 DurationTextColor;
    public Vector4 DurationEdgeColor;

    public NamePlateDebuffsPluginConfig()
    {
        SetToDefaults();
    }

    public void SetToDefaults()
    {
        // General
        Enabled = true;
        ShowSelfDebuffsOnEnemies = true;
        ShowDebuffsOnSelf = false;
        ShowDebuffsOnOthers = false;
        HidePermanentStatuses = true;
        UpdateIntervalMillis = 100;

        // NodeGroup
        MaximumStatuses = 8;
        GroupX = 27;
        GroupY = 30;
        NodeSpacing = 3;
        Scale = 1;
        FillFromRight = false;

        // Node
        IconX = 0;
        IconY = 0;
        IconWidth = 24;
        IconHeight = 32;
        DurationX = 0;
        DurationY = 23;
        FontSize = 14;
        DurationPadding = 2;
        DurationTextColor = new Vector4(1, 1, 1, 1);
        DurationEdgeColor = new Vector4(0, 0, 0, 1);
    }

    [NonSerialized] private DalamudPluginInterface? _pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}
