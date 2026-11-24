using System.Runtime.CompilerServices;
using IntendedExperience.Compat;
using IntendedExperience.Core;
using IntendedExperience.Patches;
using JetBrains.Annotations;
using Zenject;

namespace IntendedExperience.Installers;

[UsedImplicitly]
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
        Container.Bind<MasterSwitch>().AsSingle();
        Container.Bind<SongCoreColors>().AsSingle();
        Container.BindInterfacesAndSelfTo<BeatSaberPlusLights>().AsSingle();
        Container.Bind<HeckSettingsSetter>().AsSingle();
        Container.Bind<MapExclusions>().AsSingle();
        Container.BindInterfacesAndSelfTo<RankedMaps>().AsSingle();
        Container.Bind<LaunchPlanner>().AsSingle();
        Container.Bind<SettingsSetterPrompt>().AsSingle();
        Container.BindInterfacesTo<LevelStartPatch>().AsSingle();

        if (InstalledMods.Heck)
            BindHeck();
    }

    // Kept separate so Heck's types are only loaded when Heck is installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void BindHeck()
    {
        Container.BindInterfacesTo<HeckSettingsSetterPrompt>().AsSingle();
        Container.BindInterfacesTo<HeckSettingsSetterAutoApply>().AsSingle();
    }
}
