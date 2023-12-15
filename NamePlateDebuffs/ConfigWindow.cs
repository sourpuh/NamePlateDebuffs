using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace NamePlateDebuffs
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly NamePlateDebuffsPlugin _plugin;

        public ConfigWindow(NamePlateDebuffsPlugin p) : base("Nameplate Debuffs Configuration")
        {
            _plugin = p;

            Size = new Vector2(500, 647);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            bool needSave = false;

            needSave |= ImGui.Checkbox("Enabled", ref _plugin.Config.Enabled);
            ImGui.Text("While config is open, test nodes are displayed to help with configuration.");
            if (ImGui.Button("Reset Config to Defaults"))
            {
                _plugin.Config.SetDefaults();
                needSave = true;
            }
            if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                needSave |= ImGui.InputInt("Update Interval (ms)", ref _plugin.Config.UpdateIntervalMillis, 10);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Interval between status updates in milliseconds");
                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Node Group", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                needSave |= ImGui.Checkbox("Fill From Right", ref _plugin.Config.FillFromRight);
                needSave |= ImGui.SliderInt("X Offset", ref _plugin.Config.GroupX, -200, 200);
                needSave |= ImGui.SliderInt("Y Offset", ref _plugin.Config.GroupY, -200, 200);
                needSave |= ImGui.SliderInt("Node Spacing", ref _plugin.Config.NodeSpacing, -5, 30);
                needSave |= ImGui.SliderFloat("Group Scale", ref _plugin.Config.Scale, 0.01F, 3.0F);
                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Node", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                ImGui.Text("Try and maintain a 3:4 ratio of Icon Width:Icon Height for best results.");
                needSave |= ImGui.SliderInt("Icon X Offset", ref _plugin.Config.IconX, -200, 200);
                needSave |= ImGui.SliderInt("Icon Y Offset", ref _plugin.Config.IconY, -200, 200);
                needSave |= ImGui.SliderInt("Icon Width", ref _plugin.Config.IconWidth, 5, 100);
                needSave |= ImGui.SliderInt("Icon Height", ref _plugin.Config.IconHeight, 5, 100);
                needSave |= ImGui.SliderInt("Duration X Offset", ref _plugin.Config.DurationX, -200, 200);
                needSave |= ImGui.SliderInt("Duration Y Offset", ref _plugin.Config.DurationY, -200, 200);
                needSave |= ImGui.SliderInt("Duration Font Size", ref _plugin.Config.FontSize, 1, 60);
                needSave |= ImGui.SliderInt("Duration Padding", ref _plugin.Config.DurationPadding, -100, 100);

                needSave |= ImGui.ColorEdit4("Duration Text Color", ref _plugin.Config.DurationTextColor);
                needSave |= ImGui.ColorEdit4("Duration Edge Color", ref _plugin.Config.DurationEdgeColor);
                ImGui.Unindent();
            }

            if (needSave)
            {
                _plugin.StatusNodeManager.LoadConfig();
                _plugin.Config.Save();
            }
        }
    }
}
