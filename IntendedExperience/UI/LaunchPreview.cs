using System;
using IntendedExperience.Core;
using JetBrains.Annotations;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Tracks what launching the selected map would change, so the menu can show it.</summary>
[UsedImplicitly]
internal class LaunchPreview : ITickable
{
    private readonly StandardLevelDetailViewController levelDetailViewController;
    private readonly PlayerDataModel playerDataModel;
    private readonly LaunchPlanner launchPlanner;
    private readonly MapExclusions mapExclusions;
    private readonly SettingsSetterPrompt settingsSetterPrompt;
    private readonly PluginConfig config;

    private MapStatus status;

    public event Action? Changed;

    public MapDifficulty? Map { get; private set; }
    public LaunchPlan Plan { get; private set; }
    public bool Excluded { get; private set; }

    /// <summary>The player kept IE on for the selected ranked map with the IE button.</summary>
    public bool Included { get; private set; }

    /// <summary>The changes will be listed in Heck's Settings Setter window when Play is pressed.</summary>
    public bool ShownInSettingsSetter { get; private set; }

    public bool ShowTooltips { get; private set; }

    /// <summary>The selected map's type; null for a legacy map.</summary>
    public MapCategory? Category => status.Category;

    /// <summary>The selected map is ranked on ScoreSaber or BeatLeader.</summary>
    public bool Ranked => status.Ranked;

    /// <summary>The selected map is ranked and its type's rules are turned off for ranked maps.</summary>
    public bool RankedTurnedOff => status.RankedTurnedOff;

    /// <summary>The selected map is one IE applies its rules to (exclusions aside).</summary>
    public bool Qualifies => status.Qualifies;

    /// <summary>The selected map is left alone because its type's rules are turned off for ranked maps.</summary>
    public bool SkippedRanked => RankedTurnedOff && !Included;

    /// <summary>The IE button is on: IE isn't turned off for the selected map by the player.</summary>
    public bool ButtonOn => RankedTurnedOff ? Included && !Excluded : !Excluded;

    public LaunchPreview(StandardLevelDetailViewController levelDetailViewController, PlayerDataModel playerDataModel,
        LaunchPlanner launchPlanner, MapExclusions mapExclusions, SettingsSetterPrompt settingsSetterPrompt,
        PluginConfig config)
    {
        this.levelDetailViewController = levelDetailViewController;
        this.playerDataModel = playerDataModel;
        this.launchPlanner = launchPlanner;
        this.mapExclusions = mapExclusions;
        this.settingsSetterPrompt = settingsSetterPrompt;
        this.config = config;
    }

    // Polled so changes made anywhere (gameplay setup, mod settings, exclusions) show up immediately.
    public void Tick()
    {
        if (!levelDetailViewController.isActivated)
            return;

        var map = SelectedMap();
        LaunchPlan plan = default;
        MapStatus mapStatus = default;
        bool excluded = false, included = false, shownInSettingsSetter = false;
        if (map is { } selected)
        {
            plan = launchPlanner.Plan(selected, LaunchSettings.FromPlayerData(playerDataModel.playerData));
            mapStatus = launchPlanner.Status(selected);
            excluded = mapExclusions.IsExcluded(selected.LevelId);
            included = mapExclusions.IsIncluded(selected.LevelId);
            shownInSettingsSetter = settingsSetterPrompt.WillShowChangesFor(selected);
        }
        bool showTooltips = config.ShowTooltips;
        if (map == Map && plan == Plan && mapStatus == status && excluded == Excluded && included == Included
            && shownInSettingsSetter == ShownInSettingsSetter && showTooltips == ShowTooltips)
            return;

        Map = map;
        Plan = plan;
        status = mapStatus;
        Excluded = excluded;
        Included = included;
        ShownInSettingsSetter = shownInSettingsSetter;
        ShowTooltips = showTooltips;
        Changed?.Invoke();
    }

    // On ranked maps of a type turned off for ranked maps the button includes the map; elsewhere it excludes it.
    public void ToggleButton()
    {
        if (Map is not { } map)
            return;

        if (RankedTurnedOff)
            mapExclusions.SetIncluded(map.LevelId, !ButtonOn);
        else
            mapExclusions.SetExcluded(map.LevelId, ButtonOn);
    }

    private MapDifficulty? SelectedMap()
    {
#if GAME_1_29
        var beatmap = levelDetailViewController.selectedDifficultyBeatmap;
        return beatmap == null ? null : new MapDifficulty(beatmap);
#else
        var level = levelDetailViewController.beatmapLevel;
        return level == null ? null : new MapDifficulty(level, levelDetailViewController.beatmapKey);
#endif
    }
}
