using System.Linq;
using IntendedExperience.Compat;
using JetBrains.Annotations;
using UnityEngine;

namespace IntendedExperience.Core;

/// <summary>What IE makes of one map, whatever the player's settings.</summary>
/// <param name="Category">The map's type; null for a legacy map.</param>
/// <param name="Ranked">The map is ranked on ScoreSaber or BeatLeader.</param>
/// <param name="RankedTurnedOff">The map is ranked and its type is turned off for ranked maps, included or not.</param>
/// <param name="Qualifies">IE applies its rules to the map (exclusions aside).</param>
internal readonly record struct MapStatus(MapCategory? Category, bool Ranked, bool RankedTurnedOff, bool Qualifies);

/// <summary>Decides which settings a launch must override to show a map as intended.</summary>
[UsedImplicitly]
internal class LaunchPlanner
{
    private const string TimbalandEnvironment = "TimbalandEnvironment";

    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;
    private readonly SongCoreColors songCoreColors;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly HeckSettingsSetter heckSettingsSetter;
    private readonly RankedMaps rankedMaps;
#if !GAME_1_29
    private readonly EnvironmentsListModel environmentsListModel;
#endif

    // The menu plans every frame; a map's data only needs reading when the selected difficulty changes.
    private MapDifficulty? cachedMap;
    private MapData cachedData;

#if GAME_1_29
    public LaunchPlanner(PluginConfig config, MapExclusions mapExclusions, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, HeckSettingsSetter heckSettingsSetter, RankedMaps rankedMaps)
#else
    public LaunchPlanner(PluginConfig config, MapExclusions mapExclusions, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, HeckSettingsSetter heckSettingsSetter, RankedMaps rankedMaps,
        EnvironmentsListModel environmentsListModel)
#endif
    {
        this.config = config;
        this.mapExclusions = mapExclusions;
        this.songCoreColors = songCoreColors;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.heckSettingsSetter = heckSettingsSetter;
        this.rankedMaps = rankedMaps;
#if !GAME_1_29
        this.environmentsListModel = environmentsListModel;
#endif
    }

    /// <param name="map">The difficulty being launched.</param>
    /// <param name="settings">The settings the launch would use without IE.</param>
    /// <param name="settingsSetterApplies">
    /// Heck applies the map's Settings Setter for this launch. When it doesn't, IE also changes the settings the
    /// Settings Setter would have set.
    /// </param>
    public LaunchPlan Plan(MapDifficulty map, LaunchSettings settings, bool settingsSetterApplies = true)
    {
        if (!config.Enabled || mapExclusions.IsExcluded(map.LevelId))
            return default;

        var status = Status(map);
        if (!status.Qualifies || status.Category is not { } category)
            return default;

        var data = DataOf(map);
        var rules = config.RulesFor(category);
        var currentEffects = settings.PlayerSpecificSettings.GetEnvironmentEffectsFilterPreset(map.Difficulty);
        var environmentOverride = ActiveEnvironmentOverride(data.Environment, settings.OverrideEnvironmentSettings);
        float currentBrightness = beatSaberPlusLights.EffectiveBrightness;

        // Settings the map's Settings Setter sets are the mapper's call, so IE leaves them alone.
        var setterControls = settingsSetterApplies ? heckSettingsSetter.GetControlledSettings(map) : default;

        // Chroma maps color their own lights, so the player's color override doesn't matter there.
        bool manageColors = rules.ForceIntendedColors && category != MapCategory.Chroma;

        return new LaunchPlan(
            EnableAllEffects: rules.ForceAllEffects
                              && currentEffects != EnvironmentEffectsFilterPreset.AllEffects
                              && !setterControls.EnvironmentEffects,
            UseMapEnvironment: rules.ForceMapEnvironment && environmentOverride != null && !setterControls.Environment,
            // With both environment color pairs from the map (turned on through SongCore below), the player's
            // "All" override can't reach the lights, so Notes Only isn't needed.
            UseNotesOnlyColors: manageColors && settings.OverridesLightColors && !setterControls.Colors
                                && !data.HasFullEffectColors,
            EnableMapEffectColors: manageColors && !songCoreColors.EnvironmentColorsEnabled && data.HasEffectColors,
            SetFullBrightness: rules.ForceFullBrightness && !Mathf.Approximately(currentBrightness, 1f),
            CurrentEffects: currentEffects,
            CurrentEnvironment: environmentOverride?.environmentName,
            MapEnvironment: data.Environment?.environmentName,
            CurrentBrightness: currentBrightness);
    }

