using Heck.PlayView;
using IntendedExperience.Core;

namespace IntendedExperience.Compat;

/// <summary>Reads Heck's launch parameters the same way on every game version. Only loaded when Heck is installed.</summary>
internal static class HeckLaunchParameters
{
    public static MapDifficulty Map(this StartStandardLevelParameters parameters) =>
#if GAME_1_29
        new(parameters.DifficultyBeatmap);
#else
        new(parameters.BeatmapLevel, parameters.BeatmapKey);
#endif

    public static LaunchSettings Settings(this StartStandardLevelParameters parameters) => new(
        parameters.PlayerSpecificSettings, parameters.GameplayModifiers, parameters.OverrideEnvironmentSettings,
#if GAME_1_29
        parameters.OverrideColorScheme);
#else
        parameters.OverrideColorScheme, parameters.PlayerOverrideLightshowColors);
#endif

    /// <summary>Copies the settings back, e.g. after Heck's Settings Setter changed them.</summary>
    public static void CopyTo(this StartStandardLevelParameters parameters, LaunchSettings settings)
    {
        settings.PlayerSpecificSettings = parameters.PlayerSpecificSettings;
        settings.GameplayModifiers = parameters.GameplayModifiers;
        settings.OverrideEnvironmentSettings = parameters.OverrideEnvironmentSettings;
        settings.OverrideColorScheme = parameters.OverrideColorScheme;
    }
}
