using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Gameplay setup tab: shows what the selected map's launch changes and lets the player exclude it.</summary>
internal class MapTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string TabName = "Intended Experience";
    private const string ResourcePath = "IntendedExperience.UI.MapTab.bsml";

    private readonly GameplaySetup gameplaySetup;
    private readonly LaunchPreview launchPreview;

    public MapTab(GameplaySetup gameplaySetup, LaunchPreview launchPreview)
    {
        this.gameplaySetup = gameplaySetup;
        this.launchPreview = launchPreview;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [UIValue("qualifies")]
    public bool Qualifies => launchPreview.Qualifies;

    [UIValue("excluded")]
    public bool Excluded
    {
        get => launchPreview.Excluded;
        set => launchPreview.SetExcluded(value);
    }

    [UIValue("status-text")]
    public string StatusText
    {
        get
        {
            if (launchPreview.Level == null)
                return "No map selected.";
            if (!Qualifies)
                return "Not a GLS map or modchart: your own settings will be used.";
            if (Excluded)
                return "Excluded: your own settings will be used.";

            var plan = launchPreview.Plan;
            if (plan.IsEmpty)
                return "Nothing to change for this map.";

            return "<color=#FF9933>This launch will apply:</color>\n" + plan.DescribeChanges();
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
        NotifyChanged(nameof(Qualifies));
        NotifyChanged(nameof(Excluded));
        NotifyChanged(nameof(StatusText));
    }

    private void NotifyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
