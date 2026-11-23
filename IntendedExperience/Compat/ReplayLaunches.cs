using System;
using System.Diagnostics;
using System.Linq;

namespace IntendedExperience.Compat;

/// <summary>
/// Recognizes replay launches by the replay mod's code on the call stack while the level is set up:
/// BeatLeader's ReplayerLauncher calls the setup itself, ScoreSaber's ReplayLoader goes through StartStandardLevel.
/// </summary>
internal static class ReplayLaunches
{
    private static readonly string[] ReplayNamespaces =
    {
        "BeatLeader.Replayer.",
        "ScoreSaber.Features.Replays."
    };

    public static bool IsCurrentLaunchReplay() =>
        new StackTrace().GetFrames()?.Any(frame => IsReplayCode(frame.GetMethod()?.DeclaringType)) == true;

    private static bool IsReplayCode(Type? type) =>
        type?.FullName is { } name && ReplayNamespaces.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
}
