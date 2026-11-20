using IntendedExperience.Compat;
using IntendedExperience.Core;
using SiraUtil.Affinity;

namespace IntendedExperience.Patches;

/// <summary>
/// Hands the level launch modified copies of the player's settings, so the overrides last for that map only
/// and the saved settings are never touched.
/// </summary>
internal class LevelStartPatch : IAffinity
{
    private readonly LaunchPlanner launchPlanner;
    private readonly SongCoreColors songCoreColors;

    private bool restoreEnvironmentColors;

    public LevelStartPatch(LaunchPlanner launchPlanner, SongCoreColors songCoreColors)
    {
        this.launchPlanner = launchPlanner;
        this.songCoreColors = songCoreColors;
    }

    [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.StartStandardLevel))]
    [AffinityPrefix]
    private void Prefix(ref BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
        ref bool playerOverrideLightshowColors, ref PlayerSpecificSettings playerSpecificSettings)
    {
        var plan = launchPlanner.Plan(beatmapLevel, beatmapKey, playerSpecificSettings, playerOverrideLightshowColors);
        if (plan.IsEmpty)
            return;

        if (plan.EnableAllEffects)
        {
            playerSpecificSettings = playerSpecificSettings.CopyWith(
                environmentEffectsFilterDefaultPreset: EnvironmentEffectsFilterPreset.AllEffects,
                environmentEffectsFilterExpertPlusPreset: EnvironmentEffectsFilterPreset.AllEffects);
        }

        if (plan.UseNotesOnlyColors)
            playerOverrideLightshowColors = false;

        // SongCore reads this while the level is set up inside this call; the finalizer turns it back off.
        if (plan.EnableMapEffectColors)
        {
            songCoreColors.SetEnvironmentColorsEnabled(true);
            restoreEnvironmentColors = true;
        }

        Plugin.Log.Info($"Applying intended settings to '{beatmapLevel.songName}': {plan}");
    }

    [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.StartStandardLevel))]
    [AffinityFinalizer]
    private void Finalizer()
    {
        if (!restoreEnvironmentColors)
            return;

        songCoreColors.SetEnvironmentColorsEnabled(false);
        restoreEnvironmentColors = false;
    }
}
