using System.Runtime.CompilerServices;
using IntendedExperience.Compat;
using IntendedExperience.UI;
using IPA.Loader;
using SiraUtil.Extras;
using Zenject;

namespace IntendedExperience.Installers;

internal class MenuInstaller : Installer
{
    private const string HeckPluginId = "Heck";

    public override void InstallBindings()
    {
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
}
