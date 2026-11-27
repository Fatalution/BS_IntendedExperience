using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace IntendedExperience.Core;

/// <summary>
/// Maps the player chose to always play with their own settings, and ranked maps they chose to keep IE on for while
/// their type is turned off for ranked maps.
/// </summary>
[UsedImplicitly]
internal class MapExclusions
{
    private readonly PluginConfig config;

    public event Action? Changed;

    public IReadOnlyList<string> ExcludedLevelIds => config.ExcludedLevelIds;
    public IReadOnlyList<string> IncludedLevelIds => config.IncludedLevelIds;

    public MapExclusions(PluginConfig config)
    {
        this.config = config;
    }

    public bool IsExcluded(string levelId) => config.ExcludedLevelIds.Contains(levelId);

    public bool IsIncluded(string levelId) => config.IncludedLevelIds.Contains(levelId);

    public void SetExcluded(string levelId, bool excluded)
    {
        if (SetListed(config.ExcludedLevelIds, levelId, excluded))
            Save();
    }

    /// <summary>Including a map also takes it off the exclusion list, so IE applies to it.</summary>
    public void SetIncluded(string levelId, bool included)
    {
        bool changed = SetListed(config.IncludedLevelIds, levelId, included);
        if (included)
            changed |= SetListed(config.ExcludedLevelIds, levelId, false);
        if (changed)
            Save();
    }

    public void ClearExcluded() => ClearList(config.ExcludedLevelIds);

    public void ClearIncluded() => ClearList(config.IncludedLevelIds);

    private void ClearList(List<string> levelIds)
    {
        levelIds.Clear();
        Save();
    }

    // Returns whether the list changed.
    private static bool SetListed(List<string> levelIds, string levelId, bool listed)
    {
        if (levelIds.Contains(levelId) == listed)
            return false;

        if (listed)
            levelIds.Add(levelId);
        else
            levelIds.Remove(levelId);
        return true;
    }

    private void Save()
    {
        config.Changed();
        Changed?.Invoke();
    }
}
