using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IntendedExperience.Core;
using JetBrains.Annotations;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Gameplay setup tab: the mod's on/off switch and what launching the selected map changes.</summary>
[UsedImplicitly]
internal class MapTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string TabName = "Intended Experience";
    private const string ResourcePath = "IntendedExperience.UI.MapTab.bsml";

    // IE only applies to solo-style launches (Solo, Party, and mod flows like Multiplayer+), not Online or Campaign.
    private const MenuType TabMenus = MenuType.Solo | MenuType.Custom;

    private readonly GameplaySetup gameplaySetup;
    private readonly LaunchPreview launchPreview;
    private readonly MasterSwitch masterSwitch;

    public event PropertyChangedEventHandler? PropertyChanged;

    [UIValue("enabled")]
    public bool Enabled
    {
        get => masterSwitch.Enabled;
        set => masterSwitch.Enabled = value;
    }

    [UIValue("status-text")]
    public string StatusText
    {
        get
        {
            if (!masterSwitch.Enabled)
                return "Intended Experience is turned off: your own settings will be used.";
            if (launchPreview.Map == null)
                return "No map selected.";
            if (!launchPreview.Qualifies)
                return "Not a GLS map, Chroma map or modchart: your own settings will be used.";
            if (launchPreview.Excluded)
                return "Excluded with the IE button: your own settings will be used.";

            var plan = launchPreview.Plan;
            if (plan.IsEmpty)
                return "Nothing to change for this map.";

            return "<color=#FF9933>This launch will apply:</color>\n" + plan.DescribeChanges();
        }
    }

    public MapTab(GameplaySetup gameplaySetup, LaunchPreview launchPreview, MasterSwitch masterSwitch)
    {
        this.gameplaySetup = gameplaySetup;
        this.launchPreview = launchPreview;
        this.masterSwitch = masterSwitch;
    }

    public void Initialize()
    {
        gameplaySetup.AddTab(TabName, ResourcePath, this, TabMenus);
        launchPreview.Changed += NotifyAllChanged;
        masterSwitch.Changed += NotifyAllChanged;
    }

    public void Dispose()
    {
        masterSwitch.Changed -= NotifyAllChanged;
        launchPreview.Changed -= NotifyAllChanged;
        gameplaySetup.RemoveTab(TabName);
    }

    private void NotifyAllChanged()
    {
        NotifyChanged(nameof(Enabled));
        NotifyChanged(nameof(StatusText));
    }

    private void NotifyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
