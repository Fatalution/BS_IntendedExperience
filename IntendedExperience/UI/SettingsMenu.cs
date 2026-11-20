using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Settings;
using IntendedExperience.Core;
using SongCore;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Settings -> Mod Settings page.</summary>
internal class SettingsMenu : IInitializable, IDisposable, INotifyPropertyChanged
{
    private const string MenuName = "Intended Experience";
    private const string ResourcePath = "IntendedExperience.UI.SettingsMenu.bsml";
    private const string CustomLevelPrefix = "custom_level_";

    private readonly BSMLSettings bsmlSettings;
    private readonly BeatmapLevelsModel beatmapLevelsModel;
    private readonly PluginConfig config;
    private readonly MapExclusions mapExclusions;

    [UIComponent("excluded-list")]
    private readonly CustomCellListTableData excludedList = null!;

    public SettingsMenu(BSMLSettings bsmlSettings, BeatmapLevelsModel beatmapLevelsModel, PluginConfig config,
        MapExclusions mapExclusions)
    {
        this.bsmlSettings = bsmlSettings;
        this.beatmapLevelsModel = beatmapLevelsModel;
        this.config = config;
        this.mapExclusions = mapExclusions;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- Settings ----
    [UIValue("enabled")]
    public bool Enabled { get => config.Enabled; set => config.Enabled = value; }

    [UIValue("force-all-effects")]
    public bool ForceAllEffects { get => config.ForceAllEffects; set => config.ForceAllEffects = value; }

    [UIValue("force-intended-colors")]
    public bool ForceIntendedColors { get => config.ForceIntendedColors; set => config.ForceIntendedColors = value; }

    [UIValue("tint-play-button")]
    public bool TintPlayButton { get => config.TintPlayButton; set => config.TintPlayButton = value; }

    // ---- Exclusions ----
    [UIValue("excluded-maps")]
    public List<object> ExcludedMaps { get; } = new();

    [UIValue("exclusions-text")]
    public string ExclusionsText => mapExclusions.LevelIds.Count == 0
        ? "Excluded maps: none"
        : $"Excluded maps: {mapExclusions.LevelIds.Count}";

    public void Initialize()
    {
        RebuildExcludedMaps();
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

    [UIAction("#post-parse")]
    private void PostParse() => OnExclusionsChanged();

    [UIAction("clear-exclusions")]
    private void ClearExclusions() => mapExclusions.Clear();

    // Custom map names are only known once SongCore has loaded the songs.
    private void OnSongsLoaded(Loader loader, ConcurrentDictionary<string, BeatmapLevel> levels) => OnExclusionsChanged();

    private void OnExclusionsChanged()
    {
        RebuildExcludedMaps();
        if (excludedList != null)
            excludedList.TableView.ReloadData();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExclusionsText)));
    }

    private void RebuildExcludedMaps()
    {
        ExcludedMaps.Clear();
        ExcludedMaps.AddRange(mapExclusions.LevelIds
            .Select(levelId => new ExcludedMapCell(levelId, DisplayName(levelId), mapExclusions))
            .OrderBy(cell => cell.MapName, StringComparer.OrdinalIgnoreCase));
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

    /// <summary>One row of the excluded-maps list.</summary>
    private class ExcludedMapCell
    {
        private readonly string levelId;
        private readonly MapExclusions mapExclusions;

        public ExcludedMapCell(string levelId, string mapName, MapExclusions mapExclusions)
        {
            this.levelId = levelId;
            this.mapExclusions = mapExclusions;
            MapName = mapName;
        }

        [UIValue("map-name")]
        public string MapName { get; }

        [UIAction("remove")]
        private void Remove() => mapExclusions.SetExcluded(levelId, false);
    }
}
