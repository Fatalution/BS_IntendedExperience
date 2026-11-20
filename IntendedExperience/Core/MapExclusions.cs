namespace IntendedExperience.Core;

/// <summary>Maps the player chose to always play with their own settings.</summary>
internal class MapExclusions
{
    private readonly PluginConfig config;

    public MapExclusions(PluginConfig config)
    {
        this.config = config;
    }

    public int Count => config.ExcludedLevelIds.Count;

    public bool IsExcluded(string levelId) => config.ExcludedLevelIds.Contains(levelId);

    public void SetExcluded(string levelId, bool excluded)
    {
        if (IsExcluded(levelId) == excluded)
            return;

        if (excluded)
            config.ExcludedLevelIds.Add(levelId);
        else
            config.ExcludedLevelIds.Remove(levelId);
        config.Changed();
    }

    public void Clear()
    {
        config.ExcludedLevelIds.Clear();
        config.Changed();
    }
}
