using System.Linq;
using IntendedExperience.Compat;

namespace IntendedExperience.Core;

/// <summary>Decides which settings a launch must override to show a map as intended.</summary>
internal class LaunchPlanner
{
    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;
    private readonly SongCoreColors songCoreColors;
    private readonly EnvironmentsListModel environmentsListModel;

    public LaunchPlanner(PluginConfig config, MapExclusions mapExclusions, SongCoreColors songCoreColors,
        EnvironmentsListModel environmentsListModel)
    {
        this.config = config;
        this.mapExclusions = mapExclusions;
        this.songCoreColors = songCoreColors;
        this.environmentsListModel = environmentsListModel;
    }

    public LaunchPlan Plan(BeatmapLevel level, BeatmapKey key, PlayerSpecificSettings playerSettings,
        bool overrideLightshowColors)
    {
        if (!config.Enabled || mapExclusions.IsExcluded(level.levelID))
            return default;

        bool groupLighting = UsesGroupLighting(level, key);
        bool lightsReduced = playerSettings.GetEnvironmentEffectsFilterPreset(key.difficulty)
                             != EnvironmentEffectsFilterPreset.AllEffects;

        return new LaunchPlan(
            EnableLights: config.ForceLights && groupLighting && lightsReduced,
            UseNotesOnlyColors: config.ForceNotesOnlyColors && groupLighting && overrideLightshowColors,
            EnableMapEffectColors: config.ForceMapEffectColors
                                   && (groupLighting || config.MapEffectColorsOnLegacyEnvironments)
                                   && !songCoreColors.EnvironmentColorsEnabled
                                   && SongCoreColors.HasEffectColors(key));
    }

    // Only group-lighting (v3+) environments, i.e. Weave and newer, define light groups.
    private bool UsesGroupLighting(BeatmapLevel level, BeatmapKey key)
    {
        string environmentName = level.GetEnvironmentName(key.beatmapCharacteristic, key.difficulty);
        var environmentInfo = environmentsListModel.GetEnvironmentInfoBySerializedName(environmentName);
        return environmentInfo != null && environmentInfo.environmentLightGroups.lightGroups.Any();
    }
}
