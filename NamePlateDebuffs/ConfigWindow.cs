using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace NamePlateDebuffs;

public class ConfigWindow : Window, IDisposable
{
    private readonly NamePlateDebuffsPlugin _plugin;

    public ConfigWindow(NamePlateDebuffsPlugin p) : base("Nameplate Debuffs Configuration")
    {
        _plugin = p;

        Size = new Vector2(500, 800);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        bool needSave = false;

        needSave |= ImGui.Checkbox("Enabled", ref _plugin.Config.Enabled);
        ImGui.Checkbox("Show Test Statuses", ref _plugin.ShowTestStatuses);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("While config is open, test nodes are displayed to help with configuration.");
        if (_plugin.MoodlesManager.Available)
        {
            needSave |= ImGui.Checkbox("Show Moodles", ref _plugin.Config.ShowMoodles);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Show statuses from the Moodles plugin. Positive Moodle statuses show as Ally Buffs.");
        }
        if (ImGui.CollapsingHeader("Status", ImGuiTreeNodeFlags.DefaultOpen))
        {
            using (ImRaii.PushIndent())
            {
                if (ImGui.Button("Reset to Defaults##status"))
                {
                    _plugin.Config.SetGeneralToDefaults();
                    needSave = true;
                }
                if (ImGui.BeginTable("statusconfig", 2))
                {
                    ImGui.TableSetupColumn("##nameplatekind", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("##configs", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Enemy");
                    ImGui.TableNextColumn();
                    needSave |= ImGui.Checkbox("Your Debuffs", ref _plugin.Config.ShowSelfDebuffsOnEnemies);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("You");
                    ImGui.TableNextColumn();
                    DrawStatusVisibilityGroup("self",
                        ref _plugin.Config.DebuffsOnSelf,
                        ref _plugin.Config.YourBuffsOnSelf,
                        ref _plugin.Config.AllyBuffsOnSelf,
                        ref _plugin.Config.SpecialOnSelf,
                        ref needSave);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Allies");
                    ImGui.TableNextColumn();
                    DrawStatusVisibilityGroup("allies",
                        ref _plugin.Config.DebuffsOnAllies,
                        ref _plugin.Config.YourBuffsOnAllies,
                        ref _plugin.Config.AllyBuffsOnAllies,
                        ref _plugin.Config.SpecialOnAllies,
                        ref needSave);

                    ImGui.EndTable();
                }

                ImGui.PushItemWidth(200);
                needSave |= ImGui.InputInt("Update Interval (ms)", ref _plugin.Config.UpdateIntervalMillis, 10);
                ImGui.PopItemWidth();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Interval between status updates in milliseconds");
            }
        }

        if (ImGui.CollapsingHeader("Node Group", ImGuiTreeNodeFlags.DefaultOpen))
        {
            using (ImRaii.PushIndent())
            {
                if (ImGui.Button("Reset to Defaults##group"))
                {
                    _plugin.Config.SetGroupToDefaults();
                    needSave = true;
                }
                ImGui.PushItemWidth(200);
                needSave |= ImGui.SliderInt("Maximum Statuses per group", ref _plugin.Config.MaximumStatuses, 4, 10);
                needSave |= ImGui.Checkbox("Fill From Right", ref _plugin.Config.FillFromRight);
                needSave |= ImGui.DragFloat2("Offset", ref _plugin.Config.GroupOffset, 1, -50, 50, "%.0f");
                needSave |= ImGui.SliderInt("Node Spacing", ref _plugin.Config.NodeSpacing, -5, 30);
                needSave |= ImGui.SliderFloat("Group Scale", ref _plugin.Config.Scale, 0.01F, 3.0F);
                ImGui.PopItemWidth();
            }
        }

        if (ImGui.CollapsingHeader("Node", ImGuiTreeNodeFlags.DefaultOpen))
        {
            using (ImRaii.PushIndent())
            {
                if (ImGui.Button("Reset to Defaults##node"))
                {
                    _plugin.Config.SetNodeToDefaults();
                    needSave = true;
                }
                ImGui.PushItemWidth(200);
                needSave |= ImGui.ColorEdit4("Duration Text Color (other statuses)", ref _plugin.Config.DurationTextColor);
                needSave |= ImGui.ColorEdit4("Duration Edge Color (other statuses)", ref _plugin.Config.DurationEdgeColor);
                needSave |= ImGui.ColorEdit4("Duration Text Color (your statuses)", ref _plugin.Config.SelfDurationTextColor);
                needSave |= ImGui.ColorEdit4("Duration Edge Color (your statuses)", ref _plugin.Config.SelfDurationEdgeColor);
                needSave |= ImGui.SliderInt("Duration Font Size", ref _plugin.Config.FontSize, 1, 60);
                needSave |= ImGui.DragFloat2("Duration Offset", ref _plugin.Config.DurationOffset, 1, -20, 20, "%.0f");

                needSave |= ImGui.DragFloat2("Icon Offset", ref _plugin.Config.IconOffset, 1, -20, 20, "%.0f");
                ImGui.Text("Maintain a 3:4 ratio of Width:Height for best results.");
                needSave |= ImGui.DragFloat2("Icon Size", ref _plugin.Config.IconSize, 1, 5, 40, "%.0f");
                ImGui.PopItemWidth();
            }
        }

        if (needSave)
        {
            _plugin.StatusNodeManager.LoadConfig();
            _plugin.Config.Save();
        }
    }

    private static void DrawStatusVisibilityGroup(string suffix, ref StatusVisibilityConfig debuffs, ref StatusVisibilityConfig yourBuffs, ref StatusVisibilityConfig allyBuffs, ref StatusVisibilityConfig special, ref bool needSave)
    {
        if (!ImGui.BeginTable($"vis_{suffix}", 2, ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("##type", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("##max", ImGuiTableColumnFlags.WidthStretch);

        DrawStatusVisibilityRow($"Debuffs##{suffix}", ref debuffs, ref needSave);
        DrawStatusVisibilityRow($"Your Buffs##{suffix}", ref yourBuffs, ref needSave);
        DrawStatusVisibilityRow($"Ally Buffs##{suffix}", ref allyBuffs, ref needSave);
        DrawStatusVisibilityRow($"Special##{suffix}", ref special, ref needSave);

        ImGui.EndTable();
    }

    private static void DrawStatusVisibilityRow(string label, ref StatusVisibilityConfig vis, ref bool needSave)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        needSave |= ImGui.Checkbox(label, ref vis.Enabled);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(90);
        var minutes = vis.MaxTimeSeconds / 60;
        if (ImGui.InputUInt($"Max time (min)##maxmin_{label}", ref minutes, 5))
        {
            vis.MaxTimeSeconds = minutes * 60;
            needSave = true;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Only show statuses with time remaining less than this; 0 shows all.");
        }
    }
}
