using System;
using System.Collections.Generic;
using IntendedExperience.Compat;

namespace IntendedExperience.Core;

/// <summary>
/// The player's answer to IE's changes shown before the level starts: as extra rows in the map's own Heck Settings
/// Setter window, or in IE's own window in Heck's play flow when the map has none.
/// Free of Heck types so the rest of IE works without Heck; the Heck hooks fill it in.
/// </summary>
internal class SettingsSetterPrompt
{
    private readonly PluginConfig config;
    private readonly HeckSettingsSetter heckSettingsSetter;
    private readonly HashSet<ChangeKind> accepted = new();

    private BeatmapKey? offeredKey;

    public SettingsSetterPrompt(PluginConfig config, HeckSettingsSetter heckSettingsSetter)
    {
        this.config = config;
        this.heckSettingsSetter = heckSettingsSetter;
    }

    /// <summary>The Heck hook is active, so the "show in Settings Setter" option can be used.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Whether rows can be added to the map's Settings Setter window (custom maps only); set by the Heck hook.</summary>
    public Func<BeatmapLevel, BeatmapKey, bool> CanOfferFor { get; set; } = (_, _) => false;

    /// <summary>The map has its own Settings Setter, whose window appears anyway: IE's rows are added to it.</summary>
    public bool ShowsInMapWindow(BeatmapLevel level, BeatmapKey key) =>
        IsAvailable && CanOfferFor(level, key) && heckSettingsSetter.GetControlledSettings(level, key).Present;

    /// <summary>The option is on and the map has no Settings Setter: IE shows its own window instead.</summary>
    public bool ShowsOwnWindow(BeatmapLevel level, BeatmapKey key) =>
        IsAvailable && config.ShowInSettingsSetter && !heckSettingsSetter.GetControlledSettings(level, key).Present;

    /// <summary>IE's changes for this map are shown before the level starts, so the player decides on them.</summary>
    public bool WillShowChangesFor(BeatmapLevel level, BeatmapKey key) =>
        ShowsInMapWindow(level, key) || ShowsOwnWindow(level, key);

    public void Offer(BeatmapKey key)
    {
        offeredKey = key;
        accepted.Clear();
    }

    public void Accept(ChangeKind kind) => accepted.Add(kind);

    /// <summary>
    /// If the window offered this launch's changes, keeps only the ones the player accepted
    /// (none when declined). Launches that never showed the window are left as they are.
    /// </summary>
    public LaunchPlan ApplyDecision(BeatmapKey key, LaunchPlan plan)
    {
        if (offeredKey is not { } offered)
            return plan;

        offeredKey = null;
        return offered.Equals(key) ? plan.Only(accepted.Contains) : plan;
    }
}
