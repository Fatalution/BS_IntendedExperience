using System.Linq;
using IntendedExperience.Core;

namespace IntendedExperience.Compat;

/// <summary>Reads a difficulty's mod requirements and suggestions from SongCore.</summary>
internal static class SongCoreRequirements
{
    private const string ChromaRequirement = "Chroma";

    private static readonly string[] ModchartRequirements = { "Noodle Extensions", "Mapping Extensions", "Vivify" };

    /// <summary>The difficulty requires a modcharting mod (Noodle Extensions, Mapping Extensions or Vivify).</summary>
    public static bool IsModchart(MapDifficulty map)
    {
        var requirements = SongCoreDifficulty.Get(map)?.additionalDifficultyData?._requirements;
        return requirements != null && requirements.Any(ModchartRequirements.Contains);
    }

    /// <summary>The difficulty requires or suggests Chroma, i.e. it colors its own lights.</summary>
    public static bool UsesChroma(MapDifficulty map)
    {
        var data = SongCoreDifficulty.Get(map)?.additionalDifficultyData;
        return data != null
               && (data._requirements?.Contains(ChromaRequirement) == true
                   || data._suggestions?.Contains(ChromaRequirement) == true);
    }
}
