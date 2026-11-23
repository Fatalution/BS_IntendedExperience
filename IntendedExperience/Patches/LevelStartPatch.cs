using System;
using System.Linq;
using HarmonyLib;
using IntendedExperience.Compat;
using IntendedExperience.Core;
using Zenject;

namespace IntendedExperience.Patches;

/// <summary>
/// Hands the level setup modified copies of the player's settings, so the overrides last for that map only
/// and the saved settings are never touched. Hooks the setup data itself, which every solo launch goes through,
/// including ones that skip the menu's StartStandardLevel (e.g. BeatLeader replays).
/// </summary>
internal class LevelStartPatch : IInitializable, IDisposable
{
    private const string HarmonyId = "com.fatalution.IntendedExperience.LevelStart";

    // Read by the static Harmony patches.
    private static LevelStartPatch? active;

    private readonly PluginConfig config;
    private readonly LaunchPlanner launchPlanner;
    private readonly SongCoreColors songCoreColors;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly SettingsSetterPrompt settingsSetterPrompt;
    private readonly Harmony harmony = new(HarmonyId);

    private bool restoreEnvironmentColors;

    public LevelStartPatch(PluginConfig config, LaunchPlanner launchPlanner, SongCoreColors songCoreColors,
        BeatSaberPlusLights beatSaberPlusLights, SettingsSetterPrompt settingsSetterPrompt)
    {
        this.config = config;
        this.launchPlanner = launchPlanner;
        this.songCoreColors = songCoreColors;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.settingsSetterPrompt = settingsSetterPrompt;
    }

    // Some game versions (e.g. 1.40.8) have more than one Init overload; all of them are patched.
    // They share the parameters used here and don't call each other, so each launch runs this once.
    public void Initialize()
    {
        active = this;
        var overloads = AccessTools.GetDeclaredMethods(typeof(StandardLevelScenesTransitionSetupDataSO))
            .Where(method => method.Name == nameof(StandardLevelScenesTransitionSetupDataSO.Init));
        foreach (var overload in overloads)
        {
            harmony.Patch(overload,
                prefix: new HarmonyMethod(typeof(LevelStartPatch), nameof(InitPrefix)),
                postfix: new HarmonyMethod(typeof(LevelStartPatch), nameof(InitPostfix)),
                finalizer: new HarmonyMethod(typeof(LevelStartPatch), nameof(InitFinalizer)));
        }
    }

    public void Dispose()
    {
        harmony.UnpatchSelf();
        if (active == this)
            active = null;
    }

    private static void InitPrefix(ref BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
        ref bool playerOverrideLightshowColors, ref PlayerSpecificSettings playerSpecificSettings) =>
        active?.ApplyPlan(beatmapKey, beatmapLevel, ref playerOverrideLightshowColors, ref playerSpecificSettings);

    // The setup data is reused between launches, so the handler is re-added rather than stacked.
    private static void InitPostfix(StandardLevelScenesTransitionSetupDataSO __instance)
    {
        __instance.didFinishEvent -= HandleLevelFinished;
        __instance.didFinishEvent += HandleLevelFinished;
    }

    private static void InitFinalizer() => active?.RestoreEnvironmentColors();

    // A restart replays the same setup without calling Init again, so the brightness override stays for it.
    private static void HandleLevelFinished(StandardLevelScenesTransitionSetupDataSO setupData,
        LevelCompletionResults results)
    {
        if (results.levelEndAction != LevelCompletionResults.LevelEndAction.Restart)
            active?.beatSaberPlusLights.SetForceFullBrightness(false);
    }

    private void ApplyPlan(BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
        ref bool playerOverrideLightshowColors, ref PlayerSpecificSettings playerSpecificSettings)
    {
        var planned = launchPlanner.Plan(beatmapLevel, beatmapKey, playerSpecificSettings, playerOverrideLightshowColors);

        // If the changes were shown in the Settings Setter window, only what the player accepted applies.
        var plan = settingsSetterPrompt.ApplyDecision(beatmapKey, planned);
        if (plan != planned)
            Plugin.Log.Info($"Settings Setter answer for '{beatmapLevel.songName}': applying {plan} instead of {planned}");

        if (!plan.IsEmpty && !config.ApplyToReplays && ReplayLaunches.IsCurrentLaunchReplay())
        {
            plan = plan.Only(_ => false);
            Plugin.Log.Info($"Replay of '{beatmapLevel.songName}': IE is turned off for replays.");
        }

        // Set on every launch so a previous map's state never carries over.
        beatSaberPlusLights.SetForceFullBrightness(plan.SetFullBrightness);
        beatSaberPlusLights.Refresh();

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
}
