using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using NamePlateDebuffs.StatusNode;
using System.Collections.Generic;
using Dalamud.Logging;

namespace NamePlateDebuffs
{
    public class NamePlateDebuffsPlugin : IDalamudPlugin
    {
        public string Name => "NamePlateDebuffs";

        public PluginAddressResolver Address { get; private set; } = null!;
        public StatusNodeManager StatusNodeManager { get; private set; } = null!;
        public static AddonNamePlateHooks Hooks { get; private set; } = null!;
        public NamePlateDebuffsPluginUI UI { get; private set; } = null!;
        public NamePlateDebuffsPluginConfig Config { get; private set; } = null!;

        internal bool InPvp;

        public NamePlateDebuffsPlugin(DalamudPluginInterface pluginInterface)
        {
            Service.Initialize(pluginInterface);

            Config = Service.Interface.GetPluginConfig() as NamePlateDebuffsPluginConfig ?? new NamePlateDebuffsPluginConfig();
            Config.Initialize(Service.Interface);

            Address = new PluginAddressResolver();
            Address.Setup(Service.SigScanner);

            StatusNodeManager = new StatusNodeManager(this);

            Hooks = new AddonNamePlateHooks(this);
            Hooks.Initialize();

            UI = new NamePlateDebuffsPluginUI(this);

            Service.ClientState.TerritoryChanged += OnTerritoryChange;

            Service.CommandManager.AddHandler("/npdebuffs", new CommandInfo(this.ToggleConfig)
            {
                HelpMessage = "Toggles config window."
            });
        }
        public void Dispose()
        {
            Service.ClientState.TerritoryChanged -= OnTerritoryChange;
            Service.CommandManager.RemoveHandler("/npdebuffs");

            UI.Dispose();
            Hooks.Dispose();
            StatusNodeManager.Dispose();
        }

        private void OnTerritoryChange(ushort e)
        {
            try
            {
                TerritoryType territory = Service.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(e);
                if (territory != null) InPvp = territory.IsPvpZone;
            }
            catch (KeyNotFoundException)
            {
                Service.Log.Warning("Could not get territory for current zone");
            }
        }

        private void ToggleConfig(string command, string args)
        {
            UI.ToggleConfig();
        }
    }
}
