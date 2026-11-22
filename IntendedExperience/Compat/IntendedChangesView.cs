using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Heck;
using Heck.PlayView;
using IntendedExperience.Core;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace IntendedExperience.Compat;

/// <summary>
/// IE's own window in Heck's play flow, shown when the map has no Settings Setter and the player turned the
/// option on (maps with one get IE's rows in Heck's window instead). Only loaded when Heck is installed.
/// </summary>
[PlayViewControllerSettings(1, "Intended Experience")]
internal class IntendedChangesView : BSMLResourceViewController, IPlayViewController
{
    private const string RowResourcePath = "IntendedExperience.UI.IntendedChangesRow.bsml";

    private readonly List<SettingChange> changes = new();

    private LaunchPlanner launchPlanner = null!;
    private SettingsSetterPrompt prompt = null!;
    private BSMLParser bsmlParser = null!;
    private string rowTemplate = string.Empty;

    [UIObject("rows")]
    private readonly GameObject rows = null!;

    public event Action? Finished;

    public override string ResourceName => "IntendedExperience.UI.IntendedChangesView.bsml";

    [Inject]
    [UsedImplicitly]
    private void Construct(LaunchPlanner launchPlanner, SettingsSetterPrompt prompt, BSMLParser bsmlParser)
    {
        this.launchPlanner = launchPlanner;
        this.prompt = prompt;
        this.bsmlParser = bsmlParser;
        rowTemplate = Utilities.GetResourceContent(typeof(IntendedChangesView).Assembly, RowResourcePath);
    }

    /// <summary>Called by Heck when Play is pressed; returning false skips this window.</summary>
    public bool Init(StartStandardLevelParameters parameters)
    {
        // IE doesn't apply to vanilla multiplayer launches.
        if (parameters is StartMultiplayerLevelParameters)
            return false;

        var level = parameters.BeatmapLevel;
        var key = parameters.BeatmapKey;
        if (!prompt.ShowsOwnWindow(level, key))
            return false;

        changes.Clear();
        changes.AddRange(launchPlanner
            .Plan(level, key, parameters.PlayerSpecificSettings, parameters.PlayerOverrideLightshowColors)
            .Changes());
        if (changes.Count == 0)
            return false;

        prompt.Offer(key);
        RebuildRows();
        return true;
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        RebuildRows();
    }

    [UIAction("accept-click")]
    private void AcceptClick()
    {
        foreach (var change in changes)
            prompt.Accept(change.Kind);
        Finished?.Invoke();
    }

    // Nothing accepted, so IE applies none of its changes for this launch.
    [UIAction("decline-click")]
    private void DeclineClick() => Finished?.Invoke();

    // The view is parsed on first activation, after the first Init; rows are rebuilt whenever both are ready.
    private void RebuildRows()
    {
        if (rows == null)
            return;

        foreach (Transform row in rows.transform)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }

        foreach (var change in changes)
            bsmlParser.Parse(rowTemplate, rows, new ChangeRow(change));
    }

    /// <summary>Host for one row: setting name on the left, "from -> to" on the right.</summary>
    private class ChangeRow
    {
        public ChangeRow(SettingChange change)
        {
            Setting = change.Setting;
            Change = $"{change.From} -> {change.To}";
        }

        [UIValue("setting")]
        public string Setting { get; }

        [UIValue("change")]
        public string Change { get; }
    }
}
