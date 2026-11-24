using System;
using System.Linq;
using HarmonyLib;
using IntendedExperience.Compat;
using IntendedExperience.Core;
#if GAME_1_29
using IPA.Utilities;
#endif
using JetBrains.Annotations;
using Zenject;

namespace IntendedExperience.Patches;

/// <summary>
/// Hands the level setup modified copies of the player's settings, so the overrides last for that map only
/// and the saved settings are never touched. Hooks the setup data itself, which every solo launch goes through,
/// including ones that skip the menu's StartStandardLevel (e.g. BeatLeader replays).
/// </summary>
[UsedImplicitly]
internal class LevelStartPatch : IInitializable, IDisposable
{
    private const string HarmonyId = "com.fatalution.IntendedExperience.LevelStart";
    private const string InitMethodName = "Init";

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
    // The prefix runs first so mods that patch Init too (SongCore's map colors on 1.29.1) see IE's changes.
    public void Initialize()
    {
        active = this;
        var overloads = AccessTools.GetDeclaredMethods(typeof(StandardLevelScenesTransitionSetupDataSO))
            .Where(method => method.Name == InitMethodName);
        foreach (var overload in overloads)
        {
            harmony.Patch(overload,
                prefix: new HarmonyMethod(typeof(LevelStartPatch), nameof(InitPrefix)) { priority = Priority.First },
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

#if GAME_1_29
    // Any color override reaches the lights on 1.29.1. Notes Only is rebuilt as the player's scheme
    // with the environment's own light colors.
    private static void InitPrefix(IDifficultyBeatmap difficultyBeatmap,
        ref OverrideEnvironmentSettings? overrideEnvironmentSettings, ref ColorScheme? overrideColorScheme,
        ref GameplayModifiers gameplayModifiers, ref PlayerSpecificSettings playerSpecificSettings)
    {
        if (active == null)
            return;

        var settings = new LaunchSettings(playerSpecificSettings, gameplayModifiers, overrideEnvironmentSettings,
            overrideColorScheme);
        var plan = active.ApplyPlan(new MapDifficulty(difficultyBeatmap), settings);

        playerSpecificSettings = settings.PlayerSpecificSettings;
        gameplayModifiers = settings.GameplayModifiers;
        overrideEnvironmentSettings = settings.OverrideEnvironmentSettings;
        overrideColorScheme = plan.UseNotesOnlyColors
            ? WithEnvironmentLights(settings.OverrideColorScheme!, difficultyBeatmap, settings.OverrideEnvironmentSettings)
            : settings.OverrideColorScheme;
    }
#else
    private static void InitPrefix(ref BeatmapKey beatmapKey, BeatmapLevel beatmapLevel,
        ref OverrideEnvironmentSettings? overrideEnvironmentSettings, ref ColorScheme? playerOverrideColorScheme,
        ref bool playerOverrideLightshowColors, ref GameplayModifiers gameplayModifiers,
        ref PlayerSpecificSettings playerSpecificSettings)
    {
        if (active == null)
            return;

        var settings = new LaunchSettings(playerSpecificSettings, gameplayModifiers, overrideEnvironmentSettings,
            playerOverrideColorScheme, playerOverrideLightshowColors);
        var plan = active.ApplyPlan(new MapDifficulty(beatmapLevel, beatmapKey), settings);

        playerSpecificSettings = settings.PlayerSpecificSettings;
        gameplayModifiers = settings.GameplayModifiers;
        overrideEnvironmentSettings = settings.OverrideEnvironmentSettings;
        playerOverrideColorScheme = settings.OverrideColorScheme;
        playerOverrideLightshowColors = settings.OverrideLightshowColors && !plan.UseNotesOnlyColors;
    }
#endif

    // The setup data is reused between launches, so the handler is re-added rather than stacked.
    private static void InitPostfix(StandardLevelScenesTransitionSetupDataSO __instance)
    {
        __instance.didFinishEvent -= HandleLevelFinished;
        __instance.didFinishEvent += HandleLevelFinished;
    }

    private static void InitFinalizer() => active?.RestoreEnvironmentColors();

    // A restart replays the same setup without calling Init again, so the overrides stay for it.
    private static void HandleLevelFinished(StandardLevelScenesTransitionSetupDataSO setupData,
        LevelCompletionResults results)
    {
        if (active == null || results.levelEndAction == LevelCompletionResults.LevelEndAction.Restart)
            return;

        active.beatSaberPlusLights.SetForceFullBrightness(false);
        active.settingsSetterPrompt.RestoreAppliedWithoutWindow();
    }

    /// <summary>
    /// Applies the plan's effects, environment, map colors and brightness to the launch settings;
    /// the caller applies the color override.
    /// </summary>
    private LaunchPlan ApplyPlan(MapDifficulty map, LaunchSettings settings)
    {
        // Heck applies the map's Settings Setter only in its own play flow. Other launches get IE's changes
        // without asking; Multiplayer+ launches also get the map's Settings Setter, as if accepted.
        bool throughHeck = LaunchCallers.IsHeckPlayView();
        bool multiplayerPlus = !throughHeck && LaunchCallers.IsMultiplayerPlus();
        string? turnedOffFor = !config.ApplyToReplays && LaunchCallers.IsReplay() ? "replays"
            : multiplayerPlus && !config.ApplyInMultiplayerPlus ? "Multiplayer+"
            : null;

        bool settingsSetterApplied = throughHeck;
        if (!throughHeck)
        {
            settingsSetterPrompt.Withdraw();
            if (multiplayerPlus && config.Enabled && turnedOffFor == null)
                settingsSetterApplied = settingsSetterPrompt.ApplyWithoutWindow(map, settings);
        }

        var planned = launchPlanner.Plan(map, settings, settingsSetterApplied);

        // If the changes were shown in the Settings Setter window, only what the player accepted applies.
        var plan = settingsSetterPrompt.ApplyDecision(map, planned);
        if (plan != planned)
            Plugin.Log.Info($"Settings Setter answer for '{map.SongName}': applying {plan} instead of {planned}");

        if (turnedOffFor != null && !plan.IsEmpty)
        {
            plan = default;
            Plugin.Log.Info($"'{map.SongName}': IE is turned off for {turnedOffFor}.");
        }

        // Set on every launch so a previous map's state never carries over.
        beatSaberPlusLights.SetForceFullBrightness(plan.SetFullBrightness);
        beatSaberPlusLights.Refresh();

        if (plan.IsEmpty)
            return plan;

        if (plan.EnableAllEffects)
        {
            settings.PlayerSpecificSettings = settings.PlayerSpecificSettings.CopyWith(
                environmentEffectsFilterDefaultPreset: EnvironmentEffectsFilterPreset.AllEffects,
                environmentEffectsFilterExpertPlusPreset: EnvironmentEffectsFilterPreset.AllEffects);
        }

        // A new settings object has overrides off; the player's own one is left as it is.
        if (plan.UseMapEnvironment)
            settings.OverrideEnvironmentSettings = new OverrideEnvironmentSettings();

        // SongCore reads this while the level is set up inside this call; the finalizer turns it back off.
        if (plan.EnableMapEffectColors)
        {
            songCoreColors.SetEnvironmentColorsEnabled(true);
            restoreEnvironmentColors = true;
        }

        Plugin.Log.Info($"Applying intended settings to '{map.SongName}': {plan}");
        return plan;
    }

#if GAME_1_29
    /// <summary>The player's scheme with the light colors of the environment the level will use.</summary>
    private static ColorScheme WithEnvironmentLights(ColorScheme playerScheme, IDifficultyBeatmap beatmap,
        OverrideEnvironmentSettings? overrideEnvironmentSettings)
    {
        // The map's environment, or the player's override for that environment type (as the game picks it).
        var environment = BeatmapEnvironmentHelper.GetEnvironmentInfo(beatmap);
        var overridden = overrideEnvironmentSettings is { overrideEnvironments: true }
            ? overrideEnvironmentSettings.GetOverrideEnvironmentInfoForType(environment.environmentType)
            : null;
        if (overridden != null)
            environment = overridden;

        var lights = new ColorScheme(environment.colorScheme);
        var scheme = new ColorScheme(playerScheme, playerScheme.saberAColor, playerScheme.saberBColor,
            lights.environmentColor0, lights.environmentColor1, lights.supportsEnvironmentColorBoost,
            lights.environmentColor0Boost, lights.environmentColor1Boost, playerScheme.obstaclesColor);

        // The constructor sets the white lights to plain white.
        ReflectionUtil.SetField(scheme, "_environmentColorW", lights.environmentColorW);
        ReflectionUtil.SetField(scheme, "_environmentColorWBoost", lights.environmentColorWBoost);
        return scheme;
    }
#endif

    private void RestoreEnvironmentColors()
    {
        if (!restoreEnvironmentColors)
            return;

        songCoreColors.SetEnvironmentColorsEnabled(false);
        restoreEnvironmentColors = false;
    }
}
