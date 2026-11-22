using System;
using System.Collections.Generic;
using System.Reflection;
using IPA.Loader;

namespace IntendedExperience.Compat;

/// <summary>
/// Reads which settings a map's Heck Settings Setter block controls, so IE leaves those to the mapper.
/// Heck and CustomJSONData are optional; without Heck the map has no Settings Setter and nothing is reported.
/// </summary>
internal class HeckSettingsSetter
{
    private const string HeckPluginId = "Heck";
    private const string CustomJsonDataPluginId = "CustomJSONData";
    private const string ExtensionsTypeName = "CustomJSONData.DictionaryExtensions";

    // ---- Settings Setter keys (difficulty custom data) ----
    private const string SettingsKey = "_settings";
    private const string PlayerOptionsKey = "_playerOptions";
    private const string ColorsKey = "_colors";
    private const string DefaultEffectsKey = "_environmentEffectsFilterDefaultPreset";
    private const string ExpertPlusEffectsKey = "_environmentEffectsFilterExpertPlusPreset";
    private const string OverrideDefaultColorsKey = "_overrideDefaultColors";

    private readonly MethodInfo? getBeatmapCustomData;

    // The menu re-plans every frame; only re-read when the selected difficulty changes.
    private BeatmapKey cachedKey;
    private (bool Present, bool EnvironmentEffects, bool Colors) cachedResult;

    public HeckSettingsSetter()
    {
        if (PluginManager.GetPluginFromId(HeckPluginId) == null)
            return;

        getBeatmapCustomData = PluginManager.GetPluginFromId(CustomJsonDataPluginId)?.Assembly
            .GetType(ExtensionsTypeName)?
            .GetMethod("GetBeatmapCustomData", new[] { typeof(BeatmapLevel), typeof(BeatmapKey).MakeByRefType() });

        if (getBeatmapCustomData == null)
            Plugin.Log.Warn("Heck found but CustomJSONData couldn't be read; Settings Setter maps won't be detected.");
    }

    /// <summary>Whether the map has its own Settings Setter, and which of IE's settings it sets itself.</summary>
    public (bool Present, bool EnvironmentEffects, bool Colors) GetControlledSettings(BeatmapLevel level, BeatmapKey key)
    {
        if (getBeatmapCustomData == null)
            return default;
        if (!key.Equals(cachedKey))
        {
            cachedResult = ReadControlledSettings(level, key);
            cachedKey = key;
        }
        return cachedResult;
    }

    private (bool Present, bool EnvironmentEffects, bool Colors) ReadControlledSettings(BeatmapLevel level, BeatmapKey key)
    {
        try
        {
            var customData = getBeatmapCustomData!.Invoke(null, new object[] { level, key }) as IDictionary<string, object>;
            if (GetGroup(customData, SettingsKey) is not { } settings)
                return default;

            // The game uses the Expert+ preset only on Expert+ difficulties.
            string effectsKey = key.difficulty == BeatmapDifficulty.ExpertPlus ? ExpertPlusEffectsKey : DefaultEffectsKey;
            bool effects = GetGroup(settings, PlayerOptionsKey)?.ContainsKey(effectsKey) ?? false;
            bool colors = GetGroup(settings, ColorsKey)?.ContainsKey(OverrideDefaultColorsKey) ?? false;
            return (true, effects, colors);
        }
        catch (Exception ex)
        {
            Plugin.Log.Debug($"Couldn't read Settings Setter data: {ex.Message}");
            return default;
        }
    }

    private static IDictionary<string, object>? GetGroup(IDictionary<string, object>? data, string key) =>
        data != null && data.TryGetValue(key, out var value) ? value as IDictionary<string, object> : null;
}
