using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using HMUI;
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
#if GAME_1_29
    // BSML 1.6's mod list is too narrow for the name with a space.
    private const string MenuName = "IntendedExperience";
#else
    private const string MenuName = "Intended Experience";
#endif
    private const string ResourcePath = "IntendedExperience.UI.SettingsMenu.bsml";
    private const string RowResourcePath = "IntendedExperience.UI.ExcludedMapRow.bsml";
    private const string CustomLevelPrefix = "custom_level_";

    // ---- Map type switch: cells in MapCategory order ----
    private static readonly List<object> CategoryNames = new() { "GLS Maps", "Modcharts", "Chroma Maps" };
    private static readonly string[] RuleProperties =
    {
        nameof(ForceAllEffects), nameof(ForceIntendedColors), nameof(ForceMapEnvironment), nameof(ForceFullBrightness)
    };

    // ---- Services ----
    private readonly BSMLSettings bsmlSettings;
    private readonly BSMLParser bsmlParser;
    private readonly BeatmapLevelsModel beatmapLevelsModel;
    private readonly PluginConfig config;
    private readonly MasterSwitch masterSwitch;
    private readonly MapExclusions mapExclusions;
    private readonly BeatSaberPlusLights beatSaberPlusLights;
    private readonly SettingsSetterPrompt settingsSetterPrompt;
    private readonly string rowTemplate;

    // ---- Parsed objects ----
    [UIObject("settings-body")]
    private readonly GameObject settingsBody = null!;
    [UIObject("colors-toggle")]
    private readonly GameObject colorsToggle = null!;
    [UIObject("excluded-rows")]
    private readonly GameObject excludedRows = null!;

    private MapCategory selectedCategory = MapCategory.Gls;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- Settings ----
    [UIValue("enabled")]
    public bool Enabled
    {
        get => masterSwitch.Enabled;
        set => masterSwitch.Enabled = value;
    }

    // ---- Rules of the map type picked in the switch ----
    [UIValue("categories")]
    public List<object> Categories => CategoryNames;

    [UIValue("force-all-effects")]
    public bool ForceAllEffects { get => SelectedRules.ForceAllEffects; set => SelectedRules.ForceAllEffects = value; }

    [UIValue("force-intended-colors")]
    public bool ForceIntendedColors { get => SelectedRules.ForceIntendedColors; set => SelectedRules.ForceIntendedColors = value; }

    [UIValue("force-map-environment")]
    public bool ForceMapEnvironment { get => SelectedRules.ForceMapEnvironment; set => SelectedRules.ForceMapEnvironment = value; }

    [UIValue("force-full-brightness")]
    public bool ForceFullBrightness { get => SelectedRules.ForceFullBrightness; set => SelectedRules.ForceFullBrightness = value; }

    // ---- Other launchers ----
    [UIValue("apply-to-replays")]
    public bool ApplyToReplays { get => config.ApplyToReplays; set => config.ApplyToReplays = value; }

    [UIValue("apply-in-multiplayer-plus")]
    public bool ApplyInMultiplayerPlus { get => config.ApplyInMultiplayerPlus; set => config.ApplyInMultiplayerPlus = value; }

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

    [UIValue("multiplayer-plus-available")]
    public bool MultiplayerPlusAvailable => InstalledMods.MultiplayerPlus;

    // ---- Exclusions ----
    [UIValue("exclusions-text")]
    public string ExclusionsText => mapExclusions.LevelIds.Count == 0
        ? "Excluded maps: none"
        : $"Excluded maps: {mapExclusions.LevelIds.Count}";

    // ---- Footer ----
    [UIValue("version-text")]
    public string VersionText => $"v{Plugin.Version}";

    private RuleSet SelectedRules => config.RulesFor(selectedCategory);

    public SettingsMenu(BSMLSettings bsmlSettings, BSMLParser bsmlParser, BeatmapLevelsModel beatmapLevelsModel,
        PluginConfig config, MasterSwitch masterSwitch, MapExclusions mapExclusions,
        BeatSaberPlusLights beatSaberPlusLights, SettingsSetterPrompt settingsSetterPrompt)
    {
        this.bsmlSettings = bsmlSettings;
        this.bsmlParser = bsmlParser;
        this.beatmapLevelsModel = beatmapLevelsModel;
        this.config = config;
        this.masterSwitch = masterSwitch;
        this.mapExclusions = mapExclusions;
        this.beatSaberPlusLights = beatSaberPlusLights;
        this.settingsSetterPrompt = settingsSetterPrompt;
        rowTemplate = Utilities.GetResourceContent(typeof(SettingsMenu).Assembly, RowResourcePath);
    }

    public void Initialize()
    {
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, this);
        masterSwitch.Changed += OnMasterSwitchChanged;
        mapExclusions.Changed += OnExclusionsChanged;
        Loader.SongsLoadedEvent += OnSongsLoaded;
    }

    public void Dispose()
    {
        Loader.SongsLoadedEvent -= OnSongsLoaded;
        mapExclusions.Changed -= OnExclusionsChanged;
        masterSwitch.Changed -= OnMasterSwitchChanged;
        bsmlSettings.RemoveSettingsMenu(this);
    }

    [UsedImplicitly]
    [UIAction("#post-parse")]
    private void PostParse()
    {
        UpdateBodyVisibility();
        UpdateColorsToggle();
        OnExclusionsChanged();
    }

    // Also switched from the song browser tab.
    private void OnMasterSwitchChanged()
    {
        NotifyChanged(nameof(Enabled));
        UpdateBodyVisibility();
    }

    // Everything below the master switch is hidden while the mod is disabled.
    private void UpdateBodyVisibility()
    {
        if (settingsBody != null)
            settingsBody.SetActive(masterSwitch.Enabled);
    }

    // The toggles re-read the picked type's rules.
    [UsedImplicitly]
    [UIAction("select-category")]
    private void SelectCategory(SegmentedControl segmentedControl, int index)
    {
        selectedCategory = (MapCategory)index;
        foreach (string property in RuleProperties)
            NotifyChanged(property);
        UpdateColorsToggle();
    }

    // Chroma maps color their own lights. The toggle is made invisible rather than removed, so the list keeps its height.
    private void UpdateColorsToggle()
    {
        if (colorsToggle == null)
            return;

        bool shown = selectedCategory != MapCategory.Chroma;
        var canvasGroup = colorsToggle.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = colorsToggle.AddComponent<CanvasGroup>();
        canvasGroup.alpha = shown ? 1f : 0f;
        canvasGroup.interactable = shown;
        canvasGroup.blocksRaycasts = shown;
    }

    [UsedImplicitly]
    [UIAction("clear-exclusions")]
    private void ClearExclusions() => mapExclusions.Clear();

    // Custom map names are only known once SongCore has loaded the songs.
