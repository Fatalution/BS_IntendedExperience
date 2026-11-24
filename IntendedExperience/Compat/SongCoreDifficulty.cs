using IntendedExperience.Core;
using SongCore;
#if GAME_1_29
using DifficultyData = SongCore.Data.ExtraSongData.DifficultyData;
#else
using DifficultyData = SongCore.Data.SongData.DifficultyData;
#endif

namespace IntendedExperience.Compat;

/// <summary>SongCore's data for one difficulty of a custom map (colors, requirements); null for other maps.</summary>
internal static class SongCoreDifficulty
{
    public static DifficultyData? Get(MapDifficulty map) =>
#if GAME_1_29
        Collections.RetrieveDifficultyData(map.Beatmap);
#else
        Collections.GetCustomLevelSongDifficultyData(map.Key);
#endif
}
