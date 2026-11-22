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
    private readonly SettingsSetterPrompt settingsSetterPrompt;
    private readonly PluginConfig config;

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

    public event Action? Changed;

    public BeatmapLevel? Level { get; private set; }
    public LaunchPlan Plan { get; private set; }
    public bool Excluded { get; private set; }

    /// <summary>The selected map is a GLS map or a modchart, so the mod can affect it.</summary>
    public bool Qualifies { get; private set; }

    /// <summary>The changes will be listed in Heck's Settings Setter window when Play is pressed.</summary>
    public bool ShownInSettingsSetter { get; private set; }

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
        bool qualifies = level != null && launchPlanner.Qualifies(level, key);
        bool shownInSettingsSetter = level != null && settingsSetterPrompt.WillShowChangesFor(level, key);
        bool showTooltips = config.ShowTooltips;
        if (level == Level && plan == Plan && excluded == Excluded && qualifies == Qualifies
            && shownInSettingsSetter == ShownInSettingsSetter && showTooltips == ShowTooltips)
            return;

        Level = level;
        Plan = plan;
        Excluded = excluded;
        Qualifies = qualifies;
        ShownInSettingsSetter = shownInSettingsSetter;
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
