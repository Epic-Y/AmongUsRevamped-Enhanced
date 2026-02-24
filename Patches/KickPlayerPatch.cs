using InnerNet;
using UnityEngine;

// https://github.com/tukasa0001/TownOfHost/blob/main/Patches/ClientPatch.cs
namespace AmongUsRevamped;

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
internal class KickPlayerPatch
{
    public static Dictionary<string, int> AttemptedKickPlayerList = [];
    public static bool Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));

        return true;
    }
}