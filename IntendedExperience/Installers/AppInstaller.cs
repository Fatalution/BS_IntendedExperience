using System.Runtime.CompilerServices;
using IntendedExperience.Compat;
using IntendedExperience.Core;
using IntendedExperience.Patches;
using IPA.Loader;
using Zenject;

namespace IntendedExperience.Installers;

internal class AppInstaller : Installer
{
    private const string HeckPluginId = "Heck";

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
        Container.Bind<HeckSettingsSetter>().AsSingle();
        Container.Bind<MapExclusions>().AsSingle();
        Container.Bind<LaunchPlanner>().AsSingle();
        Container.Bind<SettingsSetterPrompt>().AsSingle();
        Container.BindInterfacesTo<LevelStartPatch>().AsSingle();

        if (PluginManager.GetPluginFromId(HeckPluginId) != null)
            BindHeckPrompt();
    }

    // Kept separate so Heck's types are only loaded when Heck is installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void BindHeckPrompt()
    {
        Container.BindInterfacesAndSelfTo<HeckSettingsSetterPrompt>().AsSingle();
        Container.BindInterfacesTo<HeckSettingsSetterAutoApply>().AsSingle();
    }
}
