﻿using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game;

namespace NameplateStatus
{
    public class Service
    {
        [PluginService]
        [RequiredVersion("1.0")]
        public static DalamudPluginInterface Interface { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        public static IClientState ClientState { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
        public static void Initialize(DalamudPluginInterface pluginInterface)
            => pluginInterface.Create<Service>();
    }
}