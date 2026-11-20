using IntendedExperience.Gameplay;
using Zenject;

namespace IntendedExperience.Installers;

internal class GameplayInstaller : Installer
{
    public override void InstallBindings()
    {
        // The SettingsRestorer is initialized when gameplay starts
        // and disposed when gameplay ends, which triggers the settings restoration
        Container.BindInterfacesTo<SettingsRestorer>().AsSingle();
    }
}
