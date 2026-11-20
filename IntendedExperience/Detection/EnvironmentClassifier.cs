using System.Collections.Generic;

namespace IntendedExperience.Detection;

internal static class EnvironmentClassifier
{
    // Environments that use the V3 lighting system
    // This list should be updated as new environments are released
    private static readonly HashSet<string> V3Environments = new()
    {
        // Interscope environments
        "BillieEnvironment",
        "LizzoEnvironment",
        "TheWeekndEnvironment",
        "LadyGagaEnvironment",

        // Rock Mixtape pack
        "RockMixtapeEnvironment",
        "LinkinParkEnvironment",
        "TheRollingStonesEnvironment",
        "QueenEnvironment",
        "PanicEnvironment",
        "ImagineDragons2Environment",

        // Electronic / EDM
        "EDMEnvironment",
        "PyroEnvironment",
        "WeaveEnvironment",
        "DaftPunkEnvironment",
        "LatticeEnvironment",
        "ColliderEnvironment",

        // Other DLC environments
        "LilypadEnvironment",
        "SpookyEnvironment",
        "Dragons2Environment",
        "TheSecondEnvironment",
        "MetallicaEnvironment",
        "EminemEnvironment",
        "MadonnaEnvironment",
        "KraftwerkEnvironment",
        "HipHopMixtapeEnvironment"
    };

    public static bool IsV3Environment(string environmentName)
    {
        if (string.IsNullOrEmpty(environmentName))
            return false;

        // Normalize the environment name
        var normalized = environmentName.Trim();

        // Direct match
        if (V3Environments.Contains(normalized))
            return true;

        // Try adding "Environment" suffix if not present
        if (!normalized.EndsWith("Environment"))
        {
            return V3Environments.Contains(normalized + "Environment");
        }

        return false;
    }
}
