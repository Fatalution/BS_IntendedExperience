using System;
using System.Reflection;
using SongCore;
using Zenject;

namespace IntendedExperience.Compat;

/// <summary>Reads map effect colors from SongCore and toggles its "environment colors" setting in memory.</summary>
internal class SongCoreColors
{
    private const string ConfigTypeName = "SongCore.PluginConfig";
    private const string EnvironmentColorsField = "<CustomSongEnvironmentColors>k__BackingField";

    // The backing field is written directly so the toggle never reaches SongCore's config file.
    private readonly object? songCoreConfig;
    private readonly FieldInfo? environmentColorsField;

    public SongCoreColors(DiContainer container)
    {
        Type? configType = typeof(Collections).Assembly.GetType(ConfigTypeName);
        environmentColorsField = configType?.GetField(EnvironmentColorsField, BindingFlags.NonPublic | BindingFlags.Instance);
        songCoreConfig = configType == null ? null : container.TryResolve(configType);

        if (songCoreConfig == null || environmentColorsField == null)
            Plugin.Log.Warn("SongCore config not found; map effect colors can't be enabled.");
    }

    public bool EnvironmentColorsEnabled =>
        songCoreConfig == null || environmentColorsField == null || (bool)environmentColorsField.GetValue(songCoreConfig);

    public void SetEnvironmentColorsEnabled(bool enabled)
    {
        if (songCoreConfig != null && environmentColorsField != null)
            environmentColorsField.SetValue(songCoreConfig, enabled);
    }

    public static bool HasEffectColors(BeatmapKey key)
    {
        var difficultyData = Collections.GetCustomLevelSongDifficultyData(key);
        return difficultyData != null
               && (difficultyData._envColorLeft != null || difficultyData._envColorRight != null
                   || difficultyData._envColorWhite != null || difficultyData._envColorLeftBoost != null
                   || difficultyData._envColorRightBoost != null || difficultyData._envColorWhiteBoost != null);
    }
}
