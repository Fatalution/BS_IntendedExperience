using System;

namespace IntendedExperience.Core;

/// <summary>Custom (SongCore) maps' level ids: "custom_level_" followed by the map's hash.</summary>
internal static class CustomLevels
{
    private const string Prefix = "custom_level_";

    /// <summary>The map's hash, or null for a level that isn't a custom map.</summary>
    public static string? HashOf(string levelId) =>
        levelId.StartsWith(Prefix, StringComparison.Ordinal) ? levelId.Substring(Prefix.Length) : null;
}
