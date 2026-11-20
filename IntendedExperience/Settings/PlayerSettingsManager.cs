using System;
using System.Reflection;
using IntendedExperience.Compat;

namespace IntendedExperience.Settings;

internal class PlayerSettingsManager
{
    private readonly PlayerDataModel _playerDataModel;
    private SettingsSnapshot? _savedSnapshot;

    public PlayerSettingsManager(PlayerDataModel playerDataModel)
    {
        _playerDataModel = playerDataModel;
    }

    public bool HasSavedSnapshot => _savedSnapshot != null;

    public SettingsSnapshot CreateSnapshot()
    {
        var playerData = _playerDataModel.playerData;
        var playerSettings = playerData.playerSpecificSettings;
        var overrideSettings = playerData.overrideEnvironmentSettings;

        var snapshot = new SettingsSnapshot
        {
            EffectsFilter = playerSettings.environmentEffectsFilterDefaultPreset,
            OverrideEnvironments = overrideSettings.overrideEnvironments,
            OverrideDefaultColors = playerData.colorSchemesSettings.overrideDefaultColors,
            ChromaEnabled = ChromaCompat.IsEnabled(),
            BSPlusBrightness = BSPlusCompat.GetBrightness()
        };

        _savedSnapshot = snapshot;

        Plugin.Log.Debug($"Created settings snapshot: Effects={snapshot.EffectsFilter}, " +
                         $"OverrideEnv={snapshot.OverrideEnvironments}, " +
                         $"OverrideColors={snapshot.OverrideDefaultColors}, " +
                         $"Chroma={snapshot.ChromaEnabled}");

        return snapshot;
    }

    public void RestoreSnapshot()
    {
        if (_savedSnapshot == null)
        {
            Plugin.Log.Warn("No snapshot to restore");
            return;
        }

        var snapshot = _savedSnapshot;
        _savedSnapshot = null;

        SetEffectsFilter(snapshot.EffectsFilter);
        SetOverrideEnvironments(snapshot.OverrideEnvironments);
        SetOverrideDefaultColors(snapshot.OverrideDefaultColors);

        if (snapshot.ChromaEnabled != ChromaCompat.IsEnabled())
        {
            ChromaCompat.SetEnabled(snapshot.ChromaEnabled);
        }

        if (snapshot.BSPlusBrightness.HasValue)
        {
            BSPlusCompat.SetBrightness(snapshot.BSPlusBrightness.Value);
        }

        Plugin.Log.Debug("Restored settings from snapshot");
    }

    public void ClearSnapshot()
    {
        _savedSnapshot = null;
    }

    public void SetEffectsFilter(EnvironmentEffectsFilterPreset preset)
    {
        try
        {
            // Use reflection to modify the settings since CopyWith may not exist
            var playerData = _playerDataModel.playerData;
            var playerSettings = playerData.playerSpecificSettings;

            // Try to set via reflection
            SetPropertyValue(playerSettings, "environmentEffectsFilterDefaultPreset", preset);
            SetPropertyValue(playerSettings, "environmentEffectsFilterExpertPlusPreset", preset);

            Plugin.Log.Debug($"Set effects filter to: {preset}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Failed to set effects filter: {ex.Message}");
        }
    }

    public void SetOverrideEnvironments(bool enabled)
    {
        try
        {
            var overrideSettings = _playerDataModel.playerData.overrideEnvironmentSettings;
            SetPropertyValue(overrideSettings, "overrideEnvironments", enabled);

            Plugin.Log.Debug($"Set override environments to: {enabled}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Failed to set override environments: {ex.Message}");
        }
    }

    public void SetOverrideDefaultColors(bool enabled)
    {
        try
        {
            var colorSettings = _playerDataModel.playerData.colorSchemesSettings;
            SetPropertyValue(colorSettings, "overrideDefaultColors", enabled);

            Plugin.Log.Debug($"Set override default colors to: {enabled}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Failed to set override default colors: {ex.Message}");
        }
    }

    public void SetColorsOverrideNotesOnly()
    {
        // In v3+ lighting, we want notes to use player colors but events to use map colors
        SetOverrideDefaultColors(true);
        Plugin.Log.Debug("Set colors override to notes only");
    }

    public void SetColorsOverrideAll()
    {
        SetOverrideDefaultColors(true);
        Plugin.Log.Debug("Set colors override to all");
    }

    private static void SetPropertyValue(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
        else
        {
            // Try field instead
            var field = obj.GetType().GetField(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Plugin.Log.Warn($"Could not find property or field: {propertyName}");
            }
        }
    }
}
