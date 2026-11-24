using System;
using System.Reflection;
using IntendedExperience.Core;
using SongCore;
using Zenject;

namespace IntendedExperience.Compat;

/// <summary>Reads map effect colors from SongCore and toggles its "environment colors" setting in memory.</summary>
internal class SongCoreColors
{
#if GAME_1_29
    // SongCore 3.11 keeps its config in a static property of its Plugin class.
    private const string ConfigTypeName = "SongCore.SConfiguration";
#else
    private const string ConfigTypeName = "SongCore.PluginConfig";
#endif
    private const string EnvironmentColorsField = "<CustomSongEnvironmentColors>k__BackingField";

    // The backing field is written directly so the toggle never reaches SongCore's config file.
    private readonly object? songCoreConfig;
    private readonly FieldInfo? environmentColorsField;

    public SongCoreColors(DiContainer container)
    {
        Type? configType = typeof(Collections).Assembly.GetType(ConfigTypeName);
        environmentColorsField = configType?.GetField(EnvironmentColorsField, BindingFlags.NonPublic | BindingFlags.Instance);
#if GAME_1_29
        songCoreConfig = typeof(SongCore.Plugin)
            .GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null);
#else
        songCoreConfig = configType == null ? null : container.TryResolve(configType);
#endif

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

    /// <summary>The map sets both environment color pairs (normal and boost), so they replace any player override.</summary>
    public static bool HasFullEffectColors(MapDifficulty map)
    {
        var difficultyData = SongCoreDifficulty.Get(map);
        return difficultyData != null
               && difficultyData._envColorLeft != null && difficultyData._envColorRight != null
               && difficultyData._envColorLeftBoost != null && difficultyData._envColorRightBoost != null;
    }

    public static bool HasEffectColors(MapDifficulty map)
    {
        var difficultyData = SongCoreDifficulty.Get(map);
        return difficultyData != null
               && (difficultyData._envColorLeft != null || difficultyData._envColorRight != null
                   || difficultyData._envColorWhite != null || difficultyData._envColorLeftBoost != null
                   || difficultyData._envColorRightBoost != null || difficultyData._envColorWhiteBoost != null);
    }
}
