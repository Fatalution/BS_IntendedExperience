using System;
using System.Runtime.CompilerServices;
using IntendedExperience.Core;
using SongDetailsCache;
using Zenject;
using RankedStates = SongDetailsCache.Structs.RankedStates;

namespace IntendedExperience.Compat;

/// <summary>
/// Whether a custom map is ranked on ScoreSaber or BeatLeader, from SongDetailsCache when it's installed.
/// Maps count as unranked while its data is still loading.
/// </summary>
internal class RankedMaps : IInitializable
{
    private const string CustomLevelPrefix = "custom_level_";
    private const int HashLength = 40;

    public void Initialize()
    {
        if (InstalledMods.SongDetailsCache)
            LoadSongDetails();
    }

    public bool IsRanked(MapDifficulty map)
    {
        if (!InstalledMods.SongDetailsCache || !map.LevelId.StartsWith(CustomLevelPrefix, StringComparison.Ordinal))
            return false;

        string hash = map.LevelId.Substring(CustomLevelPrefix.Length);
        return hash.Length == HashLength && IsRankedHash(hash);
    }

    // The two methods below are kept separate so SongDetailsCache's types are only loaded when it's installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadSongDetails() => SongDetailsLookup.Load();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsRankedHash(string hash) => SongDetailsLookup.IsRanked(hash);

    private static class SongDetailsLookup
    {
        private const RankedStates Ranked = RankedStates.ScoresaberRanked | RankedStates.BeatleaderRanked;

        private static SongDetails? songDetails;

        // Other mods load the same shared data, so this is usually instant.
        public static async void Load()
        {
            try
            {
                songDetails = await SongDetails.Init();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"Couldn't load SongDetailsCache; ranked maps won't be recognized: {ex.Message}");
            }
        }

        public static bool IsRanked(string hash) =>
            songDetails != null && songDetails.songs.FindByHash(hash, out var song) && (song.rankedStates & Ranked) != 0;
    }
}
