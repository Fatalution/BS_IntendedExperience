namespace IntendedExperience.Core;

#if GAME_1_29
/// <summary>One difficulty of one map. 1.29.1 identifies it by the difficulty beatmap itself.</summary>
internal readonly record struct MapDifficulty(IDifficultyBeatmap Beatmap)
{
    public string LevelId => Beatmap.level.levelID;
    public string SongName => Beatmap.level.songName;
    public BeatmapDifficulty Difficulty => Beatmap.difficulty;
    private string Characteristic => Beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

    // The game can hand out a new beatmap object for the same difficulty, so compare what it stands for.
    public bool Equals(MapDifficulty other) =>
        LevelId == other.LevelId && Characteristic == other.Characteristic && Difficulty == other.Difficulty;

    public override int GetHashCode() => (LevelId, Characteristic, Difficulty).GetHashCode();
}
#else
/// <summary>One difficulty of one map: the level and the key that picks the difficulty.</summary>
internal readonly record struct MapDifficulty(BeatmapLevel Level, BeatmapKey Key)
{
    public string LevelId => Level.levelID;
    public string SongName => Level.songName;
    public BeatmapDifficulty Difficulty => Key.difficulty;

    // The key alone identifies the difficulty.
    public bool Equals(MapDifficulty other) => Key.Equals(other.Key);

    public override int GetHashCode() => Key.GetHashCode();
}
#endif
