using System;
using JetBrains.Annotations;

namespace IntendedExperience.Core;

/// <summary>Turns the whole mod on or off; the settings page and the song browser tab both show it.</summary>
[UsedImplicitly]
internal class MasterSwitch
{
    private readonly PluginConfig config;

    public event Action? Changed;

    public bool Enabled
    {
        get => config.Enabled;
        set
        {
            if (config.Enabled == value)
                return;

            config.Enabled = value;
            Changed?.Invoke();
        }
    }

    public MasterSwitch(PluginConfig config)
    {
        this.config = config;
    }
}
