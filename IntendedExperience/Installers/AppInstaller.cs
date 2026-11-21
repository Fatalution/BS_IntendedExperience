using IntendedExperience.Compat;
using IntendedExperience.Core;
using IntendedExperience.Patches;
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
        Container.Bind<SongCoreColors>().AsSingle();
        Container.BindInterfacesAndSelfTo<BeatSaberPlusLights>().AsSingle();
        Container.Bind<MapExclusions>().AsSingle();
        Container.Bind<LaunchPlanner>().AsSingle();
        Container.BindInterfacesTo<LevelStartPatch>().AsSingle();
    }
}