#if GAME_1_29
    private void OnSongsLoaded(Loader loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> levels) => OnExclusionsChanged();
#else
    private void OnSongsLoaded(Loader loader, ConcurrentDictionary<string, BeatmapLevel> levels) => OnExclusionsChanged();
#endif

    private void OnExclusionsChanged()
    {
        RebuildRows();
        NotifyChanged(nameof(ExclusionsText));
    }

    private void NotifyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
#if GAME_1_29
        var level = beatmapLevelsModel.GetLevelPreviewForLevelId(levelId);
        string? mappers = level?.levelAuthorName;
#elif GAME_1_40
        var level = beatmapLevelsModel.GetBeatmapLevel(levelId);
        string? mappers = level == null ? null : string.Join(", ", level.allMappers);
#else
        var level = beatmapLevelsModel.GetBeatmapLevel(levelId, true);
        string? mappers = level == null ? null : string.Join(", ", level.allMappers);
#endif
        if (level == null)
            return $"<color=#808080>Unknown map ({levelId.Replace(CustomLevelPrefix, string.Empty)})</color>";

        string mapperText = string.IsNullOrEmpty(mappers) ? string.Empty : $" <color=#808080>[{mappers}]</color>";
        return $"{level.songName} <size=80%>- {level.songAuthorName}</size>{mapperText}";
    }

    /// <summary>Host for one excluded-map row.</summary>
    private class ExcludedMapRow
    {
        private readonly string levelId;
        private readonly MapExclusions mapExclusions;

        [UIValue("map-name")]
        public string MapName { get; }

        public ExcludedMapRow(string levelId, string mapName, MapExclusions mapExclusions)
        {
            this.levelId = levelId;
            this.mapExclusions = mapExclusions;
            MapName = mapName;
        }

        [UsedImplicitly]
        [UIAction("remove")]
        private void Remove() => mapExclusions.SetExcluded(levelId, false);
    }
}
