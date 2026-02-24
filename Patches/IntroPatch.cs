using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using UnityEngine;

namespace AmongUsRevamped;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
internal static class CoShowIntroPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        Logger.Info(" Intro initiated", "CoShowIntro");

        if (!AmongUsClient.Instance.AmHost) return;

        if ((Options.Gamemode.GetValue() == 0 || Options.Gamemode.GetValue() == 1) && !Utils.isHideNSeek)
        {
            CustomRoleManagement.AssignRoles();
        }

        CustomRoleManagement.SendRoleMessages(new Dictionary<string, string>
        {
            { "Jester", Translator.Get("JesterPriv")},
            { "Mayor", Translator.Get("MayorPriv")}
        });

        if (Options.DisableAnnoyingMeetingCalls.GetBool())
        {
            Utils.CanCallMeetings = false;
            _ = new LateTask(() =>
            {       
                Utils.CanCallMeetings = true;
            }, 33f, "MeetingEnabled");     
        }
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (Main.GM.Value)
        {
            __instance.TeamTitle.text = "Game Master";
        }
    }
}