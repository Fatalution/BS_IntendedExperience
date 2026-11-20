using System;
using System.Reflection;
using IPA.Loader;

namespace IntendedExperience.Compat;

internal static class BSPlusCompat
{
    private const string BSPlusPluginId = "BeatSaberPlus";

    private static bool? _isInstalled;
    private static Type? _configType;
    private static PropertyInfo? _brightnessProperty;
    private static object? _configInstance;

    public static bool IsInstalled
    {
        get
        {
            _isInstalled ??= PluginManager.GetPluginFromId(BSPlusPluginId) != null;
            return _isInstalled.Value;
        }
    }

    public static float? GetBrightness()
    {
        if (!IsInstalled)
            return null;

        try
        {
            EnsureReflectionSetup();

            if (_brightnessProperty == null || _configInstance == null)
                return null;

            var value = _brightnessProperty.GetValue(_configInstance);
            return value as float?;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"BSPlusCompat: Failed to get brightness: {ex.Message}");
            return null;
        }
    }

    public static void SetBrightness(float brightness)
    {
        if (!IsInstalled)
        {
            Plugin.Log.Debug("BSPlusCompat: BeatSaberPlus is not installed");
            return;
        }

        try
        {
            EnsureReflectionSetup();

            if (_brightnessProperty == null || _configInstance == null)
            {
                Plugin.Log.Warn("BSPlusCompat: Could not find brightness property");
                return;
            }

            _brightnessProperty.SetValue(_configInstance, brightness);
            Plugin.Log.Debug($"BSPlusCompat: Set brightness to {brightness}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"BSPlusCompat: Failed to set brightness: {ex.Message}");
        }
    }

    private static void EnsureReflectionSetup()
    {
        if (_configType != null)
            return;

        try
        {
            var bsPlusPlugin = PluginManager.GetPluginFromId(BSPlusPluginId);
            if (bsPlusPlugin == null)
                return;

            var assembly = bsPlusPlugin.Assembly;

            // Try to find the config class
            // BeatSaberPlus typically uses patterns like:
            // BeatSaberPlus.Config or BeatSaberPlus.SDK.Config
            _configType = assembly.GetType("BeatSaberPlus.SDK.Config")
                          ?? assembly.GetType("BeatSaberPlus.Config");

            if (_configType == null)
            {
                // Try to find any config type with brightness settings
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.Name.Contains("Config") || type.Name.Contains("Settings"))
                    {
                        var prop = type.GetProperty("EventBrightness",
                                       BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                   ?? type.GetProperty("LightBrightness",
                                       BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                   ?? type.GetProperty("Brightness",
                                       BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                        if (prop != null && prop.PropertyType == typeof(float))
                        {
                            _configType = type;
                            _brightnessProperty = prop;
                            break;
                        }
                    }
                }
            }

            if (_configType == null)
            {
                Plugin.Log.Warn("BSPlusCompat: Could not find config type");
                return;
            }

            // Look for instance
            var instanceProp = _configType.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);

            if (instanceProp != null)
            {
                _configInstance = instanceProp.GetValue(null);
            }

            // Find brightness property if not already found
            _brightnessProperty ??= _configType.GetProperty("EventBrightness",
                                        BindingFlags.Public | BindingFlags.Instance)
                                    ?? _configType.GetProperty("LightBrightness",
                                        BindingFlags.Public | BindingFlags.Instance)
                                    ?? _configType.GetProperty("Brightness",
                                        BindingFlags.Public | BindingFlags.Instance);

            Plugin.Log.Debug($"BSPlusCompat: Reflection setup complete. " +
                             $"ConfigType={_configType?.Name}, " +
                             $"BrightnessProp={_brightnessProperty?.Name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"BSPlusCompat: Reflection setup failed: {ex.Message}");
        }
    }
}
