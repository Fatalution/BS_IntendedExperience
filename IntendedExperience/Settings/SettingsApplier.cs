using IntendedExperience.Compat;
using IntendedExperience.Detection;

namespace IntendedExperience.Settings;

internal class SettingsApplier
{
    private readonly PlayerSettingsManager _settingsManager;

    public SettingsApplier(PlayerSettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public bool ApplySettings(MapInfo mapInfo)
    {
        // V2 map or V2 environment - do nothing, keep user's settings
        if (mapInfo.IsV2OrV2Environment)
        {
            Plugin.Log.Info("SettingsApplier: V2 map/environment detected, keeping user settings");
            return false;
        }

        // Map with Chroma/NE/Vivify - let mods control
        if (mapInfo.HasModdedRequirements)
        {
            ApplyModdedMapSettings(mapInfo);
            return true;
        }

        // V3+ map in V3+ environment - apply optimal settings
        if (mapInfo.IsV3PlusInV3Environment)
        {
            ApplyV3OptimalSettings();
            return true;
        }

        Plugin.Log.Info("SettingsApplier: No matching rule, keeping user settings");
        return false;
    }

    private void ApplyModdedMapSettings(MapInfo mapInfo)
    {
        Plugin.Log.Info("SettingsApplier: Applying modded map settings");

        // All effects ON - let the mod lighting work
        _settingsManager.SetEffectsFilter(EnvironmentEffectsFilterPreset.AllEffects);

        // Environment override OFF - use the map's intended environment
        _settingsManager.SetOverrideEnvironments(false);

        // Enable Chroma if the map uses it
        if (mapInfo.HasChroma && ChromaCompat.IsInstalled)
        {
            ChromaCompat.SetEnabled(true);
            Plugin.Log.Debug("SettingsApplier: Enabled Chroma for modded map");
        }
    }

    private void ApplyV3OptimalSettings()
    {
        Plugin.Log.Info("SettingsApplier: Applying V3+ optimal settings");

        // All effects ON
        _settingsManager.SetEffectsFilter(EnvironmentEffectsFilterPreset.AllEffects);

        // Environment override OFF
        _settingsManager.SetOverrideEnvironments(false);

        // Colors override type: notes only
        // This means override note colors but use map's event/light colors
        _settingsManager.SetColorsOverrideNotesOnly();
    }
}
