namespace IntendedExperience.Detection;

internal enum MapVersion
{
    Unknown,
    V2,
    V3,
    V4
}

internal sealed class MapInfo
{
    public MapVersion Version { get; set; } = MapVersion.Unknown;
    public string EnvironmentName { get; set; } = string.Empty;
    public bool IsV3Environment { get; set; }
    public bool HasChroma { get; set; }
    public bool HasNoodleExtensions { get; set; }
    public bool HasVivify { get; set; }

    public bool HasModdedRequirements => HasChroma || HasNoodleExtensions || HasVivify;
    public bool IsV2OrV2Environment => Version == MapVersion.V2 || !IsV3Environment;
    public bool IsV3PlusInV3Environment => Version >= MapVersion.V3 && IsV3Environment;
}
