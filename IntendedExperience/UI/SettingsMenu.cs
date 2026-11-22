using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using IntendedExperience.Compat;
using IntendedExperience.Core;
using JetBrains.Annotations;
using SongCore;
using UnityEngine;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Settings -> Mod Settings page.</summary>
internal class SettingsMenu : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string MenuName = "Intended Experience";
    private const string ResourcePath = "IntendedExperience.UI.SettingsMenu.bsml";
    private const string RowResourcePath = "IntendedExperience.UI.ExcludedMapRow.bsml";
    private const string CustomLevelPrefix = "custom_level_";

    private readonly BSMLSettings bsmlSettings;
    private readonly BSMLParser bsmlParser;
    private readonly BeatmapLevelsModel beatmapLevelsModel;
    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly SettingsSetterPrompt settingsSetterPrompt;
    private readonly string rowTemplate;

    [UIObject("settings-body")]
    private readonly GameObject settingsBody = null!;
    [UIObject("excluded-rows")]
    private readonly GameObject excludedRows = null!;

    public SettingsMenu(BSMLSettings bsmlSettings, BSMLParser bsmlParser, BeatmapLevelsModel beatmapLevelsModel,
        PluginConfig config, MapExclusions mapExclusions, BeatSaberPlusLights beatSaberPlusLights,
        SettingsSetterPrompt settingsSetterPrompt)
    {
        this.bsmlSettings = bsmlSettings;
        this.bsmlParser = bsmlParser;
        this.beatmapLevelsModel = beatmapLevelsModel;
        this.config = config;
        this.mapExclusions = mapExclusions;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.settingsSetterPrompt = settingsSetterPrompt;
        rowTemplate = Utilities.GetResourceContent(typeof(SettingsMenu).Assembly, RowResourcePath);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- Settings ----
    [UIValue("enabled")]
    public bool Enabled
    {
        get => config.Enabled;
        set
        {
            config.Enabled = value;
            UpdateBodyVisibility();
        }
    }

    // ---- GLS rules ----
    [UIValue("gls-force-all-effects")]
    public bool GlsForceAllEffects { get => config.Gls.ForceAllEffects; set => config.Gls.ForceAllEffects = value; }

    [UIValue("gls-force-intended-colors")]
    public bool GlsForceIntendedColors { get => config.Gls.ForceIntendedColors; set => config.Gls.ForceIntendedColors = value; }

    [UIValue("gls-force-full-brightness")]
    public bool GlsForceFullBrightness { get => config.Gls.ForceFullBrightness; set => config.Gls.ForceFullBrightness = value; }

    // ---- Modchart rules ----
    [UIValue("modcharts-force-all-effects")]
    public bool ModchartsForceAllEffects { get => config.Modcharts.ForceAllEffects; set => config.Modcharts.ForceAllEffects = value; }

    [UIValue("modcharts-force-intended-colors")]
    public bool ModchartsForceIntendedColors { get => config.Modcharts.ForceIntendedColors; set => config.Modcharts.ForceIntendedColors = value; }

    [UIValue("modcharts-force-full-brightness")]
    public bool ModchartsForceFullBrightness { get => config.Modcharts.ForceFullBrightness; set => config.Modcharts.ForceFullBrightness = value; }

    // ---- Menu ----
    [UIValue("tint-play-button")]
    public bool TintPlayButton { get => config.TintPlayButton; set => config.TintPlayButton = value; }

    [UIValue("show-tooltips")]
    public bool ShowTooltips { get => config.ShowTooltips; set => config.ShowTooltips = value; }

    [UIValue("show-in-settings-setter")]
    public bool ShowInSettingsSetter { get => config.ShowInSettingsSetter; set => config.ShowInSettingsSetter = value; }

    // ---- Optional mods (grey out their toggles when missing) ----
    [UIValue("beat-saber-plus-available")]
    public bool BeatSaberPlusAvailable => beatSaberPlusLights.IsAvailable;

    [UIValue("settings-setter-available")]
    public bool SettingsSetterAvailable => settingsSetterPrompt.IsAvailable;

    // ---- Exclusions ----
    [UIValue("exclusions-text")]
    public string ExclusionsText => mapExclusions.LevelIds.Count == 0
        ? "Excluded maps: none"
        : $"Excluded maps: {mapExclusions.LevelIds.Count}";

    // ---- Footer ----
    [UIValue("version-text")]
    public string VersionText => $"v{Plugin.Version}";

    public void Initialize()
    {
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, this);
        mapExclusions.Changed += OnExclusionsChanged;
        Loader.SongsLoadedEvent += OnSongsLoaded;
    }

    public void Dispose()
    {
        Loader.SongsLoadedEvent -= OnSongsLoaded;
        mapExclusions.Changed -= OnExclusionsChanged;
        bsmlSettings.RemoveSettingsMenu(this);
    }

    [UsedImplicitly]
    [UIAction("#post-parse")]
    private void PostParse()
    {
        UpdateBodyVisibility();
        OnExclusionsChanged();
    }

    // Everything below the master switch is hidden while the mod is disabled.
    private void UpdateBodyVisibility()
    {
        if (settingsBody != null)
            settingsBody.SetActive(config.Enabled);
    }

    [UsedImplicitly]
    [UIAction("clear-exclusions")]
    private void ClearExclusions() => mapExclusions.Clear();

    // Custom map names are only known once SongCore has loaded the songs.
    private void OnSongsLoaded(Loader loader, ConcurrentDictionary<string, BeatmapLevel> levels) => OnExclusionsChanged();

    private void OnExclusionsChanged()
    {
        RebuildRows();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExclusionsText)));
    }

    // One plain row per map inside the page's own scroll view; a nested list view doesn't lay out there.
    private void RebuildRows()
    {
        if (excludedRows == null)
            return;

        foreach (Transform row in excludedRows.transform)
        {
            row.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(row.gameObject);
        }

        var rows = mapExclusions.LevelIds
            .Select(levelId => new ExcludedMapRow(levelId, DisplayName(levelId), mapExclusions))
            .OrderBy(row => row.MapName, StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
            bsmlParser.Parse(rowTemplate, excludedRows, row);
    }

    // Maps that aren't loaded (deleted, or SongCore still loading) fall back to their hash.
    private string DisplayName(string levelId)
    {
        var level = beatmapLevelsModel.GetBeatmapLevel(levelId, true);
        if (level == null)
            return $"<color=#808080>Unknown map ({levelId.Replace(CustomLevelPrefix, string.Empty)})</color>";

        string mappers = level.allMappers.Length > 0 ? $" <color=#808080>[{string.Join(", ", level.allMappers)}]</color>" : string.Empty;
        return $"{level.songName} <size=80%>- {level.songAuthorName}</size>{mappers}";
    }

    /// <summary>Host for one excluded-map row.</summary>
    private class ExcludedMapRow
    {
        private readonly string levelId;
        private readonly MapExclusions mapExclusions;

        public ExcludedMapRow(string levelId, string mapName, MapExclusions mapExclusions)
        {
            this.levelId = levelId;
            this.mapExclusions = mapExclusions;
            MapName = mapName;
        }

        [UIValue("map-name")]
        public string MapName { get; }

        [UsedImplicitly]
        [UIAction("remove")]
        private void Remove() => mapExclusions.SetExcluded(levelId, false);
    }
}
