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
    public bool Enabled = true;
    public bool ShowSelfDebuffsOnEnemies = true;
    public bool ShowDebuffsOnSelf = true;
    public bool ShowDebuffsOnOthers = true;
    public bool HidePermanentStatuses = true;
    public int UpdateIntervalMillis = 100;

    // NodeGroup
    public int MaximumStatuses = 8;
    public int GroupX = 27;
    public int GroupY = 30;
    public int NodeSpacing = 3;
    public float Scale = 1;
    public bool FillFromRight = false;

    // Node
    public int IconX = 0;
    public int IconY = 0;
    public int IconWidth = 24;
    public int IconHeight = 32;
    public int DurationX = 0;
    public int DurationY = 23;
    public int FontSize = 14;
    public int DurationPadding = 2;
    public Vector4 DurationTextColor = new Vector4(1, 1, 1, 1);
    public Vector4 DurationEdgeColor = new Vector4(0, 0, 0, 1);

    public void SetDefaults()
    {
        // General
        Enabled = true;
        ShowSelfDebuffsOnEnemies = true;
        ShowDebuffsOnSelf = true;
        ShowDebuffsOnOthers = true;
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
        DurationTextColor.X = 1;
        DurationTextColor.Y = 1;
        DurationTextColor.Z = 1;
        DurationTextColor.W = 1;
        DurationEdgeColor.X = 0;
        DurationEdgeColor.Y = 0;
        DurationEdgeColor.Z = 0;
        DurationEdgeColor.W = 1;
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
