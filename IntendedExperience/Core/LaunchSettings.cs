namespace IntendedExperience.Core;

/// <summary>The player's settings one level launch uses; changed here before the level is set up with them.</summary>
internal class LaunchSettings
{
#if GAME_1_29
    public LaunchSettings(PlayerSpecificSettings playerSpecificSettings, GameplayModifiers gameplayModifiers,
        OverrideEnvironmentSettings? overrideEnvironmentSettings, ColorScheme? overrideColorScheme)
#else
    public LaunchSettings(PlayerSpecificSettings playerSpecificSettings, GameplayModifiers gameplayModifiers,
        OverrideEnvironmentSettings? overrideEnvironmentSettings, ColorScheme? overrideColorScheme,
        bool overrideLightshowColors)
#endif
    {
        PlayerSpecificSettings = playerSpecificSettings;
        GameplayModifiers = gameplayModifiers;
        OverrideEnvironmentSettings = overrideEnvironmentSettings;
        OverrideColorScheme = overrideColorScheme;
#if !GAME_1_29
        OverrideLightshowColors = overrideLightshowColors;
#endif
    }

    public PlayerSpecificSettings PlayerSpecificSettings { get; set; }
    public GameplayModifiers GameplayModifiers { get; set; }
    public OverrideEnvironmentSettings? OverrideEnvironmentSettings { get; set; }

    /// <summary>The player's color scheme override; null when it's off.</summary>
    public ColorScheme? OverrideColorScheme { get; set; }

#if GAME_1_29
    /// <summary>The color override reaches the lights too; on 1.29.1 any override does.</summary>
    public bool OverridesLightColors => OverrideColorScheme != null;
#else
    /// <summary>The override type is "All" rather than "Notes Only".</summary>
    public bool OverrideLightshowColors { get; set; }

    /// <summary>The color override reaches the lights too ("All" override).</summary>
    public bool OverridesLightColors => OverrideColorScheme != null && OverrideLightshowColors;
#endif
}
