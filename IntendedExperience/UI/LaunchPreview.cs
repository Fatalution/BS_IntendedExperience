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

    public LaunchPreview(StandardLevelDetailViewController levelDetailViewController, PlayerDataModel playerDataModel,
        LaunchPlanner launchPlanner, MapExclusions mapExclusions)
    {
        this.levelDetailViewController = levelDetailViewController;
        this.playerDataModel = playerDataModel;
        this.launchPlanner = launchPlanner;
        this.mapExclusions = mapExclusions;
    }

    public event Action? Changed;

    public BeatmapLevel? Level { get; private set; }
    public LaunchPlan Plan { get; private set; }
    public bool Excluded { get; private set; }

    // Polled so changes made anywhere (gameplay setup, mod settings, exclusions) show up immediately.
    public void Tick()
    {
        if (!levelDetailViewController.isActivated)
            return;

        var level = levelDetailViewController.beatmapLevel;
        var plan = level == null ? default : PlanFor(level, levelDetailViewController.beatmapKey);
        bool excluded = level != null && mapExclusions.IsExcluded(level.levelID);
        if (level == Level && plan == Plan && excluded == Excluded)
            return;

        Level = level;
        Plan = plan;
        Excluded = excluded;
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
