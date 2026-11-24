using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Heck;
using Heck.PlayView;
using IntendedExperience.Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IntendedExperience.Compat;

/// <summary>
/// IE's own window in Heck's play flow, shown when the map has no Settings Setter and the player turned the
/// option on (maps with one get IE's rows in Heck's window instead). Built like Heck's Settings Setter view.
/// Only loaded when Heck is installed.
/// </summary>
[UsedImplicitly]
[PlayViewControllerSettings(1, "Intended Experience")]
internal class IntendedChangesView : BSMLResourceViewController, IPlayViewController
{
    private const string ContentResourcePath = "IntendedExperience.UI.IntendedChangesContent.bsml";

    // Heck raises its header the same way so it sits above the rows panel.
    private const float HeaderAnchorMaxY = 1.55f;

    private readonly List<SettingChange> changes = new();

    private LaunchPlanner launchPlanner = null!;
    private SettingsSetterPrompt prompt = null!;
    private BSMLParser bsmlParser = null!;
    private string contentTemplate = string.Empty;

    [UIComponent("top-vertical")]
    private readonly VerticalLayoutGroup topVertical = null!;
    [UIObject("content-object")]
    private readonly GameObject? contentObject = null;

    public event Action? Finished;

    public override string ResourceName => "IntendedExperience.UI.IntendedChangesView.bsml";

    [UIValue("rows")]
    private List<object> Rows => changes.Select(change => (object)new ChangeRow(change)).ToList();

    [Inject]
    [UsedImplicitly]
    private void Construct(LaunchPlanner launchPlanner, SettingsSetterPrompt prompt, BSMLParser bsmlParser)
    {
        this.launchPlanner = launchPlanner;
        this.prompt = prompt;
        this.bsmlParser = bsmlParser;
        contentTemplate = Utilities.GetResourceContent(typeof(IntendedChangesView).Assembly, ContentResourcePath);
    }

    /// <summary>Called by Heck when Play is pressed; returning false skips this window.</summary>
    public bool Init(StartStandardLevelParameters parameters)
    {
        // IE doesn't apply to vanilla multiplayer launches.
        if (parameters is StartMultiplayerLevelParameters)
            return false;

        var map = parameters.Map();
        if (!prompt.ShowsOwnWindow(map))
            return false;

        changes.Clear();
        changes.AddRange(launchPlanner.Plan(map, parameters.Settings()).Changes());
        if (changes.Count == 0)
            return false;

        prompt.Offer(map);

        // Like Heck, parse the rows panel fresh for every map so it lays out correctly.
        if (contentObject != null)
            Destroy(contentObject);
        bsmlParser.Parse(contentTemplate, gameObject, this);
        return true;
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        if (!addedToHierarchy)
            return;

        var header = (RectTransform)topVertical.transform;
        header.anchorMax = new Vector2(header.anchorMax.x, HeaderAnchorMaxY);
    }

    [UsedImplicitly]
    [UIAction("accept-click")]
    private void AcceptClick()
    {
        foreach (var change in changes)
            prompt.Accept(change.Kind);
        Finished?.Invoke();
    }

    // Nothing accepted, so IE applies none of its changes for this launch.
    [UsedImplicitly]
    [UIAction("decline-click")]
    private void DeclineClick() => Finished?.Invoke();

    /// <summary>Host for one row: setting name on the left, "from -> to" on the right.</summary>
    private class ChangeRow
    {
        [UIValue("setting")]
        public string Setting { get; }

        [UIValue("change")]
        public string Change { get; }

        public ChangeRow(SettingChange change)
        {
            Setting = change.Setting;
            Change = $"{change.From} -> {change.To}";
        }
    }
}
