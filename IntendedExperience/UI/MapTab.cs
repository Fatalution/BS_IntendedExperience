using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IntendedExperience.Core;
using JetBrains.Annotations;
using TMPro;
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

    // Same orange as the tinted Play button.
    private const string HighlightColor = "#FF9933";
    private const string NothingToChangeText = $"<color={HighlightColor}>Nothing to change.</color>";

    private readonly GameplaySetup gameplaySetup;
    private readonly LaunchPreview launchPreview;
    private readonly MasterSwitch masterSwitch;

    // Set by BSML when the tab is first shown.
    [UIComponent("changes-text")]
    private TextMeshProUGUI? changesTextView = null;
    private CenteredTextBlock? changesTextBlock;

    public event PropertyChangedEventHandler? PropertyChanged;

    [UIValue("enabled")]
    public bool Enabled
    {
        get => masterSwitch.Enabled;
        set => masterSwitch.Enabled = value;
    }

    // E.g. "GLS · Ranked".
    [UIValue("map-type-text")]
    public string MapTypeText
    {
        get
        {
            if (launchPreview.Map == null)
                return "No map selected.";

            string type = launchPreview.Category switch
            {
                MapCategory.Gls => "GLS",
                MapCategory.Modcharts => "Modchart",
                MapCategory.Chroma => "Chroma",
                _ => "Legacy"
            };
            return launchPreview.Ranked ? $"<b>{type}</b> · {RankedText.Title}" : $"<b>{type}</b>";
        }
    }

    // One centred line: what IE will do with this map.
    [UIValue("status-text")]
    public string StatusText
    {
        get
        {
            if (launchPreview.Map == null)
                return "";
            if (!masterSwitch.Enabled)
                return "Intended Experience is turned off.";
            if (launchPreview.SkippedRanked)
                return $"Turned off for {RankedText.Lower} {RankedMapsText} in settings.";
            if (!launchPreview.Qualifies)
                return NothingToChangeText;
            if (launchPreview.Excluded)
                return "Excluded with the IE button.";
            if (launchPreview.Plan.IsEmpty)
                return NothingToChangeText;

            return $"<color={HighlightColor}>This launch will apply:</color>";
        }
    }

    [UIValue("changes-text")]
    public string ChangesText => AppliesChanges ? launchPreview.Plan.DescribeChanges() : "";

    // The selected map's type as it follows "ranked".
    private string RankedMapsText => launchPreview.Category switch
    {
        MapCategory.Modcharts => "modcharts",
        MapCategory.Chroma => "Chroma maps",
        _ => "GLS maps"
    };

    private bool AppliesChanges => launchPreview.Map != null && masterSwitch.Enabled && launchPreview.Qualifies
                                   && !launchPreview.Excluded && !launchPreview.Plan.IsEmpty;

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

    [UIAction("#post-parse")]
    [UsedImplicitly]
    private void PostParse() => changesTextBlock = changesTextView!.gameObject.AddComponent<CenteredTextBlock>();

    private void NotifyAllChanged()
    {
        NotifyChanged(nameof(Enabled));
        NotifyChanged(nameof(MapTypeText));
        NotifyChanged(nameof(StatusText));
        NotifyChanged(nameof(ChangesText));
        changesTextBlock?.Refresh();
    }

    private void NotifyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
