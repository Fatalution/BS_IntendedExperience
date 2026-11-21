using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Small button beside Play that toggles whether the selected map is excluded.</summary>
internal class ExcludeButton : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string ResourcePath = "IntendedExperience.UI.ExcludeButton.bsml";
    private const string ActiveText = "<color=#FF9933>IE</color>";
    private const string ExcludedText = "<color=#808080><s>IE</s></color>";
    private const string ActiveHint = "Click to disable IE for this map. This will be saved";
    private const string ExcludedHint = "Click to enable IE for this map. This will be saved";

    private readonly BSMLParser bsmlParser;
    private readonly LaunchPreview launchPreview;
    private readonly StandardLevelDetailViewController levelDetailViewController;

    [UIObject("exclude-button")]
    private readonly GameObject excludeButton = null!;

    public ExcludeButton(BSMLParser bsmlParser, LaunchPreview launchPreview,
        StandardLevelDetailViewController levelDetailViewController)
    {
        this.bsmlParser = bsmlParser;
        this.launchPreview = launchPreview;
        this.levelDetailViewController = levelDetailViewController;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [UIValue("button-text")]
    public string ButtonText => launchPreview.Excluded ? ExcludedText : ActiveText;

    public void Initialize()
    {
        var levelDetailView = levelDetailViewController.GetComponentInChildren<StandardLevelDetailView>(true);
        var buttonRow = levelDetailView.actionButton.transform.parent.gameObject;
        bsmlParser.Parse(Utilities.GetResourceContent(typeof(ExcludeButton).Assembly, ResourcePath), buttonRow, this);
        launchPreview.Changed += Refresh;
        Refresh();
    }

    public void Dispose() => launchPreview.Changed -= Refresh;

    [UsedImplicitly]
    [UIAction("toggle-exclusion")]
    private void ToggleExclusion() => launchPreview.SetExcluded(!launchPreview.Excluded);

    // Only shown for maps the mod can affect (v3+ environments).
    private void Refresh()
    {
        if (excludeButton != null)
        {
            excludeButton.SetActive(launchPreview.Qualifies);
            var hoverHint = excludeButton.GetComponent<HoverHint>();
            if (hoverHint != null)
            {
                hoverHint.enabled = launchPreview.ShowTooltips;
                hoverHint.text = launchPreview.Excluded ? ExcludedHint : ActiveHint;
            }
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonText)));
    }
}
