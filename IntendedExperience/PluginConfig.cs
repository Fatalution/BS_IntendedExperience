using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace IntendedExperience;

internal class PluginConfig
{
    // ---- Master switch ----
    public virtual bool Enabled { get; set; } = true;

    // ---- Rules ----
    public virtual bool ForceLights { get; set; } = true;
    public virtual bool ForceNotesOnlyColors { get; set; } = true;
    public virtual bool ForceMapEffectColors { get; set; } = true;
    public virtual bool MapEffectColorsOnLegacyEnvironments { get; set; } = false;

    // ---- Menu ----
    public virtual bool TintPlayButton { get; set; } = true;

    // ---- Exclusions ----
    [NonNullable, UseConverter(typeof(ListConverter<string>))]
    public virtual List<string> ExcludedLevelIds { get; set; } = new();

    /// <summary>Saves the config; called after in-place changes BSIPA can't detect (list edits).</summary>
    public virtual void Changed() { }
}
