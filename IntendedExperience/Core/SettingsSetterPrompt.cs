using System;
using System.Collections.Generic;
using IntendedExperience.Compat;

namespace IntendedExperience.Core;

/// <summary>
/// The player's answer to IE's changes shown in Heck's Settings Setter window.
/// Free of Heck types so the rest of IE works without Heck; the Heck hook fills it in.
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

    /// <summary>Whether the window can show IE's changes for this map (custom maps only); set by the Heck hook.</summary>
    public Func<BeatmapLevel, BeatmapKey, bool> CanOfferFor { get; set; } = (_, _) => false;

    /// <summary>
    /// IE's changes for this map go into the Settings Setter window: when the option is on, or when the map has its
    /// own Settings Setter, since that window appears anyway.
    /// </summary>
    public bool WillShowChangesFor(BeatmapLevel level, BeatmapKey key) =>
        IsAvailable && CanOfferFor(level, key)
        && (config.ShowInSettingsSetter || heckSettingsSetter.GetControlledSettings(level, key).Present);

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
