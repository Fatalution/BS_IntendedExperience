using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck.PlayView;
using Heck.SettingsSetter;
using IntendedExperience.Core;
using Zenject;

namespace IntendedExperience.Compat;

/// <summary>
/// Lists IE's changes in Heck's Settings Setter window. IE registers its own rows with Heck and, while the window
/// is being set up, briefly adds matching entries to the map's "_settings" data so Heck shows them next to the
/// mapper's. Only loaded when Heck is installed.
/// </summary>
internal class HeckSettingsSetterPrompt : IInitializable, IDisposable
{
    private const string HarmonyId = "com.fatalution.IntendedExperience.HeckSettingsSetter";
    private const string InitMethod = "Heck.SettingsSetter.SettingsSetterViewController:Init";
    private const string SettingsKey = "_settings";
    private const string GroupKey = "_intendedExperience";
    private const string GroupName = "Intended Experience";
    private const string BeatLeaderNamespace = "BeatLeader.";

    // Heck's registry is static and outlives IE's app container, so rows are registered once per game session.
    private static readonly Dictionary<ChangeKind, PromptSetting> PromptSettings = new();

    // Read by the static Harmony patches.
    private static HeckSettingsSetterPrompt? active;

    private readonly LaunchPlanner launchPlanner;
    private readonly SettingsSetterPrompt prompt;
    private readonly Harmony harmony = new(HarmonyId);

    // ---- Entries added to the map's data for the current window ----
    private CustomData? injectedRoot;
    private CustomData? injectedSettings;
    private bool createdSettings;
    private BeatmapKey injectedKey;

    public HeckSettingsSetterPrompt(LaunchPlanner launchPlanner, SettingsSetterPrompt prompt)
    {
        this.launchPlanner = launchPlanner;
        this.prompt = prompt;
    }

    public void Initialize()
    {
        try
        {
            var init = AccessTools.Method(InitMethod)
                       ?? throw new MissingMethodException("SettingsSetterViewController.Init not found.");

            RegisterSettings();
            harmony.Patch(init,
                prefix: new HarmonyMethod(typeof(HeckSettingsSetterPrompt), nameof(InitPrefix)),
                postfix: new HarmonyMethod(typeof(HeckSettingsSetterPrompt), nameof(InitPostfix)),
                finalizer: new HarmonyMethod(typeof(HeckSettingsSetterPrompt), nameof(InitFinalizer)));

            active = this;
            prompt.IsAvailable = true;
            prompt.CanOfferFor = HasCustomData;
            Plugin.Log.Info("Heck Settings Setter hooked; IE's changes can be shown in its window.");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Couldn't hook Heck's Settings Setter; IE's changes won't be shown in it: {ex}");
        }
    }

    public void Dispose()
    {
        harmony.UnpatchSelf();
        if (active == this)
            active = null;
    }

    // Only custom maps keep their custom data between calls, so only they can carry the injected entries.
    private static bool HasCustomData(BeatmapLevel level, BeatmapKey key) =>
        level.GetDifficultyBeatmapData(key.beatmapCharacteristic, key.difficulty) is CustomBeatmapBasicData;

    private static void RegisterSettings()
    {
        if (PromptSettings.Count > 0)
            return;

        foreach (ChangeKind kind in Enum.GetValues(typeof(ChangeKind)))
        {
            var setting = new PromptSetting(kind);
            PromptSettings[kind] = setting;
            SettingSetterSettableSettingsManager.RegisterSettableSetting(GroupKey, FieldKey(kind), setting);
        }
    }

    private static string FieldKey(ChangeKind kind) => "_" + kind;

    private static void InitPrefix(StartStandardLevelParameters __0) => active?.InjectChanges(__0);

    private static void InitPostfix(bool __result)
    {
        if (active is { injectedRoot: not null } prompter && __result)
            prompter.prompt.Offer(prompter.injectedKey);
    }

    private static void InitFinalizer() => active?.RemoveInjectedChanges();

    private void InjectChanges(StartStandardLevelParameters parameters)
    {
        // IE doesn't apply to vanilla multiplayer launches, so there's nothing to show there.
        if (parameters is StartMultiplayerLevelParameters)
            return;

        // BeatLeader opens this window before replays but never applies what's accepted in it, and an extra row
        // makes it open the window more often (which runs into its back-button bug). IE applies its changes directly.
        if (IsOpenedByBeatLeader())
        {
            prompt.Withdraw();
            Plugin.Log.Info("BeatLeader replay: IE's changes are applied without the Settings Setter window.");
            return;
        }

        var level = parameters.BeatmapLevel;
        var key = parameters.BeatmapKey;
        if (!prompt.ShowsInMapWindow(level, key))
            return;

        var changes = launchPlanner
            .Plan(level, key, parameters.PlayerSpecificSettings, parameters.PlayerOverrideLightshowColors)
            .Changes();
        if (changes.Count == 0)
            return;

        var root = level.GetBeatmapCustomData(in key);
        var settings = root.Get<CustomData>(SettingsKey);
        createdSettings = settings == null;
        settings ??= new CustomData();

        // Heck lists a registered setting when the map's value differs from the setting's current value.
        var group = new CustomData();
        foreach (var change in changes)
        {
            var setting = PromptSettings[change.Kind];
            setting.FieldName = change.Setting;
            setting.CurrentValue = change.From;
            group[FieldKey(change.Kind)] = change.To;
        }

        settings[GroupKey] = group;
        if (createdSettings)
            root[SettingsKey] = settings;

        injectedRoot = root;
        injectedSettings = settings;
        injectedKey = key;
    }

    // BeatLeader calls Init itself from its replay navigation, so its code is on the stack.
    private static bool IsOpenedByBeatLeader() =>
        new StackTrace().GetFrames()?.Any(frame =>
            frame.GetMethod()?.DeclaringType?.FullName?.StartsWith(BeatLeaderNamespace, StringComparison.Ordinal) == true) == true;

    private void RemoveInjectedChanges()
    {
        if (injectedRoot == null)
            return;

        injectedSettings!.TryRemove(GroupKey, out _);
        if (createdSettings)
            injectedRoot.TryRemove(SettingsKey, out _);

        injectedRoot = null;
        injectedSettings = null;
    }

    /// <summary>One IE row in the Settings Setter window; accepting it calls <see cref="SetTemporary"/>.</summary>
    private class PromptSetting : ISettableSetting
    {
        private readonly ChangeKind kind;

        public PromptSetting(ChangeKind kind)
        {
            this.kind = kind;
            FieldName = kind.ToString();
        }

        public string GroupName => HeckSettingsSetterPrompt.GroupName;

        // Both are set from the change being shown, just before Heck builds the window.
        public string FieldName { get; set; }
        public string CurrentValue { get; set; } = string.Empty;

        public object TrueValue => CurrentValue;

        public void SetTemporary(object? tempValue)
        {
            if (tempValue != null)
                active?.prompt.Accept(kind);
        }
    }
}
