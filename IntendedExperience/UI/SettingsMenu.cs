using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using IntendedExperience.Core;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Settings -> Mod Settings page.</summary>
internal class SettingsMenu : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string MenuName = "Intended Experience";
    private const string ResourcePath = "IntendedExperience.UI.SettingsMenu.bsml";

    private readonly BSMLSettings bsmlSettings;
    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;

    public SettingsMenu(BSMLSettings bsmlSettings, PluginConfig config, MapExclusions mapExclusions)
    {
        this.bsmlSettings = bsmlSettings;
        this.config = config;
        this.mapExclusions = mapExclusions;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- Settings ----
    [UIValue("enabled")]
    public bool Enabled { get => config.Enabled; set => config.Enabled = value; }

    [UIValue("force-lights")]
    public bool ForceLights { get => config.ForceLights; set => config.ForceLights = value; }

    [UIValue("force-notes-only")]
    public bool ForceNotesOnlyColors { get => config.ForceNotesOnlyColors; set => config.ForceNotesOnlyColors = value; }

    [UIValue("force-map-effect-colors")]
    public bool ForceMapEffectColors { get => config.ForceMapEffectColors; set => config.ForceMapEffectColors = value; }

    [UIValue("legacy-map-effect-colors")]
    public bool MapEffectColorsOnLegacyEnvironments
    {
        get => config.MapEffectColorsOnLegacyEnvironments;
        set => config.MapEffectColorsOnLegacyEnvironments = value;
    }

    [UIValue("tint-play-button")]
    public bool TintPlayButton { get => config.TintPlayButton; set => config.TintPlayButton = value; }

    // ---- Exclusions ----
    [UIValue("exclusions-text")]
    public string ExclusionsText => $"Excluded maps: {mapExclusions.Count}";

    public void Initialize()
    {
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, this);
        mapExclusions.Changed += NotifyExclusionsChanged;
    }

    public void Dispose()
    {
        mapExclusions.Changed -= NotifyExclusionsChanged;
        bsmlSettings.RemoveSettingsMenu(this);
    }

    [UIAction("#post-parse")]
    private void PostParse() => NotifyExclusionsChanged();

    [UIAction("clear-exclusions")]
    private void ClearExclusions() => mapExclusions.Clear();

    private void NotifyExclusionsChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExclusionsText)));
}
