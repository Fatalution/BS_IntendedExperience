using System;
using System.Runtime.CompilerServices;
using IntendedExperience.Core;
using JetBrains.Annotations;
using SongDetailsCache;
using Zenject;
using RankedStates = SongDetailsCache.Structs.RankedStates;

namespace IntendedExperience.Compat;

/// <summary>
/// Whether a custom map is ranked on ScoreSaber or BeatLeader, from SongDetailsCache when it's installed.
/// Maps count as unranked while its data is still loading.
/// </summary>
[UsedImplicitly]
internal class RankedMaps : IInitializable
{
    private const int HashLength = 40;

    // The menu asks every frame; once the data is loaded, the answer for a map doesn't change.
    private string? cachedLevelId;
    private bool cachedRanked;

    public void Initialize()
    {
        if (InstalledMods.SongDetailsCache)
            LoadSongDetails();
    }

    public bool IsRanked(MapDifficulty map)
    {
        if (!InstalledMods.SongDetailsCache)
            return false;
        if (map.LevelId == cachedLevelId)
            return cachedRanked;

        bool ranked = CustomLevels.HashOf(map.LevelId) is { Length: HashLength } hash && IsRankedHash(hash);
        if (IsLoaded())
        {
            cachedLevelId = map.LevelId;
            cachedRanked = ranked;
        }
        return ranked;
    }

    /// <summary>The map is a custom map whose ranked state isn't known yet: SongDetailsCache is still loading.</summary>
    public bool IsPending(MapDifficulty map) =>
        InstalledMods.SongDetailsCache && IsLoading() && CustomLevels.HashOf(map.LevelId) is { Length: HashLength };

    // The methods below are kept separate so SongDetailsCache's types are only loaded when it's installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadSongDetails() => SongDetailsLookup.Load();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsLoaded() => SongDetailsLookup.IsLoaded;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsLoading() => SongDetailsLookup.IsLoading;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsRankedHash(string hash) => SongDetailsLookup.IsRanked(hash);

    private static class SongDetailsLookup
    {
        private const RankedStates Ranked = RankedStates.ScoresaberRanked | RankedStates.BeatleaderRanked;

        private static SongDetails? songDetails;

        public static bool IsLoaded => songDetails != null;

        public static bool IsLoading { get; private set; }

        // Other mods load the same shared data, so this is usually instant.
        public static async void Load()
        {
            IsLoading = true;
            try
            {
                songDetails = await SongDetails.Init();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"Couldn't load SongDetailsCache; ranked maps won't be recognized: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public static bool IsRanked(string hash) =>
            songDetails != null && songDetails.songs.FindByHash(hash, out var song) && (song.rankedStates & Ranked) != 0;
    }
}
