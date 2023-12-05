using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Dalamud.Interface.Components;

namespace NameplateStatus
{
    public class NameplateStatusPluginUI : IDisposable
    {
        private readonly NameplateStatusPlugin _plugin;

        private bool ConfigOpen = false;
        
        public bool IsConfigOpen => ConfigOpen;

        public NameplateStatusPluginUI(NameplateStatusPlugin p)
        {
            _plugin = p;

            Service.Interface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
            Service.Interface.UiBuilder.Draw += UiBuilder_OnBuild;
        }

        public void Dispose()
        {
            Service.Interface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
            Service.Interface.UiBuilder.Draw -= UiBuilder_OnBuild;
        }

        public void ToggleConfig()
        {
            ConfigOpen = !ConfigOpen;
        }

        public void UiBuilder_OnOpenConfigUi() => ConfigOpen = true;

        public void UiBuilder_OnBuild()
        {
            if (!ConfigOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(500, 780), ImGuiCond.Always);

            if (!ImGui.Begin(_plugin.Name, ref ConfigOpen, ImGuiWindowFlags.NoResize))
            {
                ImGui.End();
                return;
            }

            bool needSave = false;
            ImGui.Text("While config is open, test nodes are displayed to help with configuration.");
            if (ImGui.Button("Reset Config to Defaults"))
            {
                _plugin.Config.SetDefaults();
                needSave = true;
            }
            if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                needSave |= ImGui.Checkbox("Enabled", ref _plugin.Config.Enabled);
                needSave |= ImGui.Checkbox("Show your debuffs on enemies", ref _plugin.Config.ShowSelfDebuffsOnEnemies);
                needSave |= ImGui.Checkbox("Show your buffs on you", ref _plugin.Config.ShowSelfBuffsOnSelf);
                needSave |= ImGui.Checkbox("Show your buffs on allies", ref _plugin.Config.ShowSelfBuffsOnAllies);
                needSave |= ImGui.Checkbox("Show debuffs on you", ref _plugin.Config.ShowDebuffsOnSelf);
                needSave |= ImGui.Checkbox("Show debuffs on allies", ref _plugin.Config.ShowDebuffsOnAllies);
                needSave |= ImGui.InputInt("Update Interval (ms)", ref _plugin.Config.UpdateInterval, 10);
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


            ImGui.End();
        }
    }
}
