using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using IntendedExperience.Installers;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace IntendedExperience;

[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;

    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);

        var pluginConfig = ipaConfig.Generated<PluginConfig>();

        // App-wide services (MapAnalyzer, PlayerSettingsManager, SettingsApplier)
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);

        // Menu UI (IntendedPlayButton)
        zenjector.Install<MenuInstaller>(Location.Menu);

        // Gameplay hooks (SettingsRestorer)
        zenjector.Install<GameplayInstaller>(Location.Player);

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
}
