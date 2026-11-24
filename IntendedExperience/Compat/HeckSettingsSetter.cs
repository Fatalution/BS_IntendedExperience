using System;
using System.Collections.Generic;
using System.Reflection;
using IntendedExperience.Core;
using IPA.Loader;

namespace IntendedExperience.Compat;

/// <summary>Whether a map has its own Settings Setter, and which of IE's settings it sets itself.</summary>
internal readonly record struct SettingsSetterControls(bool Present, bool EnvironmentEffects, bool Environment, bool Colors);

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
    private const string EnvironmentsKey = "_environments";
    private const string ColorsKey = "_colors";
    private const string DefaultEffectsKey = "_environmentEffectsFilterDefaultPreset";
    private const string ExpertPlusEffectsKey = "_environmentEffectsFilterExpertPlusPreset";
    private const string OverrideEnvironmentsKey = "_overrideEnvironments";
    private const string OverrideDefaultColorsKey = "_overrideDefaultColors";

#if GAME_1_29
    private static readonly Type[] CustomDataArguments = { typeof(IDifficultyBeatmap) };
#else
    private static readonly Type[] CustomDataArguments = { typeof(BeatmapLevel), typeof(BeatmapKey).MakeByRefType() };
#endif

    private readonly MethodInfo? getBeatmapCustomData;

    // The menu re-plans every frame; only re-read when the selected difficulty changes.
    private MapDifficulty? cachedMap;
    private SettingsSetterControls cachedResult;

    public HeckSettingsSetter()
    {
        if (PluginManager.GetPluginFromId(HeckPluginId) == null)
            return;

        getBeatmapCustomData = PluginManager.GetPluginFromId(CustomJsonDataPluginId)?.Assembly
            .GetType(ExtensionsTypeName)?
            .GetMethod("GetBeatmapCustomData", CustomDataArguments);

        if (getBeatmapCustomData == null)
            Plugin.Log.Warn("Heck found but CustomJSONData couldn't be read; Settings Setter maps won't be detected.");
    }

    public SettingsSetterControls GetControlledSettings(MapDifficulty map)
    {
        if (getBeatmapCustomData == null)
            return default;
        if (cachedMap != map)
        {
            cachedResult = ReadControlledSettings(map);
            cachedMap = map;
        }
        return cachedResult;
    }

    private SettingsSetterControls ReadControlledSettings(MapDifficulty map)
    {
        try
        {
#if GAME_1_29
            object[] arguments = { map.Beatmap };
#else
            object[] arguments = { map.Level, map.Key };
#endif
            var customData = getBeatmapCustomData!.Invoke(null, arguments) as IDictionary<string, object>;
            if (GetGroup(customData, SettingsKey) is not { } settings)
                return default;

            // The game uses the Expert+ preset only on Expert+ difficulties.
            string effectsKey = map.Difficulty == BeatmapDifficulty.ExpertPlus ? ExpertPlusEffectsKey : DefaultEffectsKey;
            return new SettingsSetterControls(
                Present: true,
                EnvironmentEffects: GetGroup(settings, PlayerOptionsKey)?.ContainsKey(effectsKey) ?? false,
                Environment: GetGroup(settings, EnvironmentsKey)?.ContainsKey(OverrideEnvironmentsKey) ?? false,
                Colors: GetGroup(settings, ColorsKey)?.ContainsKey(OverrideDefaultColorsKey) ?? false);
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
