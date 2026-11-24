using IntendedExperience.Installers;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using JetBrains.Annotations;
using SiraUtil.Zenject;
using IpaConfig = IPA.Config.Config;
using IpaLogger = IPA.Logging.Logger;

namespace IntendedExperience;

[UsedImplicitly]
[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;
    internal static string Version { get; private set; } = string.Empty;

    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        // Development builds are "1.0.0-a" in the manifest (valid semver) and shown as "1.0.0a".
        Version = pluginMetadata.HVersion.ToString().Replace("-", string.Empty);
        zenjector.UseLogger(Log);

        var pluginConfig = ipaConfig.Generated<PluginConfig>();
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        zenjector.Install<MenuInstaller>(Location.Menu);

        Log.Info($"{pluginMetadata.Name} {Version} initialized.");
    }
}
