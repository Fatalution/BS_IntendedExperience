using System.Collections.Generic;

namespace IntendedExperience.Core;

/// <summary>The setting overrides one level launch will apply.</summary>
internal record struct LaunchPlan(bool EnableLights, bool UseNotesOnlyColors, bool EnableMapEffectColors)
{
    public bool IsEmpty => !EnableLights && !UseNotesOnlyColors && !EnableMapEffectColors;

    /// <summary>One bullet line per override, for menus and hints.</summary>
    public string DescribeChanges()
    {
        var changes = new List<string>();
        if (EnableLights) changes.Add("• Enable lights");
        if (UseNotesOnlyColors) changes.Add("• Notes Only color override");
        if (EnableMapEffectColors) changes.Add("• Map effect colors");
        return string.Join("\n", changes);
    }
}
