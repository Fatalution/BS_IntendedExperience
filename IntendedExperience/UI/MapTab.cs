using System;
using System.Collections.Generic;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IntendedExperience.Core;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Gameplay setup tab: shows what the selected map's launch changes and lets the player exclude it.</summary>
internal class MapTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string TabName = "Intended Exp.";
    private const string ResourcePath = "IntendedExperience.UI.MapTab.bsml";

    private readonly GameplaySetup gameplaySetup;
    private readonly LaunchPreview launchPreview;
    private readonly MapExclusions mapExclusions;

    public MapTab(GameplaySetup gameplaySetup, LaunchPreview launchPreview, MapExclusions mapExclusions)
    {
        this.gameplaySetup = gameplaySetup;
        this.launchPreview = launchPreview;
        this.mapExclusions = mapExclusions;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [UIValue("has-level")]
    public bool HasLevel => launchPreview.Level != null;

    [UIValue("excluded")]
    public bool Excluded
    {
        get => launchPreview.Level != null && mapExclusions.IsExcluded(launchPreview.Level.levelID);
        set
        {
            if (launchPreview.Level != null)
                mapExclusions.SetExcluded(launchPreview.Level.levelID, value);
        }
    }

    [UIValue("status-text")]
    public string StatusText
    {
        get
        {
            if (launchPreview.Level == null)
                return "No map selected.";
            if (Excluded)
                return "Excluded: your own settings will be used.";

            var plan = launchPreview.Plan;
            if (plan.IsEmpty)
                return "Nothing to change for this map.";

            var changes = new List<string>();
            if (plan.EnableLights) changes.Add("• Enable lights");
            if (plan.UseNotesOnlyColors) changes.Add("• Notes Only color override");
            if (plan.EnableMapEffectColors) changes.Add("• Map effect colors");
            return "<color=#FF9933>This launch will apply:</color>\n" + string.Join("\n", changes);
        }
    }

    public void Initialize()
    {
        gameplaySetup.AddTab(TabName, ResourcePath, this);
        launchPreview.Changed += NotifyAllChanged;
    }

    public void Dispose()
    {
        launchPreview.Changed -= NotifyAllChanged;
        gameplaySetup.RemoveTab(TabName);
    }

    private void NotifyAllChanged()
    {
        NotifyChanged(nameof(HasLevel));
        NotifyChanged(nameof(Excluded));
        NotifyChanged(nameof(StatusText));
    }

    private void NotifyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
