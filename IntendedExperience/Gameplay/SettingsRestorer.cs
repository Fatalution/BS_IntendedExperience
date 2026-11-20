using System;
using IntendedExperience.Settings;
using Zenject;

namespace IntendedExperience.Gameplay;

internal class SettingsRestorer : IInitializable, IDisposable
{
    private readonly PlayerSettingsManager _settingsManager;

    public SettingsRestorer(PlayerSettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public void Initialize()
    {
        Plugin.Log.Debug("SettingsRestorer: Initialized in gameplay scene");
    }

    public void Dispose()
    {
        // This is called when leaving the gameplay scene (back to menu, restart, etc.)
        Plugin.Log.Debug("SettingsRestorer: Disposing, checking for snapshot to restore");

        if (_settingsManager.HasSavedSnapshot)
        {
            try
            {
                _settingsManager.RestoreSnapshot();
                Plugin.Log.Info("SettingsRestorer: Successfully restored settings after gameplay");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"SettingsRestorer: Failed to restore settings: {ex}");
            }
        }
    }
}
