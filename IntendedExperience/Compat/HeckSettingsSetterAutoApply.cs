using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Heck.PlayView;
using IntendedExperience.Core;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace IntendedExperience.Compat;

/// <summary>
/// Applies a map's Heck Settings Setter without its window, as if the player accepted it, for launches that skip
/// Heck's play flow (Multiplayer+). Runs Heck's own view, so the result is the same as accepting the window.
/// Only loaded when Heck is installed.
/// </summary>
[UsedImplicitly]
internal class HeckSettingsSetterAutoApply : IInitializable
{
    private const string ViewTypeName = "Heck.SettingsSetter.SettingsSetterViewController";
    private const string GameMode = "Solo";
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly SettingsSetterPrompt prompt;

    // ---- Heck's view (internal, so reached through reflection) ----
    private Type? viewType;
    private FieldInfo? modifiedParametersField;
    private MethodInfo? onPlay;
    private MethodInfo? onFinish;

    // The view whose settings stay applied until the level ends.
    private IPlayViewController? appliedView;

    public HeckSettingsSetterAutoApply(SettingsSetterPrompt prompt)
    {
        this.prompt = prompt;
    }

    public void Initialize()
    {
        viewType = AccessTools.TypeByName(ViewTypeName);
        modifiedParametersField = viewType?.GetField("_modifiedParameters", InstanceMembers);
        onPlay = viewType?.GetMethod("OnPlay", InstanceMembers);
        onFinish = viewType?.GetMethod("OnFinish", InstanceMembers);
        if (modifiedParametersField == null || onPlay == null || onFinish == null)
        {
            Plugin.Log.Warn("Couldn't find Heck's Settings Setter internals; it won't be applied outside Heck's play flow.");
            return;
        }

        prompt.ApplyWithoutWindow = Apply;
        prompt.RestoreAppliedWithoutWindow = Restore;
    }

    // Returns false when Heck's view isn't available; the launch then goes ahead without it.
    private bool Apply(MapDifficulty map, LaunchSettings settings)
    {
        Restore();

        // The view lives in the menu scene.
        if (Resources.FindObjectsOfTypeAll(viewType!).FirstOrDefault() is not IPlayViewController view)
            return false;

        try
        {
            // Init works out the map's settings like it does for the window; false means there's nothing to change.
            if (!view.Init(CreateParameters(map, settings)))
            {
                onFinish!.Invoke(view, null);
                return true;
            }

            ((StartStandardLevelParameters)modifiedParametersField!.GetValue(view)).CopyTo(settings);

            // Graphics settings and other mods' settings, as Heck applies them just before the level starts.
            onPlay!.Invoke(view, null);
            appliedView = view;
            Plugin.Log.Info($"Applied the Settings Setter of '{map.SongName}' without its window.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Couldn't apply the Settings Setter of '{map.SongName}': {ex}");
            return false;
        }
    }

    private void Restore()
    {
        if (appliedView == null)
            return;

        onFinish!.Invoke(appliedView, null);
        appliedView = null;
    }

    // Heck's view only reads the map and the player's settings; the rest is left empty.
    private static StartStandardLevelParameters CreateParameters(MapDifficulty map, LaunchSettings settings) =>
#if GAME_1_29
        new(GameMode, map.Beatmap, map.Beatmap.level, settings.OverrideEnvironmentSettings, settings.OverrideColorScheme,
            settings.GameplayModifiers, settings.PlayerSpecificSettings, null, string.Empty, false, false,
            null, null, null);
#elif GAME_1_40
        new(GameMode, map.Key, map.Level, settings.OverrideEnvironmentSettings, settings.OverrideColorScheme,
            settings.OverrideLightshowColors, null, settings.GameplayModifiers, settings.PlayerSpecificSettings, null, null,
            string.Empty, false, false, null, null, null, null, null);
#else
        new(GameMode, map.Key, map.Level, settings.OverrideEnvironmentSettings, settings.OverrideColorScheme,
            settings.OverrideLightshowColors, settings.GameplayModifiers, settings.PlayerSpecificSettings,
            null, null, null, null, null, null, null, null, null);
#endif
}
