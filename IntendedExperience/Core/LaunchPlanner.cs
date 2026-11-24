using System.Linq;
using IntendedExperience.Compat;
using IPA.Loader;
using UnityEngine;

namespace IntendedExperience.Core;

/// <summary>Decides which settings a launch must override to show a map as intended.</summary>
internal class LaunchPlanner
{
    private const string ChromaPluginId = "Chroma";

    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;
    private readonly SongCoreColors songCoreColors;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly HeckSettingsSetter heckSettingsSetter;
#if !GAME_1_29
    private readonly EnvironmentsListModel environmentsListModel;
#endif
    private readonly bool chromaInstalled = PluginManager.GetPluginFromId(ChromaPluginId) != null;

#if GAME_1_29
    public LaunchPlanner(PluginConfig config, MapExclusions mapExclusions, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, HeckSettingsSetter heckSettingsSetter)
#else
    public LaunchPlanner(PluginConfig config, MapExclusions mapExclusions, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, HeckSettingsSetter heckSettingsSetter,
        EnvironmentsListModel environmentsListModel)
#endif
    {
        this.config = config;
        this.mapExclusions = mapExclusions;
        this.songCoreColors = songCoreColors;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.heckSettingsSetter = heckSettingsSetter;
#if !GAME_1_29
        this.environmentsListModel = environmentsListModel;
#endif
    }

    /// <param name="settingsSetterApplies">
    /// Heck applies the map's Settings Setter for this launch. When it doesn't, IE also changes the settings the
    /// Settings Setter would have set.
    /// </param>
    public LaunchPlan Plan(MapDifficulty map, PlayerSpecificSettings playerSettings, bool overrideLightshowColors,
        OverrideEnvironmentSettings? overrideEnvironmentSettings, bool settingsSetterApplies = true)
    {
        var environment = MapEnvironment(map);
        if (!config.Enabled || mapExclusions.IsExcluded(map.LevelId) || CategoryOf(map, environment) is not { } category)
            return default;

        var rules = config.RulesFor(category);
        var currentEffects = playerSettings.GetEnvironmentEffectsFilterPreset(map.Difficulty);
        var environmentOverride = ActiveEnvironmentOverride(environment, overrideEnvironmentSettings);
        float currentBrightness = beatSaberPlusLights.EffectiveBrightness;

        // Settings the map's Settings Setter sets are the mapper's call, so IE leaves them alone.
        var setterControls = settingsSetterApplies ? heckSettingsSetter.GetControlledSettings(map) : default;

        // Chroma maps color their own lights, so the player's color override doesn't matter there.
        bool manageColors = rules.ForceIntendedColors && category != MapCategory.Chroma;

        // With both environment color pairs from the map (turned on through SongCore below), the player's
        // "All" override can't reach the lights, so Notes Only isn't needed.
        bool mapColorsCoverEffects = SongCoreColors.HasFullEffectColors(map);

        return new LaunchPlan(
            EnableAllEffects: rules.ForceAllEffects
                              && currentEffects != EnvironmentEffectsFilterPreset.AllEffects
                              && !setterControls.EnvironmentEffects,
            UseMapEnvironment: rules.ForceMapEnvironment && environmentOverride != null && !setterControls.Environment,
            UseNotesOnlyColors: manageColors && overrideLightshowColors && !setterControls.Colors && !mapColorsCoverEffects,
            EnableMapEffectColors: manageColors && !songCoreColors.EnvironmentColorsEnabled && SongCoreColors.HasEffectColors(map),
            SetFullBrightness: rules.ForceFullBrightness && !Mathf.Approximately(currentBrightness, 1f),
            CurrentEffects: currentEffects,
            CurrentEnvironment: environmentOverride?.environmentName,
            MapEnvironment: environment?.environmentName,
            CurrentBrightness: currentBrightness);
    }

    /// <summary>The map is one IE handles: a GLS (v3+) map, a Chroma map or a modchart.</summary>
    public bool Qualifies(MapDifficulty map) => CategoryOf(map, MapEnvironment(map)) != null;

    // The most specific category wins: modchart, then Chroma, then GLS.
    private MapCategory? CategoryOf(MapDifficulty map, EnvironmentInfoSO? environment)
    {
        if (SongCoreRequirements.IsModchart(map))
            return MapCategory.Modcharts;
        if (chromaInstalled && SongCoreRequirements.UsesChroma(map))
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
}
