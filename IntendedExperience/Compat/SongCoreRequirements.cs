using System.Linq;
using SongCore;

namespace IntendedExperience.Compat;

/// <summary>Reads a difficulty's mod requirements from SongCore.</summary>
internal static class SongCoreRequirements
{
    private static readonly string[] ModchartRequirements = { "Noodle Extensions", "Mapping Extensions", "Vivify" };

    /// <summary>The difficulty requires a modcharting mod (Noodle Extensions, Mapping Extensions or Vivify).</summary>
    public static bool IsModchart(BeatmapKey key)
    {
        var requirements = Collections.GetCustomLevelSongDifficultyData(key)?.additionalDifficultyData?._requirements;
        return requirements != null && requirements.Any(ModchartRequirements.Contains);
    }
}
