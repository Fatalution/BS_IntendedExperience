#if GAME_1_29
using System;
using System.Collections.Generic;
using System.Globalization;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.TypeHandlers;
using UnityEngine.UI;

namespace IntendedExperience.UI;

/// <summary>The flexible-width/-height attributes IE's layouts use, which BSML 1.6 (1.29.1) doesn't have yet.</summary>
[ComponentHandler(typeof(LayoutElement))]
internal class FlexibleLayoutHandler : TypeHandler<LayoutElement>
{
    // The parser outlives IE's containers, so the handler is added once per game session.
    private static bool registered;

    public override Dictionary<string, string[]> Props => new()
    {
        { "flexibleWidth", new[] { "flexible-width" } },
        { "flexibleHeight", new[] { "flexible-height" } }
    };

    public override Dictionary<string, Action<LayoutElement, string>> Setters => new()
    {
        { "flexibleWidth", (element, value) => element.flexibleWidth = ParseFloat(value) },
        { "flexibleHeight", (element, value) => element.flexibleHeight = ParseFloat(value) }
    };

    public static void Register()
    {
        if (registered)
            return;

        BSMLParser.instance.RegisterTypeHandler(new FlexibleLayoutHandler());
        registered = true;
    }

    private static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);
}
#endif
