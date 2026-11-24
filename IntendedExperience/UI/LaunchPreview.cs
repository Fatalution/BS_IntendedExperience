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

    public event Action? Changed;

    public MapDifficulty? Map { get; private set; }
    public LaunchPlan Plan { get; private set; }
    public bool Excluded { get; private set; }

    /// <summary>The selected map is a GLS map, Chroma map or modchart, so the mod can affect it.</summary>
    public bool Qualifies { get; private set; }

    /// <summary>The changes will be listed in Heck's Settings Setter window when Play is pressed.</summary>
    public bool ShownInSettingsSetter { get; private set; }

    public bool ShowTooltips { get; private set; }

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
        bool excluded = false, qualifies = false, shownInSettingsSetter = false;
        if (map is { } selected)
        {
            plan = launchPlanner.Plan(selected, LaunchSettings.FromPlayerData(playerDataModel.playerData));
            excluded = mapExclusions.IsExcluded(selected.LevelId);
            qualifies = launchPlanner.Qualifies(selected);
            shownInSettingsSetter = settingsSetterPrompt.WillShowChangesFor(selected);
        }
        bool showTooltips = config.ShowTooltips;
        if (map == Map && plan == Plan && excluded == Excluded && qualifies == Qualifies
            && shownInSettingsSetter == ShownInSettingsSetter && showTooltips == ShowTooltips)
            return;

        Map = map;
        Plan = plan;
        Excluded = excluded;
        Qualifies = qualifies;
        ShownInSettingsSetter = shownInSettingsSetter;
        ShowTooltips = showTooltips;
        Changed?.Invoke();
    }

    public void SetExcluded(bool excluded)
    {
        if (Map is { } map)
            mapExclusions.SetExcluded(map.LevelId, excluded);
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
