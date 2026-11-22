using IntendedExperience.Compat;
using IntendedExperience.Core;
using JetBrains.Annotations;
using SiraUtil.Affinity;

namespace IntendedExperience.Patches;

/// <summary>
/// Hands the level launch modified copies of the player's settings, so the overrides last for that map only
/// and the saved settings are never touched.
/// </summary>
internal class LevelStartPatch : IAffinity
{
    // Private in the game, so it can't be referenced with nameof.
    private const string LevelFinishedMethodName = "HandleMainGameSceneDidFinish";

    private readonly LaunchPlanner launchPlanner;
    private readonly SongCoreColors songCoreColors;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly SettingsSetterPrompt settingsSetterPrompt;

    private bool restoreEnvironmentColors;

    public LevelStartPatch(LaunchPlanner launchPlanner, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, SettingsSetterPrompt settingsSetterPrompt)
    {
        this.launchPlanner = launchPlanner;
        this.songCoreColors = songCoreColors;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.settingsSetterPrompt = settingsSetterPrompt;
    }

    [UsedImplicitly]
    [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.StartStandardLevel))]
    [AffinityPrefix]
    private void Prefix(ref BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
        ref bool playerOverrideLightshowColors, ref PlayerSpecificSettings playerSpecificSettings)
    {
        var plan = launchPlanner.Plan(beatmapLevel, beatmapKey, playerSpecificSettings, playerOverrideLightshowColors);

        // If the changes were shown in the Settings Setter window, only what the player accepted applies.
        plan = settingsSetterPrompt.ApplyDecision(beatmapKey, plan);

        // Set on every launch so a previous map's state never carries over.
        beatSaberPlusLights.SetForceFullBrightness(plan.SetFullBrightness);

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

    [UsedImplicitly]
    [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.StartStandardLevel))]
    [AffinityFinalizer]
    private void Finalizer()
    {
        if (!restoreEnvironmentColors)
            return;

        songCoreColors.SetEnvironmentColorsEnabled(false);
        restoreEnvironmentColors = false;
    }

    // A restart replays the same launch without StartStandardLevel, so the brightness override stays for it.
    [UsedImplicitly]
    [AffinityPatch(typeof(MenuTransitionsHelper), LevelFinishedMethodName)]
    [AffinityPrefix]
    private void LevelFinishedPrefix(LevelCompletionResults levelCompletionResults)
    {
        if (levelCompletionResults.levelEndAction != LevelCompletionResults.LevelEndAction.Restart)
            beatSaberPlusLights.SetForceFullBrightness(false);
    }
}
