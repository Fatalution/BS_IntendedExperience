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

    private MapDifficulty? offeredMap;

    public SettingsSetterPrompt(PluginConfig config, HeckSettingsSetter heckSettingsSetter)
    {
        this.config = config;
        this.heckSettingsSetter = heckSettingsSetter;
    }

    /// <summary>The Heck hook is active, so the "show in Settings Setter" option can be used.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Whether rows can be added to the map's Settings Setter window (custom maps only); set by the Heck hook.</summary>
    public Func<MapDifficulty, bool> CanOfferFor { get; set; } = _ => false;

    /// <summary>
    /// Applies the map's Settings Setter without its window, as if accepted, for launches that skip Heck's play
    /// flow. Returns whether it ran. Set by the Heck hook.
    /// </summary>
    public Func<MapDifficulty, LaunchSettings, bool> ApplyWithoutWindow { get; set; } = (_, _) => false;

    /// <summary>Undoes what <see cref="ApplyWithoutWindow"/> changed outside the launch settings, after the level.</summary>
    public Action RestoreAppliedWithoutWindow { get; set; } = () => { };

    /// <summary>The map has its own Settings Setter, whose window appears anyway: IE's rows are added to it.</summary>
    public bool ShowsInMapWindow(MapDifficulty map) =>
        IsAvailable && CanOfferFor(map) && heckSettingsSetter.GetControlledSettings(map).Present;

    /// <summary>The option is on and the map has no Settings Setter: IE shows its own window instead.</summary>
    public bool ShowsOwnWindow(MapDifficulty map) =>
        IsAvailable && config.ShowInSettingsSetter && !heckSettingsSetter.GetControlledSettings(map).Present;

    /// <summary>IE's changes for this map are shown before the level starts, so the player decides on them.</summary>
    public bool WillShowChangesFor(MapDifficulty map) => ShowsInMapWindow(map) || ShowsOwnWindow(map);

    public void Offer(MapDifficulty map)
    {
        offeredMap = map;
        accepted.Clear();
    }

    public void Accept(ChangeKind kind) => accepted.Add(kind);

    /// <summary>Drops a pending offer, so the next launch applies IE's changes without asking.</summary>
    public void Withdraw()
    {
        offeredMap = null;
        accepted.Clear();
    }

    /// <summary>
    /// If the window offered this launch's changes, keeps only the ones the player accepted
    /// (none when declined). Launches that never showed the window are left as they are.
    /// </summary>
    public LaunchPlan ApplyDecision(MapDifficulty map, LaunchPlan plan)
    {
        if (offeredMap is not { } offered)
            return plan;

        offeredMap = null;
        return offered.Equals(map) ? plan.Only(accepted.Contains) : plan;
    }
}
