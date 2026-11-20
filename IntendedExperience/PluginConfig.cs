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
    public virtual bool ForceAllEffects { get; set; } = true;
    public virtual bool ForceIntendedColors { get; set; } = true;

    // ---- Menu ----
    public virtual bool TintPlayButton { get; set; } = true;

    // ---- Exclusions ----
    [NonNullable, UseConverter(typeof(ListConverter<string>))]
    public virtual List<string> ExcludedLevelIds { get; set; } = new();

    /// <summary>Saves the config; called after in-place changes BSIPA can't detect (list edits).</summary>
    public virtual void Changed() { }
}
