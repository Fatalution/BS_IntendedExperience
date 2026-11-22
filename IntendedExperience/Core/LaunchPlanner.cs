using System.Linq;
using IntendedExperience.Compat;
using UnityEngine;

namespace IntendedExperience.Core;

/// <summary>Decides which settings a launch must override to show a map as intended.</summary>
internal class LaunchPlanner
{
    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;
    private readonly SongCoreColors songCoreColors;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly HeckSettingsSetter heckSettingsSetter;
    private readonly EnvironmentsListModel environmentsListModel;

    public LaunchPlanner(PluginConfig config, MapExclusions mapExclusions, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, HeckSettingsSetter heckSettingsSetter,
        EnvironmentsListModel environmentsListModel)
    {
        this.config = config;
        this.mapExclusions = mapExclusions;
        this.songCoreColors = songCoreColors;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.heckSettingsSetter = heckSettingsSetter;
        this.environmentsListModel = environmentsListModel;
    }

    public LaunchPlan Plan(BeatmapLevel level, BeatmapKey key, PlayerSpecificSettings playerSettings,
        bool overrideLightshowColors)
    {
        if (!config.Enabled || mapExclusions.IsExcluded(level.levelID) || RulesFor(level, key) is not { } rules)
            return default;

        var currentEffects = playerSettings.GetEnvironmentEffectsFilterPreset(key.difficulty);
        float currentBrightness = beatSaberPlusLights.EffectiveBrightness;

        // Settings the map's Settings Setter sets are the mapper's call, so IE leaves them alone.
        var setterControls = heckSettingsSetter.GetControlledSettings(level, key);

        // With both environment color pairs from the map (turned on through SongCore below), the player's
        // "All" override can't reach the lights, so Notes Only isn't needed.
        bool mapColorsCoverEffects = SongCoreColors.HasFullEffectColors(key);

        return new LaunchPlan(
            EnableAllEffects: rules.ForceAllEffects
                              && currentEffects != EnvironmentEffectsFilterPreset.AllEffects
                              && !setterControls.EnvironmentEffects,
            UseNotesOnlyColors: rules.ForceIntendedColors && overrideLightshowColors
                                && !setterControls.Colors && !mapColorsCoverEffects,
            EnableMapEffectColors: rules.ForceIntendedColors
                                   && !songCoreColors.EnvironmentColorsEnabled
                                   && SongCoreColors.HasEffectColors(key),
            SetFullBrightness: rules.ForceFullBrightness && !Mathf.Approximately(currentBrightness, 1f),
            CurrentEffects: currentEffects,
            CurrentBrightness: currentBrightness);
    }

    /// <summary>The map is one IE handles: a GLS (v3+) map or a modchart.</summary>
    public bool Qualifies(BeatmapLevel level, BeatmapKey key) => RulesFor(level, key) != null;

    // A modchart on a GLS environment uses the modchart rules, the more specific of the two.
    private RuleSet? RulesFor(BeatmapLevel level, BeatmapKey key)
    {
        if (SongCoreRequirements.IsModchart(key))
            return config.Modcharts;
        return UsesGroupLighting(level, key) ? config.Gls : null;
    }

    // Only group-lighting (v3+) environments, i.e. Weave and newer, define light groups.
    private bool UsesGroupLighting(BeatmapLevel level, BeatmapKey key)
    {
        string environmentName = level.GetEnvironmentName(key.beatmapCharacteristic, key.difficulty);
        var environmentInfo = environmentsListModel.GetEnvironmentInfoBySerializedName(environmentName);
        return environmentInfo != null && environmentInfo.environmentLightGroups.lightGroups.Any();
    }
}
