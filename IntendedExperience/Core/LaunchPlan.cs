using System.Collections.Generic;

namespace IntendedExperience.Core;

/// <summary>The setting overrides one level launch will apply.</summary>
internal record struct LaunchPlan(bool EnableAllEffects, bool UseNotesOnlyColors, bool EnableMapEffectColors)
{
    public bool IsEmpty => !EnableAllEffects && !UseNotesOnlyColors && !EnableMapEffectColors;

    /// <summary>One bullet line per setting, matching the names in Mod Settings.</summary>
    public string DescribeChanges()
    {
        var changes = new List<string>();
        if (EnableAllEffects) changes.Add("• Enable All Effects");
        if (UseNotesOnlyColors || EnableMapEffectColors) changes.Add("• Use Intended Colors");
        return string.Join("\n", changes);
    }
}
