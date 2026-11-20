using IntendedExperience.Detection;
using IntendedExperience.Settings;
using Zenject;

namespace IntendedExperience.Installers;

internal class AppInstaller : Installer
{
    private readonly PluginConfig pluginConfig;

    public AppInstaller(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(pluginConfig).AsSingle();

        // Core services - available app-wide
        Container.Bind<MapAnalyzer>().AsSingle();
        Container.Bind<PlayerSettingsManager>().AsSingle();
        Container.Bind<SettingsApplier>().AsSingle();
    }
}
