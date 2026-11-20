namespace IntendedExperience.Settings;

internal sealed class SettingsSnapshot
{
    public EnvironmentEffectsFilterPreset EffectsFilter { get; set; }
    public bool OverrideEnvironments { get; set; }
    public bool OverrideDefaultColors { get; set; }
    public bool ChromaEnabled { get; set; }
    public float? BSPlusBrightness { get; set; }
}
