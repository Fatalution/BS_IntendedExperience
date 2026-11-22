using System;
using System.Linq;
using HarmonyLib;
using IntendedExperience.Compat;
using IntendedExperience.Core;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using Zenject;

namespace IntendedExperience.Patches;

/// <summary>
/// Hands the level launch modified copies of the player's settings, so the overrides last for that map only
/// and the saved settings are never touched.
/// </summary>
internal class LevelStartPatch : IInitializable, IDisposable, IAffinity
{
    private const string HarmonyId = "com.fatalution.IntendedExperience.LevelStart";

    // Private in the game, so it can't be referenced with nameof.
    private const string LevelFinishedMethodName = "HandleMainGameSceneDidFinish";

    // Read by the static Harmony patches.
    private static LevelStartPatch? active;

    private readonly LaunchPlanner launchPlanner;
    private readonly SongCoreColors songCoreColors;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly SettingsSetterPrompt settingsSetterPrompt;
    private readonly Harmony harmony = new(HarmonyId);

    private bool restoreEnvironmentColors;

    public LevelStartPatch(LaunchPlanner launchPlanner, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, SettingsSetterPrompt settingsSetterPrompt)
    {
        this.launchPlanner = launchPlanner;
        this.songCoreColors = songCoreColors;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.settingsSetterPrompt = settingsSetterPrompt;
    }

    // Some game versions (e.g. 1.40.8) have more than one StartStandardLevel overload; all of them are patched.
    // They share the parameters used here and don't call each other, so each launch runs this once.
    public void Initialize()
    {
        active = this;
        var overloads = AccessTools.GetDeclaredMethods(typeof(MenuTransitionsHelper))
            .Where(method => method.Name == nameof(MenuTransitionsHelper.StartStandardLevel));
        foreach (var overload in overloads)
        {
            harmony.Patch(overload,
                prefix: new HarmonyMethod(typeof(LevelStartPatch), nameof(StartLevelPrefix)),
                finalizer: new HarmonyMethod(typeof(LevelStartPatch), nameof(StartLevelFinalizer)));
        }
    }

    public void Dispose()
    {
        harmony.UnpatchSelf();
        if (active == this)
            active = null;
    }

    private static void StartLevelPrefix(ref BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
        ref bool playerOverrideLightshowColors, ref PlayerSpecificSettings playerSpecificSettings) =>
        active?.ApplyPlan(beatmapKey, beatmapLevel, ref playerOverrideLightshowColors, ref playerSpecificSettings);

    private static void StartLevelFinalizer() => active?.RestoreEnvironmentColors();

    private void ApplyPlan(BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
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

    private void RestoreEnvironmentColors()
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
