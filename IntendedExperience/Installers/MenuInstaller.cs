using System.Runtime.CompilerServices;
using IntendedExperience.Compat;
using IntendedExperience.UI;
using IPA.Loader;
using SiraUtil.Extras;
using Zenject;
#if GAME_1_29
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
#endif

namespace IntendedExperience.Installers;

internal class MenuInstaller : Installer
{
    private const string HeckPluginId = "Heck";

    public override void InstallBindings()
    {
#if GAME_1_29
        // BSML 1.6 has no container bindings of its own; its services are singletons.
        BindIfMissing(BSMLParser.instance);
        BindIfMissing(BSMLSettings.instance);
        BindIfMissing(GameplaySetup.instance);
        FlexibleLayoutHandler.Register();
#endif
        Container.BindInterfacesAndSelfTo<LaunchPreview>().AsSingle();
        Container.BindInterfacesTo<PlayButtonTint>().AsSingle();
        Container.BindInterfacesTo<SettingsMenu>().AsSingle();
        Container.BindInterfacesTo<MapTab>().AsSingle();
        Container.BindInterfacesTo<ExcludeButton>().AsSingle();

        if (PluginManager.GetPluginFromId(HeckPluginId) != null)
            BindHeckPlayView();
    }

    // Kept separate so Heck's types are only loaded when Heck is installed.
    // Heck's PlayViewManager collects every IPlayViewController bound in the menu container.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void BindHeckPlayView() =>
        Container.BindInterfacesTo<IntendedChangesView>().FromNewComponentAsViewController().AsSingle();

#if GAME_1_29
    private void BindIfMissing<T>(T instance)
    {
        if (!Container.HasBinding<T>())
            Container.BindInstance(instance);
    }
#endif
}