    public MapStatus Status(MapDifficulty map)
    {
        var data = DataOf(map);
        bool ranked = rankedMaps.IsRanked(map);
        if (data.Category is not { } category)
            return new MapStatus(null, ranked, false, false);

        // Until SongDetailsCache has loaded, a custom map might be ranked, so the ranked skips below apply to it too.
        bool mayBeRanked = ranked || rankedMaps.IsPending(map);

        // Ranked Chroma maps on Timbaland tend to be blinding with all effects on, so IE leaves them alone.
        if (mayBeRanked && category == MapCategory.Chroma && data.Environment?.serializedName == TimbalandEnvironment)
            return new MapStatus(category, ranked, false, false);

        // Ranked maps of a type turned off for ranked maps are skipped unless the player included them.
        bool turnedOff = mayBeRanked && !config.RulesFor(category).ApplyToRanked;
        return new MapStatus(category, ranked, ranked && turnedOff, !turnedOff || mapExclusions.IsIncluded(map.LevelId));
    }

    private MapData DataOf(MapDifficulty map)
    {
        if (cachedMap != map)
        {
            var environment = MapEnvironment(map);
            cachedData = new MapData(environment, DetectCategory(map, environment),
                SongCoreColors.HasEffectColors(map), SongCoreColors.HasFullEffectColors(map));
            cachedMap = map;
        }
        return cachedData;
    }

    // The most specific category wins: modchart, then Chroma, then GLS.
    private static MapCategory? DetectCategory(MapDifficulty map, EnvironmentInfoSO? environment)
    {
        if (SongCoreRequirements.IsModchart(map))
            return MapCategory.Modcharts;
        if (InstalledMods.Chroma && SongCoreRequirements.UsesChroma(map))
            return MapCategory.Chroma;
        return UsesGroupLighting(environment) ? MapCategory.Gls : null;
    }

    // Only group-lighting (v3+) environments, i.e. Weave and newer, define light groups.
    private static bool UsesGroupLighting(EnvironmentInfoSO? environment)
    {
        if (environment == null)
            return false;
#if GAME_1_29
        return environment.lightGroups?.lightGroupSOList?.Any() == true;
#else
        return environment.environmentLightGroups.lightGroups.Any();
#endif
    }

    // The environment the map asks for; the game falls back to the default one when it doesn't have it.
    private EnvironmentInfoSO? MapEnvironment(MapDifficulty map)
    {
#if GAME_1_29
        return BeatmapEnvironmentHelper.GetEnvironmentInfo(map.Beatmap);
#else
        string environmentName = map.Level.GetEnvironmentName(map.Key.beatmapCharacteristic, map.Key.difficulty);
        return environmentsListModel.GetEnvironmentInfoBySerializedName(environmentName);
#endif
    }

    // The player's override for this environment's type, if it would replace the map's environment.
    private static EnvironmentInfoSO? ActiveEnvironmentOverride(EnvironmentInfoSO? environment,
        OverrideEnvironmentSettings? overrideEnvironmentSettings)
    {
        if (environment == null || overrideEnvironmentSettings is not { overrideEnvironments: true })
            return null;

        var overridden = overrideEnvironmentSettings.GetOverrideEnvironmentInfoForType(environment.environmentType);
        return overridden != null && overridden.environmentName != environment.environmentName ? overridden : null;
    }

    /// <summary>A map's data that never changes while the game runs.</summary>
    private readonly record struct MapData(EnvironmentInfoSO? Environment, MapCategory? Category, bool HasEffectColors,
        bool HasFullEffectColors);
}
