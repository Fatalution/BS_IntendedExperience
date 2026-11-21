using System.Collections.Generic;

namespace IntendedExperience.Core;

/// <summary>The setting overrides one level launch will apply.</summary>
internal record struct LaunchPlan(
    bool EnableAllEffects,
    bool UseNotesOnlyColors,
    bool EnableMapEffectColors,
    bool SetFullBrightness)
{
    public bool IsEmpty => !EnableAllEffects && !UseNotesOnlyColors && !EnableMapEffectColors && !SetFullBrightness;

    /// <summary>One bullet line per setting, matching the names in Mod Settings.</summary>
    public string DescribeChanges()
    {
        var changes = new List<string>();
        if (EnableAllEffects) changes.Add("• Enable All Effects");
        if (UseNotesOnlyColors || EnableMapEffectColors) changes.Add("• Use Intended Colors");
        if (SetFullBrightness) changes.Add("• Set Effects Brightness to 100%");
        return string.Join("\n", changes);
    }
}
