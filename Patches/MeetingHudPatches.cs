using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsRevamped;

[HarmonyPatch]
public static class MeetingHudPatches
{
    /// <summary>
    /// Mayor vote multiplier (x3).
    /// This patch runs only on the host. It increases the vote weight of the Mayor by 2
    /// (the first vote is already counted by the vanilla method).
    /// </summary>
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CalculateVotes))]
    public static class MeetingHud_CalculateVotes
    {
        public static void Postfix(MeetingHud __instance, ref Dictionary<byte, int> __result)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (__instance == null || __result == null) return;

            foreach (var playerState in __instance.playerStates)
            {
                if (playerState == null) continue;
                if (playerState.AmDead) continue;

                byte voterId = playerState.TargetPlayerId;

                bool isMayor = false; // CustomRoleManagement.IsMayor removed in this fork
                // Logger.Info($"[MayorVoteCheck] PlayerId={voterId} Name={playerState.NameText?.text} IsMayor={isMayor} VotedFor={playerState.VotedFor}", "MayorDebug");

                if (!isMayor) continue;

                byte votedFor = playerState.VotedFor;

                if (votedFor == PlayerVoteArea.DeadVote ||
                    votedFor == PlayerVoteArea.MissedVote ||
                    votedFor == PlayerVoteArea.HasNotVoted ||
                    votedFor == PlayerVoteArea.SkippedVote)
                {
                    Logger.Info($"Mayor {playerState.NameText?.text} skipped or invalid vote - no multiplier", "MayorVote");
                    continue;
                }

                if (__result.ContainsKey(votedFor))
                {
                    __result[votedFor] += 2;
                    Logger.Info($"*** MAYOR VOTE MULTIPLIER APPLIED *** {playerState.NameText?.text} (id {voterId}) → +2 votes to target {votedFor}. New total: {__result[votedFor]}", "MayorVote");
                }
                else
                {
                    Logger.Info($"Mayor voted but target {votedFor} not in results yet?", "MayorVote");
                }
            }
        }
    }

    /// <summary>
    /// Visual feedback for Mayor: bloops the vote icon 2 extra times so it clearly shows 3 votes.
    /// This runs after the normal vote icon is created by the game.
    /// </summary>
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class MeetingHud_CastVote
    {
        public static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId, [HarmonyArgument(1)] byte suspectPlayerId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (__instance == null) return;

            // Only apply to living Mayors
            if (true) return; // CustomRoleManagement.IsMayor removed in this fork - mayor vote multiplier disabled

            var mayorState = __instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == srcPlayerId);
            if (mayorState == null || mayorState.AmDead) return;

            // Skip visual multiplier if Mayor skipped or voted invalid target
            if (suspectPlayerId == PlayerVoteArea.SkippedVote ||
                suspectPlayerId == PlayerVoteArea.DeadVote ||
                suspectPlayerId == PlayerVoteArea.MissedVote ||
                suspectPlayerId == PlayerVoteArea.HasNotVoted)
            {
                return;
            }

            // Find the target vote area
            var targetState = __instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == suspectPlayerId);
            if (targetState == null || targetState.transform == null) return;

            // Get the player data of the Mayor (needed for BloopAVoteIcon)
            var mayorData = GameData.Instance?.GetPlayerById(srcPlayerId);
            if (mayorData == null) return;

            // Vanilla already bloops once when the vote is cast.
            // We add two more to visually represent x3.
            __instance.BloopAVoteIcon(mayorData, 0, targetState.transform);
            __instance.BloopAVoteIcon(mayorData, 0, targetState.transform);

            Logger.Info($"Mayor triple vote visual applied (2 extra icons) for player {srcPlayerId}", "MayorVoteVisual");
        }
    }
}