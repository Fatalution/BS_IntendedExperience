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

    /// <summary>The player's color override reaches the lights too ("All" override; on 1.29.1 any override).</summary>
    public static bool OverridesLightColors(this StartStandardLevelParameters parameters) =>
#if GAME_1_29
        parameters.OverrideColorScheme != null;
#else
        parameters.PlayerOverrideLightshowColors;
#endif
}
