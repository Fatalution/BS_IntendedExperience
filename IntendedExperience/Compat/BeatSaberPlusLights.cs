using System;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;

namespace IntendedExperience.Compat;

/// <summary>
/// Reads BeatSaberPlus GameTweaker's light brightness override and forces it to 100% in memory while active.
/// Its config is internal, so everything goes through reflection; without GameTweaker this does nothing.
/// </summary>
internal class BeatSaberPlusLights : IDisposable
{
    private const string HarmonyId = "com.fatalution.IntendedExperience.BeatSaberPlusLights";
    private const string ConfigTypeName = "BeatSaberPlus_GameTweaker.GTConfig";
    private const string LightsPatchesTypeName = "BeatSaberPlus_GameTweaker.Patches.Lights.PLightsPatches";
    private const float FullBrightness = 1f;

    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ---- Read by the static Harmony postfix ----
    private static MethodInfo? setTempLightIntensity;
    private static bool forceFullBrightness;

    // ---- Reflection handles ----
    private readonly object? config;
    private readonly FieldInfo? enabledField;
    private readonly FieldInfo? playerOptionsField;
    private readonly FieldInfo? overrideOptionField;
    private readonly FieldInfo? overrideIntensityField;
    private readonly MethodInfo? setFromConfig;
    private readonly Harmony? harmony;

    public bool IsAvailable { get; }

    /// <summary>The brightness GameTweaker would apply to the next map (1 = 100%).</summary>
    public float EffectiveBrightness
    {
        get
        {
            if (!IsAvailable || !(bool)enabledField!.GetValue(config))
                return FullBrightness;

            var playerOptions = playerOptionsField!.GetValue(config);
            return (bool)overrideOptionField!.GetValue(playerOptions)
                ? (float)overrideIntensityField!.GetValue(playerOptions)
                : FullBrightness;
        }
    }

    public BeatSaberPlusLights()
    {
        var assembly = PluginManager.GetPluginFromId(InstalledMods.GameTweakerId)?.Assembly;
        if (assembly == null)
            return;

        try
        {
            var configType = assembly.GetType(ConfigTypeName, true);
            var lightsPatchesType = assembly.GetType(LightsPatchesTypeName, true);

            config = configType
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
                .GetValue(null);
            enabledField = configType.GetField("Enabled", InstanceFields);
            playerOptionsField = configType.GetField("PlayerOptions", InstanceFields);
            overrideOptionField = playerOptionsField?.FieldType.GetField("OverrideLightIntensityOption", InstanceFields);
            overrideIntensityField = playerOptionsField?.FieldType.GetField("OverrideLightIntensity", InstanceFields);
            setTempLightIntensity = lightsPatchesType.GetMethod("SetTempLightIntensity", BindingFlags.Public | BindingFlags.Static);

            setFromConfig = lightsPatchesType.GetMethod("SetFromConfig", BindingFlags.Public | BindingFlags.Static);
            if (config == null || enabledField == null || overrideOptionField == null || overrideIntensityField == null
                || setTempLightIntensity == null || setFromConfig == null)
                throw new MissingMemberException("GameTweaker light intensity members not found.");

            // GameTweaker loads its intensity at each level start; this runs straight after.
            harmony = new Harmony(HarmonyId);
            harmony.Patch(setFromConfig, postfix: new HarmonyMethod(typeof(BeatSaberPlusLights), nameof(SetFromConfigPostfix)));
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"BeatSaberPlus GameTweaker found but its light brightness couldn't be hooked: {ex.Message}");
        }
    }

    /// <summary>Forces 100% brightness for level starts until turned off; GameTweaker's config is untouched.</summary>
    public void SetForceFullBrightness(bool force) => forceFullBrightness = force && IsAvailable;

    /// <summary>
    /// Reloads GameTweaker's brightness now. It only does that itself for launches through the menu's Play button,
    /// so other launches (e.g. BeatLeader replays) would keep the previous map's value.
    /// </summary>
    public void Refresh()
    {
        if (IsAvailable && (bool)enabledField!.GetValue(config))
            setFromConfig!.Invoke(null, null);
    }

    public void Dispose() => harmony?.UnpatchSelf();

    private static void SetFromConfigPostfix()
    {
        if (forceFullBrightness)
            setTempLightIntensity!.Invoke(null, new object[] { FullBrightness });
    }
}
