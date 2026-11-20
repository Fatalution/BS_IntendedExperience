using IntendedExperience.UI;
using Zenject;

namespace IntendedExperience.Installers;

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<LaunchPreview>().AsSingle();
        Container.BindInterfacesTo<PlayButtonTint>().AsSingle();
        Container.BindInterfacesTo<SettingsMenu>().AsSingle();
        Container.BindInterfacesTo<MapTab>().AsSingle();
    }
}
