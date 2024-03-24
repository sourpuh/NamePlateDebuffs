using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using NamePlateDebuffs.StatusNode;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;

namespace NamePlateDebuffs;

public class NamePlateDebuffsPlugin : IDalamudPlugin
{
    public string Name => "NamePlateDebuffs";
    private const string CommandName = "/npdebuffs";
    public PluginAddressResolver Address { get; private set; } = null!;
    public StatusNodeManager StatusNodeManager { get; private set; } = null!;
    public static AddonNamePlateHooks Hooks { get; private set; } = null!;
    public WindowSystem WindowSystem = new("NameplateDebuffs");
    public ConfigWindow ConfigWindow { get; init; }
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

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUI;

        Service.ClientState.TerritoryChanged += OnTerritoryChange;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Toggles configuration window\n" +
            CommandName + " toggle → Toggle Nameplate Debuffs\n" +
            CommandName + " enable → Enables Nameplate Debuffs\n" +
            CommandName + " disable → Disables Nameplate Debuffs"
        });
    }

    private void OnCommand(string command, string args)
    {
        switch (args.TrimEnd().ToLower())
        {
            case "":
                ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
                return;
            case "toggle":
                Config.Enabled = !Config.Enabled;
                return;
            case "enable":
                Config.Enabled = true;
                return;
            case "disable":
                Config.Enabled = false;
                return;
        }
    }

    public void Dispose()
    {
        Service.ClientState.TerritoryChanged -= OnTerritoryChange;
        Service.CommandManager.RemoveHandler(CommandName);

        this.WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        Hooks.Dispose();
        StatusNodeManager.Dispose();
    }

    private void OnTerritoryChange(ushort e)
    {
        try
        {
            TerritoryType? territory = Service.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(e);
            if (territory is not null) InPvp = territory.IsPvpZone;
        }
        catch (KeyNotFoundException)
        {
            Service.Log.Warning("Could not get territory for current zone");
        }
    }
    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void OpenConfigUI()
    {
        ConfigWindow.IsOpen = true;
    }
}
