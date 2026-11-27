using IntendedExperience.Core;

namespace IntendedExperience.UI;

/// <summary>Colors and wording shared by IE's menu texts, not hover hints (white background).</summary>
internal static class MenuText
{
    // ---- Colors ----
    /// <summary>Same orange as the tinted Play button.</summary>
    public const string HighlightColor = "#FF9933";
    public const string RankedColor = "#FFD54F";
    public const string DimColor = "#808080";

    // ---- The word "ranked" ----
    public const string Ranked = "<color=" + RankedColor + ">ranked</color>";
    public const string RankedTitle = "<color=" + RankedColor + ">Ranked</color>";

    /// <summary>The map type in the tab's header, e.g. "GLS".</summary>
    public static string TypeName(MapCategory? category) => category switch
    {
        MapCategory.Gls => "GLS",
        MapCategory.Modcharts => "Modchart",
        MapCategory.Chroma => "Chroma",
        _ => "Legacy"
    };

    /// <summary>Maps of a type in a sentence, e.g. "GLS maps".</summary>
    public static string MapsName(MapCategory category) => category switch
    {
        MapCategory.Modcharts => "modcharts",
        MapCategory.Chroma => "Chroma maps",
        _ => "GLS maps"
    };
}
