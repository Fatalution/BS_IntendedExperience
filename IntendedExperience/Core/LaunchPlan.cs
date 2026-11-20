namespace IntendedExperience.Core;

/// <summary>The setting overrides one level launch will apply.</summary>
internal record struct LaunchPlan(bool EnableLights, bool UseNotesOnlyColors, bool EnableMapEffectColors)
{
    public bool IsEmpty => !EnableLights && !UseNotesOnlyColors && !EnableMapEffectColors;
}
