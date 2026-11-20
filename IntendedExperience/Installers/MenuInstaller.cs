using IntendedExperience.UI;
using Zenject;

namespace IntendedExperience.Installers;

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        // IntendedPlayButton handles the UI for playing with intended settings
        Container.BindInterfacesTo<IntendedPlayButton>().AsSingle();
    }
}
