using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using IntendedExperience.Core;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace IntendedExperience;

internal class PluginConfig
{
    // ---- Master switch ----
    public virtual bool Enabled { get; set; } = true;

    // ---- Rules, per map type ----
    [NonNullable]
    public virtual RuleSet Gls { get; set; } = new();
    [NonNullable]
    public virtual RuleSet Chroma { get; set; } = new();
    [NonNullable]
    public virtual RuleSet Modcharts { get; set; } = new();

    // ---- Other launchers: replays (BeatLeader, ScoreSaber), BeatSaberPlus Multiplayer+ ----
    public virtual bool ApplyToReplays { get; set; } = true;
    public virtual bool ApplyInMultiplayerPlus { get; set; } = true;

    // ---- Menu ----
    public virtual bool TintPlayButton { get; set; } = true;
    public virtual bool ShowTooltips { get; set; } = true;
    public virtual bool ShowInSettingsSetter { get; set; } = false;

    // ---- Exclusions ----
    [NonNullable, UseConverter(typeof(ListConverter<string>))]
    public virtual List<string> ExcludedLevelIds { get; set; } = new();

    /// <summary>Saves the config; called after in-place changes BSIPA can't detect (list edits).</summary>
    public virtual void Changed() { }

    public RuleSet RulesFor(MapCategory category) => category switch
    {
        MapCategory.Chroma => Chroma,
        MapCategory.Modcharts => Modcharts,
        _ => Gls
    };
}

/// <summary>The rules applied to one type of map. Chroma maps color their own lights, so they ignore the colors rule.</summary>
internal class RuleSet
{
    public virtual bool ForceAllEffects { get; set; } = true;
    public virtual bool ForceIntendedColors { get; set; } = true;
    public virtual bool ForceMapEnvironment { get; set; } = true;
    public virtual bool ForceFullBrightness { get; set; } = true;
}
