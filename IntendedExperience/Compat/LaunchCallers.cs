using System;
using System.Diagnostics;
using System.Linq;

namespace IntendedExperience.Compat;

/// <summary>
/// Recognizes who started the current launch by their code on the call stack. Level setup and Heck's window
/// run synchronously from the code that starts them, so the caller is still on the stack.
/// </summary>
internal static class LaunchCallers
{
    private const string BeatLeaderNamespace = "BeatLeader.";
    private const string HeckPlayViewNamespace = "Heck.PlayView.";
    private const string MultiplayerPlusNamespace = "BeatSaberPlus_Multiplayer.";

    // BeatLeader's ReplayerLauncher sets the level up itself, ScoreSaber's ReplayLoader goes through StartStandardLevel.
    private static readonly string[] ReplayNamespaces = { "BeatLeader.Replayer.", "ScoreSaber.Features.Replays." };

    public static bool IsReplay() => IsOnStack(ReplayNamespaces);

    public static bool IsBeatLeader() => IsOnStack(BeatLeaderNamespace);

    // Its room manager starts the level when the lobby does.
    public static bool IsMultiplayerPlus() => IsOnStack(MultiplayerPlusNamespace);

    /// <summary>
    /// Heck's play flow started the level, so it applied what the player accepted in its Settings Setter window.
    /// Other launchers (e.g. Multiplayer+, replays) skip it.
    /// </summary>
    public static bool IsHeckPlayView() => IsOnStack(HeckPlayViewNamespace);

    private static bool IsOnStack(params string[] namespaces) =>
        new StackTrace().GetFrames()?.Any(frame =>
            frame.GetMethod()?.DeclaringType?.FullName is { } name
            && namespaces.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))) == true;
}
