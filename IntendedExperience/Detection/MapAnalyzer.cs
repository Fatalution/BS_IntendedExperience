using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IntendedExperience.Detection;

internal class MapAnalyzer
{
    private const string ChromaRequirement = "Chroma";
    private const string NoodleExtensionsRequirement = "Noodle Extensions";
    private const string VivifyRequirement = "Vivify";

    public MapInfo Analyze(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
    {
        if (beatmapLevel == null)
        {
            Plugin.Log.Warn("MapAnalyzer: BeatmapLevel is null");
            return new MapInfo();
        }

        var environmentName = GetEnvironmentName(beatmapLevel);
        var isV3Env = EnvironmentClassifier.IsV3Environment(environmentName);
        var version = DetectMapVersion(beatmapLevel, beatmapKey);
        var (hasChroma, hasNE, hasVivify) = DetectModRequirements(beatmapLevel, beatmapKey);

        var mapInfo = new MapInfo
        {
            Version = version,
            EnvironmentName = environmentName,
            IsV3Environment = isV3Env,
            HasChroma = hasChroma,
            HasNoodleExtensions = hasNE,
            HasVivify = hasVivify
        };

        Plugin.Log.Debug($"MapAnalyzer: Version={version}, Env={environmentName}, IsV3Env={isV3Env}, " +
                         $"Chroma={hasChroma}, NE={hasNE}, Vivify={hasVivify}");

        return mapInfo;
    }

    private static string GetEnvironmentName(BeatmapLevel beatmapLevel)
    {
        try
        {
            // Try to get environment name through reflection since the API varies
            var type = beatmapLevel.GetType();

            // Try various property names that might contain environment info
            var propertyNames = new[] { "environmentName", "_environmentName", "EnvironmentName", 
                                        "environmentInfo", "_environmentInfo" };

            foreach (var propName in propertyNames)
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(beatmapLevel);
                    if (value is string strValue && !string.IsNullOrEmpty(strValue))
                    {
                        return strValue;
                    }
                    // If it's an EnvironmentInfoSO, try to get the serialized name
                    if (value != null)
                    {
                        var serializedNameProp = value.GetType().GetProperty("serializedName", 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (serializedNameProp != null)
                        {
                            var serializedName = serializedNameProp.GetValue(value) as string;
                            if (!string.IsNullOrEmpty(serializedName))
                            {
                                return serializedName!;
                            }
                        }
                    }
                }

                var field = type.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var value = field.GetValue(beatmapLevel);
                    if (value is string strValue && !string.IsNullOrEmpty(strValue))
                    {
                        return strValue;
                    }
                }
            }

            // Default to empty - we'll treat unknown environments as V2 (no changes)
            Plugin.Log.Debug("MapAnalyzer: Could not determine environment name");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"MapAnalyzer: Failed to get environment name: {ex.Message}");
            return string.Empty;
        }
    }

    private static MapVersion DetectMapVersion(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
    {
        try
        {
            // If it's an OST/DLC level, assume V3
            if (beatmapLevel.hasPrecalculatedData)
            {
                return MapVersion.V3;
            }

            // For custom levels, default to V3 as most modern maps use it
            return MapVersion.V3;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"MapAnalyzer: Failed to detect map version: {ex.Message}");
            return MapVersion.Unknown;
        }
    }

    private static (bool hasChroma, bool hasNE, bool hasVivify) DetectModRequirements(
        BeatmapLevel beatmapLevel,
        BeatmapKey beatmapKey)
    {
        try
        {
            var requirements = GetRequirementsForDifficulty(beatmapLevel, beatmapKey);
            var suggestions = GetSuggestionsForDifficulty(beatmapLevel, beatmapKey);

            var allMods = requirements.Concat(suggestions).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return (
                allMods.Contains(ChromaRequirement),
                allMods.Contains(NoodleExtensionsRequirement),
                allMods.Contains(VivifyRequirement)
            );
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"MapAnalyzer: Failed to detect mod requirements: {ex.Message}");
            return (false, false, false);
        }
    }

    private static IEnumerable<string> GetRequirementsForDifficulty(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
    {
        // TODO: Integrate with SongCore to get requirements from CustomLevelInfoSaveData
        // For now, return empty
        return Array.Empty<string>();
    }

    private static IEnumerable<string> GetSuggestionsForDifficulty(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
    {
        // TODO: Integrate with SongCore to get suggestions from CustomLevelInfoSaveData
        return Array.Empty<string>();
    }
}
