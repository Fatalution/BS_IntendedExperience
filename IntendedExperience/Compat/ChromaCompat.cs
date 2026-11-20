using System;
using System.Reflection;
using IPA.Loader;

namespace IntendedExperience.Compat;

internal static class ChromaCompat
{
    private const string ChromaPluginId = "Chroma";

    private static bool? _isInstalled;
    private static Type? _chromaConfigType;
    private static PropertyInfo? _chromaEnabledProperty;
    private static object? _chromaConfigInstance;

    public static bool IsInstalled
    {
        get
        {
            _isInstalled ??= PluginManager.GetPluginFromId(ChromaPluginId) != null;
            return _isInstalled.Value;
        }
    }

    public static bool IsEnabled()
    {
        if (!IsInstalled)
            return false;

        try
        {
            EnsureReflectionSetup();

            if (_chromaEnabledProperty == null || _chromaConfigInstance == null)
                return false;

            return (bool)(_chromaEnabledProperty.GetValue(_chromaConfigInstance) ?? false);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"ChromaCompat: Failed to get enabled state: {ex.Message}");
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (!IsInstalled)
        {
            Plugin.Log.Debug("ChromaCompat: Chroma is not installed, cannot set enabled state");
            return;
        }

        try
        {
            EnsureReflectionSetup();

            if (_chromaEnabledProperty == null || _chromaConfigInstance == null)
            {
                Plugin.Log.Warn("ChromaCompat: Could not find Chroma config to set enabled state");
                return;
            }

            _chromaEnabledProperty.SetValue(_chromaConfigInstance, enabled);
            Plugin.Log.Debug($"ChromaCompat: Set Chroma enabled to {enabled}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"ChromaCompat: Failed to set enabled state: {ex.Message}");
        }
    }

    private static void EnsureReflectionSetup()
    {
        if (_chromaConfigType != null)
            return;

        try
        {
            var chromaPlugin = PluginManager.GetPluginFromId(ChromaPluginId);
            if (chromaPlugin == null)
                return;

            var chromaAssembly = chromaPlugin.Assembly;

            // Try to find the Chroma config class
            // Common patterns: Chroma.Settings.ChromaConfig or Chroma.ChromaConfig
            _chromaConfigType = chromaAssembly.GetType("Chroma.Settings.ChromaConfig")
                                ?? chromaAssembly.GetType("Chroma.ChromaConfig")
                                ?? chromaAssembly.GetType("Chroma.Configuration.PluginConfig");

            if (_chromaConfigType == null)
            {
                Plugin.Log.Warn("ChromaCompat: Could not find Chroma config type");
                return;
            }

            // Look for a static Instance property or similar
            var instanceProp = _chromaConfigType.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);

            if (instanceProp != null)
            {
                _chromaConfigInstance = instanceProp.GetValue(null);
            }

            // Look for the enabled/active property
            _chromaEnabledProperty = _chromaConfigType.GetProperty("ChromaEventsDisabled",
                                         BindingFlags.Public | BindingFlags.Instance)
                                     ?? _chromaConfigType.GetProperty("Enabled",
                                         BindingFlags.Public | BindingFlags.Instance)
                                     ?? _chromaConfigType.GetProperty("CustomColorEventsEnabled",
                                         BindingFlags.Public | BindingFlags.Instance);

            Plugin.Log.Debug($"ChromaCompat: Reflection setup complete. " +
                             $"ConfigType={_chromaConfigType?.Name}, " +
                             $"EnabledProp={_chromaEnabledProperty?.Name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"ChromaCompat: Reflection setup failed: {ex.Message}");
        }
    }
}
