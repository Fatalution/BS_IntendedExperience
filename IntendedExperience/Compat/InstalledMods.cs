using IPA.Loader;

namespace IntendedExperience.Compat;

/// <summary>The optional mods IE works with, by their BSIPA plugin ids. Mods can't be added while the game runs.</summary>
internal static class InstalledMods
{
    public const string HeckId = "Heck";
    public const string CustomJsonDataId = "CustomJSONData";
    public const string ChromaId = "Chroma";
    public const string GameTweakerId = "BeatSaberPlus_GameTweaker";
    public const string MultiplayerPlusId = "BeatSaberPlus_Multiplayer";
    public const string SongDetailsCacheId = "SongDetailsCache";

    public static readonly bool Heck = IsInstalled(HeckId);
    public static readonly bool Chroma = IsInstalled(ChromaId);
    public static readonly bool MultiplayerPlus = IsInstalled(MultiplayerPlusId);
    public static readonly bool SongDetailsCache = IsInstalled(SongDetailsCacheId);

    private static bool IsInstalled(string id) => PluginManager.GetPluginFromId(id) != null;
}
