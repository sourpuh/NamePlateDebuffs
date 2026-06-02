using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace NamePlateDebuffs;

[Serializable]
public class NamePlateDebuffsPluginConfig : IPluginConfiguration
{
    private const int DefaultTimeHideThreshold = 60 * 10;
    public int Version { get; set; } = 1;

    // General
    public bool Enabled = true;
    public bool ShowMoodles;
    public bool HideMoodlesInDuty;
    public bool HideMoodlesInCombat;
    public bool ShowSelfDebuffsOnEnemies;
    public StatusVisibilityConfig DebuffsOnSelf;
    public StatusVisibilityConfig YourBuffsOnSelf;
    public StatusVisibilityConfig AllyBuffsOnSelf;
    public StatusVisibilityConfig SpecialOnSelf;
    public StatusVisibilityConfig DebuffsOnAllies;
    public StatusVisibilityConfig YourBuffsOnAllies;
    public StatusVisibilityConfig AllyBuffsOnAllies;
    public StatusVisibilityConfig SpecialOnAllies;
    public int UpdateIntervalMillis;

    // Obsolete config migration
    [JsonProperty][Obsolete] public bool HidePermanentStatuses { set => DebuffsOnSelf.MaxTimeSeconds = DebuffsOnAllies.MaxTimeSeconds = 3600; }
    [JsonProperty][Obsolete] public bool ShowDebuffsOnSelf { set => DebuffsOnSelf.Enabled = value; }
    [JsonProperty][Obsolete] public bool ShowDebuffsOnOthers { set => DebuffsOnAllies.Enabled = value; }
    [JsonProperty][Obsolete] public int GroupX { set => GroupOffset.X = value; }
    [JsonProperty][Obsolete] public int GroupY { set => GroupOffset.Y = value; }
    [JsonProperty][Obsolete] public int IconX { set => IconOffset.X = value; }
    [JsonProperty][Obsolete] public int IconY { set => IconOffset.Y = value; }
    [JsonProperty][Obsolete] public int IconWidth { set => IconSize.X = value; }
    [JsonProperty][Obsolete] public int IconHeight { set => IconSize.Y = value; }

    // NodeGroup
    public int MaximumStatuses;
    public Vector2 GroupOffset;
    public int NodeSpacing;
    public float Scale;
    public bool FillFromRight;

    // Node
    public Vector4 DurationTextColor;
    public Vector4 DurationEdgeColor;
    public Vector4 SelfDurationTextColor;
    public Vector4 SelfDurationEdgeColor;
    public int FontSize;
    public Vector2 DurationOffset;
    public Vector2 IconOffset;
    public Vector2 IconSize;

    public NamePlateDebuffsPluginConfig()
    {
        SetGeneralToDefaults();
        SetGroupToDefaults();
        SetNodeToDefaults();
    }

    public void SetGeneralToDefaults()
    {
        ShowSelfDebuffsOnEnemies = true;
        HideMoodlesInDuty = true;
        HideMoodlesInCombat = true;
        DebuffsOnSelf = new() { MaxTimeSeconds = DefaultTimeHideThreshold };
        YourBuffsOnSelf = new() { MaxTimeSeconds = DefaultTimeHideThreshold };
        AllyBuffsOnSelf = new() { MaxTimeSeconds = DefaultTimeHideThreshold };
        SpecialOnSelf = new();
        DebuffsOnAllies = new() { MaxTimeSeconds = DefaultTimeHideThreshold };
        YourBuffsOnAllies = new() { MaxTimeSeconds = DefaultTimeHideThreshold };
        AllyBuffsOnAllies = new() { MaxTimeSeconds = DefaultTimeHideThreshold };
        SpecialOnAllies = new();
        UpdateIntervalMillis = 100;
    }

    public void SetGroupToDefaults()
    {
        MaximumStatuses = 8;
        GroupOffset = new(27, 30);
        NodeSpacing = 1;
        Scale = 1;
        FillFromRight = false;
    }

    public void SetNodeToDefaults()
    {
        DurationTextColor = new Vector4(1, 1, 1, 1);
        DurationEdgeColor = new Vector4(0, 0, 0, 1);
        SelfDurationTextColor = ImGui.ColorConvertU32ToFloat4(0xFFE4FFC9);
        SelfDurationEdgeColor = ImGui.ColorConvertU32ToFloat4(0xFF245F0A);
        FontSize = 14;
        DurationOffset = new(-1, -7);
        IconOffset = new();
        IconSize = new(24, 32);

    }

    [NonSerialized] private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}
