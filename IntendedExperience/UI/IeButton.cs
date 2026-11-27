using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>
/// Small button beside Play that turns IE off (excludes) or, on ranked maps whose type is turned off for ranked maps,
/// on (includes) for the selected map.
/// </summary>
[UsedImplicitly]
internal class IeButton : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string ResourcePath = "IntendedExperience.UI.IeButton.bsml";
    private const string OnText = "<color=" + MenuText.HighlightColor + ">IE</color>";
    private const string OffText = "<color=" + MenuText.DimColor + "><s>IE</s></color>";
    private const string OnHint = "Click to disable IE for this map. This will be saved.";
    private const string OffHint = "Click to enable IE for this map. This will be saved.";

    private readonly BSMLParser bsmlParser;
    private readonly LaunchPreview launchPreview;
    private readonly StandardLevelDetailViewController levelDetailViewController;

    [UIObject("ie-button")]
    private readonly GameObject ieButton = null!;

    public event PropertyChangedEventHandler? PropertyChanged;

    [UIValue("button-text")]
    public string ButtonText => launchPreview.ButtonOn ? OnText : OffText;

    private string HintText => launchPreview.ButtonOn ? OnHint : OffHint;

    public IeButton(BSMLParser bsmlParser, LaunchPreview launchPreview,
        StandardLevelDetailViewController levelDetailViewController)
    {
        this.bsmlParser = bsmlParser;
        this.launchPreview = launchPreview;
        this.levelDetailViewController = levelDetailViewController;
    }

    public void Initialize()
    {
        var levelDetailView = levelDetailViewController.GetComponentInChildren<StandardLevelDetailView>(true);
        var buttonRow = levelDetailView.actionButton.transform.parent.gameObject;
        bsmlParser.Parse(Utilities.GetResourceContent(typeof(IeButton).Assembly, ResourcePath), buttonRow, this);
        launchPreview.Changed += Refresh;
        Refresh();
    }

    public void Dispose() => launchPreview.Changed -= Refresh;

    [UsedImplicitly]
    [UIAction("toggle")]
    private void Toggle() => launchPreview.ToggleButton();

    // Only shown for maps the mod can affect (GLS maps, Chroma maps and modcharts), including ranked maps whose
    // type is turned off for ranked maps.
    private void Refresh()
    {
        if (ieButton != null)
        {
            ieButton.SetActive(launchPreview.Qualifies || launchPreview.RankedTurnedOff);
            var hoverHint = ieButton.GetComponent<HoverHint>();
            if (hoverHint != null)
            {
                hoverHint.enabled = launchPreview.ShowTooltips;
                hoverHint.text = HintText;
            }
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonText)));
    }
}
