using System;
using IntendedExperience.Core;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>Tracks what launching the selected map would change, so the menu can show it.</summary>
internal class LaunchPreview : ITickable
{
    private readonly StandardLevelDetailViewController levelDetailViewController;
    private readonly PlayerDataModel playerDataModel;
    private readonly LaunchPlanner launchPlanner;
    private readonly MapExclusions mapExclusions;
    private readonly PluginConfig config;

    public LaunchPreview(StandardLevelDetailViewController levelDetailViewController, PlayerDataModel playerDataModel,
        LaunchPlanner launchPlanner, MapExclusions mapExclusions, PluginConfig config)
    {
        this.levelDetailViewController = levelDetailViewController;
        this.playerDataModel = playerDataModel;
        this.launchPlanner = launchPlanner;
        this.mapExclusions = mapExclusions;
        this.config = config;
    }

    public event Action? Changed;

    public BeatmapLevel? Level { get; private set; }
    public LaunchPlan Plan { get; private set; }
    public bool Excluded { get; private set; }

    /// <summary>The selected map uses a v3+ environment, so the mod can affect it.</summary>
    public bool Qualifies { get; private set; }

    public bool ShowTooltips { get; private set; }

    // Polled so changes made anywhere (gameplay setup, mod settings, exclusions) show up immediately.
    public void Tick()
    {
        if (!levelDetailViewController.isActivated)
            return;

        var level = levelDetailViewController.beatmapLevel;
        var key = levelDetailViewController.beatmapKey;
        var plan = level == null ? default : PlanFor(level, key);
        bool excluded = level != null && mapExclusions.IsExcluded(level.levelID);
        bool qualifies = level != null && launchPlanner.UsesGroupLighting(level, key);
        bool showTooltips = config.ShowTooltips;
        if (level == Level && plan == Plan && excluded == Excluded && qualifies == Qualifies
            && showTooltips == ShowTooltips)
            return;

        Level = level;
        Plan = plan;
        Excluded = excluded;
        Qualifies = qualifies;
        ShowTooltips = showTooltips;
        Changed?.Invoke();
    }

    public void SetExcluded(bool excluded)
    {
        if (Level != null)
            mapExclusions.SetExcluded(Level.levelID, excluded);
    }

    private LaunchPlan PlanFor(BeatmapLevel level, BeatmapKey key)
    {
        var playerData = playerDataModel.playerData;
        return launchPlanner.Plan(level, key, playerData.playerSpecificSettings,
            playerData.colorSchemesSettings.ShouldOverrideLightshowColors());
    }
}
