using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IntendedExperience.Core;

/// <summary>One kind of setting IE can change for a launch.</summary>
internal enum ChangeKind
{
    EnvironmentEffects,
    EnvironmentOverride,
    ColorOverride,
    MapEnvironmentColors,
    EffectsBrightness
}

/// <summary>One setting change, shown the way Heck's Settings Setter shows its rows.</summary>
internal record struct SettingChange(ChangeKind Kind, string Setting, string From, string To)
{
    public override string ToString() => $"{Setting}: {From} -> {To}";
}

/// <summary>The setting overrides one level launch will apply.</summary>
internal record struct LaunchPlan(
    bool EnableAllEffects,
    bool UseMapEnvironment,
    bool UseNotesOnlyColors,
    bool EnableMapEffectColors,
    bool SetFullBrightness,
    EnvironmentEffectsFilterPreset CurrentEffects,
    string? CurrentEnvironment,
    string? MapEnvironment,
    float CurrentBrightness)
{
    public bool IsEmpty => !EnableAllEffects && !UseMapEnvironment && !UseNotesOnlyColors && !EnableMapEffectColors
                           && !SetFullBrightness;

    public IReadOnlyList<SettingChange> Changes()
    {
        var changes = new List<SettingChange>();
        if (EnableAllEffects)
            changes.Add(new(ChangeKind.EnvironmentEffects, "Effects", EffectsName(CurrentEffects), "All Effects"));
        if (UseMapEnvironment)
            changes.Add(new(ChangeKind.EnvironmentOverride, "Environment", CurrentEnvironment ?? "Override", MapEnvironment ?? "Map's own"));
        if (UseNotesOnlyColors)
            changes.Add(new(ChangeKind.ColorOverride, "Color Override", "All", "Notes Only"));
        if (EnableMapEffectColors)
            changes.Add(new(ChangeKind.MapEnvironmentColors, "Map Environment Colors", "Off", "On"));
        if (SetFullBrightness)
            changes.Add(new(ChangeKind.EffectsBrightness, "Effects Brightness", $"{Mathf.RoundToInt(CurrentBrightness * 100)}%", "100%"));
        return changes;
    }

    /// <summary>One line per change, e.g. "Color Override: All -> Notes Only".</summary>
    public string DescribeChanges() => string.Join("\n", Changes().Select(change => change.ToString()));

    /// <summary>This plan limited to the changes the player accepted.</summary>
    public LaunchPlan Only(Func<ChangeKind, bool> accepted) => this with
    {
        EnableAllEffects = EnableAllEffects && accepted(ChangeKind.EnvironmentEffects),
        UseMapEnvironment = UseMapEnvironment && accepted(ChangeKind.EnvironmentOverride),
        UseNotesOnlyColors = UseNotesOnlyColors && accepted(ChangeKind.ColorOverride),
        EnableMapEffectColors = EnableMapEffectColors && accepted(ChangeKind.MapEnvironmentColors),
        SetFullBrightness = SetFullBrightness && accepted(ChangeKind.EffectsBrightness)
    };

    private static string EffectsName(EnvironmentEffectsFilterPreset preset) => preset switch
    {
        EnvironmentEffectsFilterPreset.NoEffects => "Off",
        EnvironmentEffectsFilterPreset.StrobeFilter => "No Flickering",
        _ => "All Effects"
    };
}
