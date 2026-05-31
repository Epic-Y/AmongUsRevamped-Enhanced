using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmongUsRevamped;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
internal static class ChatControllerUpdatePatch
{

    public static void Postfix(ChatController __instance)
    {
        if (!__instance||!__instance.freeChatField||!__instance.freeChatField.textArea||!__instance.freeChatField.background||__instance.freeChatField.textArea.compoText == null||!__instance.freeChatField.textArea.outputText) return;
        if (!__instance.quickChatField||!__instance.quickChatField.background||__instance.quickChatField.text==null) return;

        if (Main.DarkTheme.Value)
        {
            __instance.freeChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
            __instance.freeChatField.textArea.compoText.Color(Color.white);
            __instance.freeChatField.textArea.outputText.color = Color.white;

            __instance.quickChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
            __instance.quickChatField.text.color = Color.white;
        }
        else
        {
            __instance.freeChatField.background.color = Color.white;
            __instance.freeChatField.textArea.compoText.Color(Color.black);
            __instance.freeChatField.textArea.outputText.color = Color.black;

            __instance.quickChatField.background.color = Color.white;
            __instance.quickChatField.text.color = Color.black;
        }
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
internal static class ChatBubbleSetNamePatch
{
    public static void Postfix(ChatBubble __instance, [HarmonyArgument(2)] bool voted)
    {
        if (!__instance||!__instance.playerInfo||!__instance.playerInfo.Object||!__instance.playerInfo.Object.Data||!__instance.TextArea||!__instance.Background) return;

        PlayerControl target = __instance.playerInfo.Object;

        if (Main.DarkTheme.Value)
        {
            __instance.Background.color = new(0.1f, 0.1f, 0.1f, 1f);
            __instance.TextArea.color = Color.white;

            if (__instance.playerInfo.Object.Data.IsDead && Utils.InGame) __instance.Background.color = new(0.1f, 0.1f, 0.1f, 0.7f);
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal static class SendChatPatch
{
    public static string ConvertNum(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        int digitCount = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (char.IsDigit(input[i]) && ++digitCount > 5)
            {
                var sb = new System.Text.StringBuilder(input.Length);

                foreach (char c in input)
                {
                    if (char.IsDigit(c))
                        sb.Append(Main.CircledDigits[c - '0']);
                    else
                        sb.Append(c);
                }
                return sb.ToString();
            }
        }
        return input;
    }

    public static bool Prefix(ChatController __instance)
    {
        string msgtext = __instance.freeChatField.textArea.text.Trim();
        string text = msgtext.ToLower();
        string converted = ConvertNum(msgtext);

        if (!AmongUsClient.Instance.AmHost) return true;

        if (text == "/reload" || text == "/translatereload" || text == "/reset" || text == "/translatereset")
        {
            Translator.Reload();
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            return false;
        }

        if (text == "/dump")
        {
            Utils.DumpLog();
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            return false;
        }

        // ==================== /vip (Moderators + Admins) ====================
        if (text.StartsWith("/vip "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);

            if (!Utils.IsModeratorOrHigher(PlayerControl.LocalPlayer))
            {
                return false;
            }

            // Same strict detection as /kill: color first (must be unique), then exact name, then unique partial name
            string vipArg = msgtext.Substring(5).Trim();
            PlayerControl vipTarget = Utils.GetPlayerForAdminCommand(vipArg);
            if (vipTarget == null) return false;

            string result = Utils.SetPlayerRank(vipTarget, 1);
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, result);

            // When the host uses the command, broadcast it publicly and respect chat cooldown
            if (AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
            }
            return false;
        }

        // ==================== /moderator (Admins only) ====================
        if (text.StartsWith("/moderator "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);

            if (!Utils.IsAdmin(PlayerControl.LocalPlayer))
            {
                return false;
            }

            // Same strict detection as /kill: color first (must be unique), then exact name, then unique partial name
            string modArg = msgtext.Substring(11).Trim();
            PlayerControl modTarget = Utils.GetPlayerForAdminCommand(modArg);
            if (modTarget == null) return false;

            string result = Utils.SetPlayerRank(modTarget, 2);
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, result);

            // When the host uses the command, broadcast it publicly and respect chat cooldown
            if (AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
            }
            return false;
        }

        // ==================== /admin (Host only) ====================
        if (text.StartsWith("/admin "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);

            if (!AmongUsClient.Instance.AmHost)
            {
                return false;
            }

            // Same strict detection as /kill: color first (must be unique), then exact name, then unique partial name
            string adminArg = msgtext.Substring(7).Trim();
            PlayerControl adminTarget = Utils.GetPlayerForAdminCommand(adminArg);
            if (adminTarget == null) return false;

            string result = Utils.SetPlayerRank(adminTarget, 3);
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, result);

            // When the host uses the command, broadcast it publicly and respect chat cooldown
            if (AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
            }
            return false;
        }

        if (text == "/h" || text == "/help" || text == "/cmd" || text == "/commands")
        {
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{Translator.Get("allCommandsFull")}");
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            return false;
        }

        if (text == "/eg" || text == "/endgame")
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);

            if (!Utils.InGame) return false;
            MessageWriter writer = AmongUsClient.Instance.StartEndGame();
            writer.Write((byte)GameOverReason.ImpostorDisconnect);
            AmongUsClient.Instance.FinishEndGame(writer);
            return false;
        }

        if (text == "/em" || text == "/endmeeting")
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            
            if ( !Utils.InGame || !Utils.IsMeeting) return false;
            MeetingHud.Instance.RpcClose();
            return false;
        }

        if (__instance.timeSinceLastMessage < 3f || OnGameJoinedPatch.WaitingForChat || CustomRoleManagement.HandlingRoleMessages) return false;

        if (text == "/l" || text == "/lastgame" || text == "/win" || text == "/winner")
        {
            if (string.IsNullOrEmpty(NormalGameEndChecker.LastWinReason) || Utils.InGame) return false;
            Utils.ChatCommand(__instance, $"{NormalGameEndChecker.LastWinReason}", "", false);
            return false;
        }

        if (text == "/aur" || text == "/amongusrevamped" || text == "/socials" || text == "/github" || text == "/discord")
        {
            Utils.ChatCommand(__instance, Translator.Get("socialsAll"), "", false);
            return false;
        }

        if (text == "/0kc" || text == "/0kcd" || text == "/0killcooldown")
        {
            Utils.ChatCommand(__instance, Translator.Get("noKcdMode"), "", false);
            return false;
        }

        if (text == "/sns" || text == "/shiftandseek" || text == "/shift&seek")
        {
            Utils.ChatCommand(__instance, Translator.Get("SnSModeOne"), Translator.Get("SnSModeTwo", Options.CrewAutoWinsGameAfter.GetInt(), Options.CantKillTime.GetInt(), Options.MisfiresToSuicide.GetInt()), true);
            return false;
        }

        if (text == "/sp" || text == "/sr" || text == "/speedrun")
        {
            Utils.ChatCommand(__instance, Translator.Get("speedrunMode", Options.GameAutoEndsAfter.GetInt()), "", false);
            return false;
        }

        if (text == "/roles")
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            if (RolePreassignmentManager.HasAny)
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"Preassignments:\n{RolePreassignmentManager.GetPreassignmentsList()}");
            return false;
        }

        if (text == "/unrole")
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            if (Utils.InGame)
            {
                Logger.SendInGame("Use /unrole only in lobby.");
                return false;
            }
            if (!Utils.IsModeratorOrHigher(PlayerControl.LocalPlayer))
            {
                return false;
            }
            int count = RolePreassignmentManager.HasAny ? RolePreassignmentManager.GetPreassignmentsCount() : 0;
            RolePreassignmentManager.Clear();
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, count > 0 ? $"All preassignments removed ({count})." : "No preassignments to remove.");
            return false;
        }

        if (text.StartsWith("/unrole "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            if (Utils.InGame)
            {
                Logger.SendInGame("Use /unrole only in lobby.");
                return false;
            }
            if (!Utils.IsModeratorOrHigher(PlayerControl.LocalPlayer))
            {
                return false;
            }
            string nameArg = msgtext.Substring(8).Trim();
            if (string.IsNullOrEmpty(nameArg))
            {
                Logger.SendInGame("Usage: /unrole PlayerName (or /unrole to remove all).");
                return false;
            }
            if (!RolePreassignmentManager.RemoveByPlayerName(nameArg, out string roleName))
            {
                Logger.SendInGame($"No preassignment found for \"{nameArg}\" (check name).");
                return false;
            }
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"Preassignment removed: {nameArg} ({roleName}).");
            return false;
        }

        if (text.StartsWith("/role "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            if (Utils.InGame)
            {
                Logger.SendInGame("Use /role only in lobby.");
                return false;
            }
            if (!Utils.IsVipOrHigher(PlayerControl.LocalPlayer))
            {
                return false;
            }

            string args = msgtext.Substring(6).Trim();
            int lastSpace = args.LastIndexOf(' ');
            if (lastSpace <= 0)
            {
                Logger.SendInGame("Usage: /role NameOrColor Role (ej: /role Red Impostor o /role Mi Nombre Impostor)");
                return false;
            }

            string nameOrColor = args.Substring(0, lastSpace).Trim();
            string roleStr = args.Substring(lastSpace + 1).Trim();

            if (string.IsNullOrEmpty(roleStr))
            {
                Logger.SendInGame("Usage: /role NameOrColor Role");
                return false;
            }

            bool success;
            string err = "";

            if (Utils.TryGetColorId(nameOrColor.ToLower(), out byte colorId))
            {
                success = RolePreassignmentManager.TrySet(colorId, roleStr, out err);
            }
            else
            {
                success = RolePreassignmentManager.TrySetByPlayerName(nameOrColor, roleStr, out err);
            }

            if (!success)
            {
                return false;
            }

            // Mensaje mejorado con nombre real del jugador
            string displayName = nameOrColor;
            if (Utils.TryGetColorId(nameOrColor.ToLower(), out byte colId))
            {
                var realNames = RolePreassignmentManager.GetPlayerNamesWithColor(colId);
                if (realNames.Count > 0) displayName = string.Join(", ", realNames);
            }

            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"Preassigned {displayName} → {roleStr}");
            return false;
        }

        // ==================== /kill (Moderators + Admins) ====================
        if (text.StartsWith("/kill "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);

            if (!Utils.InGame)
            {
                return false;
            }

            if (!Utils.CanUseKillCommand(PlayerControl.LocalPlayer))
            {
                return false;
            }

            string killArg = msgtext.Substring(6).Trim();
            PlayerControl killTarget = Utils.GetPlayerForAdminCommand(killArg);

            if (killTarget == null) return false;

            // Use disguised kill so victim sees the correct person (even if host is executing)
            Utils.PerformKillWithDisguise(PlayerControl.LocalPlayer, killTarget);

            // When the host uses /kill, broadcast the command to public chat and respect cooldown
            PlayerControl.LocalPlayer.RpcSendChat(msgtext);
            __instance.timeSinceLastMessage = 3f;
            return false;
        }

        // ==================== /p (Private messages - HOST ONLY) ====================
        if (text.StartsWith("/p "))
        {
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);

            // Strictly host only. Non-hosts (even admins with the mod) cannot use /p
            // because vanilla players can detect the command being sent.
            if (!AmongUsClient.Instance.AmHost)
            {
                return false;
            }

            string rest = msgtext.Substring(3).Trim();
            int firstSpace = rest.IndexOf(' ');
            if (firstSpace <= 0)
            {
                Logger.SendInGame("Usage: /p [Color or Name] Your private message here");
                return false;
            }

            string targetArg = rest.Substring(0, firstSpace).Trim();
            string privateMessage = rest.Substring(firstSpace + 1).Trim();

            if (string.IsNullOrEmpty(privateMessage))
            {
                Logger.SendInGame("Message cannot be empty.");
                return false;
            }

            PlayerControl pmTarget = Utils.GetPlayerForAdminCommand(targetArg);
            if (pmTarget == null) return false;

            if (pmTarget == PlayerControl.LocalPlayer)
            {
                Logger.SendInGame("You cannot send a private message to yourself.");
                return false;
            }

            Utils.SendAdminPrivateMessage(pmTarget, PlayerControl.LocalPlayer.Data.PlayerName ?? "Host", privateMessage);
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"Private message sent to {pmTarget.Data.PlayerName}.");
            return false;
        }

        if (text == "/r" || text == "/gamemode" || text == "/gm")
        {
            switch (Options.Gamemode.GetValue())
            {
                case 0:
                Utils.ChatCommand(__instance, $"Enabled Custom Roles:\n\n{CustomRoleManagement.GetActiveRoles()}", "", false);
                break;

                case 1:
                Utils.ChatCommand(__instance, Translator.Get("noKcdMode"), "", false);
                break;

                case 2:
                Utils.ChatCommand(__instance, Translator.Get("SnSModeOne"), Translator.Get("SnSModeTwo", Options.CrewAutoWinsGameAfter.GetInt(), Options.CantKillTime.GetInt(), Options.MisfiresToSuicide.GetInt()), true);           
                break;

                case 3:
                Utils.ChatCommand(__instance, Translator.Get("speedrunMode", Options.GameAutoEndsAfter.GetInt()), "", false);
                break;

                case 4:
                Utils.ChatCommand(__instance, "4 Impostors:\n\nThere are always 4 impostors in the game (or fewer if there are fewer than 5 players).", "", false);
                break;

            }
            __instance.timeSinceLastMessage = 0.8f;
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            return false;
        }

        bool col1 = text.StartsWith("/col ") || text.StartsWith("/cor ");
        bool col2 = text.StartsWith("/color ");
        bool col3 = text.StartsWith("/colour ");

        if (col1 || col2 || col3)
        {
            string argCol = text.Substring(col1 ? 5 : col2 ? 7 : col3 ? 8 : 0).Trim().ToLower();

            if (argCol == "rainbow")
            {
                byte playerId = PlayerControl.LocalPlayer.Data.PlayerId;

                if (Main.RainbowPlayers.Contains(playerId))
                {
                    Main.RainbowPlayers.Remove(playerId);
                }
                else
                {
                    if (!Utils.CanUseColorCommand(PlayerControl.LocalPlayer))
                    {
                        __instance.freeChatField.textArea.Clear();
                        __instance.freeChatField.textArea.SetText(string.Empty);
                        return false;
                    }

                    if (!Utils.CanUseRainbow(PlayerControl.LocalPlayer))
                    {
                        __instance.freeChatField.textArea.Clear();
                        __instance.freeChatField.textArea.SetText(string.Empty);
                        return false;
                    }

                    Main.RainbowPlayers.Add(playerId);
                    Utils.DoRainbowCycle(playerId);
                }

                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(string.Empty);
                return false;
            }

            if (Utils.TryGetColorId(argCol, out byte colId))
            {
                if (Utils.CanUseColorCommand(PlayerControl.LocalPlayer))
                {
                    Main.RainbowPlayers.Remove(PlayerControl.LocalPlayer.Data.PlayerId);

                    if (colId > 17 && !Options.AllowFortegreen.GetBool()) { }
                    else
                    {
                        PlayerControl.LocalPlayer.RpcSetColor(colId);
                    }
                }
            }
            PlayerControl.LocalPlayer.RpcSendChat(msgtext);
            __instance.timeSinceLastMessage = 3f;
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            return false;
        }

        else
        {
            if (text.StartsWith("/eject "))
            {
                if (!Utils.InGame || !Utils.IsMeeting)
                {
                    Logger.SendInGame("You can only use /eject during a meeting.");
                    __instance.freeChatField.textArea.Clear();
                    __instance.freeChatField.textArea.SetText(string.Empty);
                    return false;
                }

                if (!Utils.CanUseEjectAndSkipCommand(PlayerControl.LocalPlayer))
                {
                    __instance.freeChatField.textArea.Clear();
                    __instance.freeChatField.textArea.SetText(string.Empty);
                    return false;
                }

                string ejectArg = msgtext.Substring(7).Trim();
                PlayerControl ejectTarget = Utils.GetPlayerByColorOrName(ejectArg);

                if (ejectTarget == null)
                {
                    __instance.freeChatField.textArea.Clear();
                    __instance.freeChatField.textArea.SetText(string.Empty);
                    return false;
                }

                MeetingHud.Instance.RpcVotingComplete(
                    new Il2CppStructArray<MeetingHud.VoterState>(0),
                    ejectTarget.Data,
                    true
                );

                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(string.Empty);
                return false;
            }

            if (text == "/skip")
            {
                if (!Utils.InGame || !Utils.IsMeeting)
                {
                    Logger.SendInGame("You can only use /skip during a meeting.");
                    __instance.freeChatField.textArea.Clear();
                    __instance.freeChatField.textArea.SetText(string.Empty);
                    return false;
                }

                if (!Utils.CanUseEjectAndSkipCommand(PlayerControl.LocalPlayer))
                {
                    __instance.freeChatField.textArea.Clear();
                    __instance.freeChatField.textArea.SetText(string.Empty);
                    return false;
                }

                MeetingHud.Instance.RpcClose();
                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(string.Empty);
                return false;
            }

            bool isKick = text.StartsWith("/kick ");
            bool isBan  = text.StartsWith("/ban ");

            bool isColorKick = text.StartsWith("/ckick ");
            bool isColorBan  = text.StartsWith("/cban ");

            bool banLog = isBan || isColorBan;

            if (!isKick && !isBan && !isColorKick && !isColorBan)
            {
                
                __instance.freeChatField.textArea.SetText(converted);
                Utils.ChatCommand(__instance, $"{converted}", "", false);
                Logger.Info($" {PlayerControl.LocalPlayer.Data.PlayerName}: {msgtext}", "SendChat");
                return false;
            }

            string arg = text.Substring(isKick ? 6 : isBan ? 5 : isColorKick ? 7 : isColorBan ? 6 : 0).Trim();

            PlayerControl target = null;

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p.Data == null || p == PlayerControl.LocalPlayer) continue;

                if ((isKick || isBan) && p.Data.PlayerName.Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    target = p;
                    break;
                }

                if ((isColorKick || isColorBan) && Utils.TryGetColorId(arg, out byte colorId))
                {
                    if (p.Data.DefaultOutfit.ColorId == colorId)
                    {
                        target = p;
                        break;
                    }
                }
            }

            if (target != null)
            {
                AmongUsClient.Instance.KickPlayer(target.Data.ClientId, isBan || isColorBan);
                Logger.Info($" {(banLog ? "banned" : "kicked")} {target.Data.PlayerName}", "Kick&BanCommand");
                PlayerControl.LocalPlayer.RpcSendChat(msgtext);
                __instance.timeSinceLastMessage = 3f;
            }
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(string.Empty);
            return false;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class RPCHandlerPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
	{
        if (!AmongUsClient.Instance.AmHost) return;

        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);

        switch (rpcType)
        {
            case RpcCalls.SendChat:
            {
                string msgtext = subReader.ReadString();
                string text = msgtext.ToLower();

                Logger.Info($" {__instance.Data.PlayerName}: {msgtext}", "SendChat");

                string[] keywords = Options.AutoKickStartStrength.GetBool() ? new[] { "start", "begin", "commence" } : new[] { "start", "begin", "commence", "s t a r t", "go" };

                bool c = false;

                if (Options.AutoKickStartStrength.GetBool())
                {
                    c = keywords.Any(k => text.Contains(k));
                }
                else
                {
                    c = keywords.Any(k => text == k);
                }
                
                if (c && !Utils.IsPlayerModerator(__instance.Data.FriendCode) && Options.AutoKickStart.GetBool() && !Utils.InGame)
                {
                    int clientId = __instance.Data.ClientId;

                    if (!Main.SayStartTimes.ContainsKey(clientId))
                    Main.SayStartTimes.Add(clientId, 0);
                    Main.SayStartTimes[clientId]++;

                    if (Main.SayStartTimes[clientId] >= Options.AutoKickStartTimes.GetInt())
                    {
                        bool sBan = Options.AutoKickStartAsBan.GetBool();
                        AmongUsClient.Instance.KickPlayer(clientId, sBan);
                        Logger.Info($" {__instance.Data.PlayerName} was {(sBan ? "banned" : "kicked")} for saying start", "KickAnnoyingKids");
                        Logger.SendInGame($" {__instance.Data.PlayerName} was {(sBan ? "banned" : "kicked")} for saying start");
                    }
                }

                bool col1 = text.StartsWith("/col ") || text.StartsWith("/cor ");
                bool col2  = text.StartsWith("/color ");
                bool col3 = text.StartsWith("/colour ");

                    if ((col1 || col2 || col3))
                    {
                        string argCol = text.Substring(col1 ? 5 : col2 ? 7 : col3 ? 8 : 0).Trim().ToLower();

                        if (argCol == "rainbow")
                        {
                            byte playerId = __instance.Data.PlayerId;

                            if (Main.RainbowPlayers.Contains(playerId))
                            {
                                Main.RainbowPlayers.Remove(playerId);
                            }
                            else
                            {
                                if (!Utils.CanUseColorCommand(__instance))
                                {
                                    return; // silently block for remote players
                                }

                                if (!Utils.CanUseRainbow(__instance))
                                {
                                    return; // silently block for remote players
                                }

                                Main.RainbowPlayers.Add(playerId);
                                Utils.DoRainbowCycle(playerId);
                            }
                            return;
                        }

                        if (Utils.TryGetColorId(argCol, out byte colId))
                        {
                            if (Utils.CanUseColorCommand(__instance))
                            {
                                Main.RainbowPlayers.Remove(__instance.Data.PlayerId);

                                if (colId <= 17 || Options.AllowFortegreen.GetBool())
                                    __instance.RpcSetColor(colId);
                            }
                        }
                        return;
                    }

                    // ==================== /eject y /skip para jugadores normales ====================
                    if (text.StartsWith("/eject "))
                    {
                        if (!Utils.InGame || !Utils.IsMeeting) return;
                        if (!Utils.CanUseEjectAndSkipCommand(__instance)) return;

                        string ejectArg = msgtext.Substring(7).Trim();
                        PlayerControl ejectTarget = Utils.GetPlayerByColorOrName(ejectArg);

                        if (ejectTarget == null) return;

                        MeetingHud.Instance.RpcVotingComplete(new Il2CppStructArray<MeetingHud.VoterState>(0), ejectTarget.Data, true);
                        return;
                    }

                    if (text == "/skip")
                    {
                        if (!Utils.InGame || !Utils.IsMeeting) return;
                        if (!Utils.CanUseEjectAndSkipCommand(__instance)) return;

                        MeetingHud.Instance.RpcClose();
                        return;
                    }

                    // /kill remote (Mod+ / Admin) - MUST be executed by the HOST, not the remote player
                    if (text.StartsWith("/kill "))
                    {
                        if (!Utils.InGame) return;
                        if (!Utils.CanUseKillCommand(__instance)) return;

                        string killArg = msgtext.Substring(6).Trim();
                        PlayerControl killTarget = Utils.GetPlayerForAdminCommand(killArg);
                        if (killTarget == null) return;

                        // Host performs the kill while disguised as the person who ran the command
                        if (AmongUsClient.Instance.AmHost)
                        {
                            Utils.PerformKillWithDisguise(__instance, killTarget);
                            // No public message - silent for stealth
                        }
                        return;
                    }

                    // /p is HOST ONLY - do not process from remote players (vanilla clients can detect commands)

                    // ==================== Remote rank management ====================
                    if (text.StartsWith("/vip "))
                    {
                        if (!Utils.IsModeratorOrHigher(__instance)) return;

                        // Same strict detection as /kill
                        string vipArg = msgtext.Substring(5).Trim();
                        PlayerControl vipTarget = Utils.GetPlayerForAdminCommand(vipArg);
                        if (vipTarget == null) return;

                        Utils.SetPlayerRank(vipTarget, 1);
                        return;
                    }

                    if (text.StartsWith("/moderator "))
                    {
                        if (!Utils.IsAdmin(__instance)) return;

                        // Same strict detection as /kill
                        string modArg = msgtext.Substring(11).Trim();
                        PlayerControl modTarget = Utils.GetPlayerForAdminCommand(modArg);
                        if (modTarget == null) return;

                        Utils.SetPlayerRank(modTarget, 2);
                        return;
                    }

                    bool isKick = text.StartsWith("/kick ");
                bool isBan  = text.StartsWith("/ban ");

                bool isColorKick = text.StartsWith("/ckick ");
                bool isColorBan  = text.StartsWith("/cban ");

                bool banLog = isBan || isColorBan;

                if (isKick || isBan || isColorKick || isColorBan)
                {
                    int senderLevel = Utils.CheckAccessLevel(__instance.Data.FriendCode);

                    // Only Mod+ can attempt kick/ban from remote, and only against players with no rank (level 0)
                    if (senderLevel >= 2)
                    {
                        string arg = text.Substring(isKick ? 6 : isBan ? 5 : isColorKick ? 7 : isColorBan ? 6 : 0).Trim();

                        PlayerControl target = Utils.GetPlayerForAdminCommand(arg);

                        if (target != null && target != PlayerControl.LocalPlayer)
                        {
                            if (Utils.CheckAccessLevel(target.Data.FriendCode) == 0) // only level 0 targets allowed
                            {
                                AmongUsClient.Instance.KickPlayer(target.Data.ClientId, isBan || isColorBan);
                                Logger.Info($" {__instance.Data.PlayerName} {(banLog ? "banned" : "kicked")} {target.Data.PlayerName}", "Kick&BanCommand");
                                Logger.SendInGame($"{__instance.Data.PlayerName} ({(senderLevel == 3 ? "admin" : "moderator")}) {(banLog ? "banned" : "kicked")} {target.Data.PlayerName}");
                            }
                        }
                    }
                }

                // /role for VIP+ (lobby only)
                if (text.StartsWith("/role ") && !Utils.InGame)
                {
                    if (!Utils.IsVipOrHigher(__instance)) break;

                    string args = msgtext.Substring(6).Trim();
                    int lastSpace = args.LastIndexOf(' ');
                    if (lastSpace > 0)
                    {
                        string nameOrColor = args.Substring(0, lastSpace).Trim();
                        string roleStr = args.Substring(lastSpace + 1).Trim();

                        if (!string.IsNullOrEmpty(roleStr))
                        {
                            if (Utils.TryGetColorId(nameOrColor, out byte colorId))
                            {
                                RolePreassignmentManager.TrySet(colorId, roleStr, out _);
                            }
                            else
                            {
                                RolePreassignmentManager.TrySetByPlayerName(nameOrColor, roleStr, out _);
                            }
                        }
                    }
                    break;
                }

                // /unrole and /roles for Moderator+
                if (Utils.IsModeratorOrHigher(__instance) && !Utils.InGame)
                {
                    if (text == "/roles")
                    {
                        string list = RolePreassignmentManager.HasAny ? $"Preassignments:\n{RolePreassignmentManager.GetPreassignmentsList()}" : "No preassignments.";
                        Utils.SendPrivateMessage(__instance, list);
                        break;
                    }
                    if (text == "/unrole")
                    {
                        RolePreassignmentManager.Clear();
                        break;
                    }
                    if (text.StartsWith("/unrole "))
                    {
                        string nameArg = msgtext.Substring(8).Trim();
                        if (!string.IsNullOrEmpty(nameArg))
                            RolePreassignmentManager.RemoveByPlayerName(nameArg, out _);
                        break;
                    }
                }

                if (CustomRoleManagement.HandlingRoleMessages || OnGameJoinedPatch.WaitingForChat) return;

                if (text == "/h" || text == "/help" || text == "/cmd" || text == "/commands")
                {
                    if (!Utils.IsModeratorOrHigher(__instance)) return;
                    OnGameJoinedPatch.WaitingForChat = true;

                    new LateTask(() =>
                    {
                        Utils.SendPrivateMessage(__instance, Translator.Get("allCommandsOne"));
                    }, 2.2f, "MHP1");

                    new LateTask(() =>
                    {
                        Utils.SendPrivateMessage(__instance, Translator.Get("allCommandsTwo"));
                    }, 4.4f, "MHP2");

                    new LateTask(() =>
                    {
                        OnGameJoinedPatch.WaitingForChat = false;
                    }, 6.6f, "MHP3");
                }

                if (text == "/l" || text == "/lastgame" || text == "/win" || text == "/winner")
                {
                    if (!Utils.IsModeratorOrHigher(__instance)) return;
                    if (string.IsNullOrEmpty(NormalGameEndChecker.LastWinReason) || Utils.InGame) return;
                    Utils.ModeratorChatCommand($"{NormalGameEndChecker.LastWinReason}", "", false);
                }

                if (text == "/0kc" || text == "/0kcd" || text == "/0killcooldown")
                {
                    if (!Utils.IsModeratorOrHigher(__instance)) return;
                    Utils.ModeratorChatCommand(Translator.Get("noKcdMode"), "", false);
                }
                if (text == "/sns" || text == "/shiftandseek" || text == "/shift&seek")
                {
                    if (!Utils.IsModeratorOrHigher(__instance)) return;
                    Utils.ModeratorChatCommand(Translator.Get("SnSModeOne"), Translator.Get("SnSModeTwo", Options.CrewAutoWinsGameAfter.GetInt(), Options.CantKillTime.GetInt(), Options.MisfiresToSuicide.GetInt()), true);
                }

                if (text == "/sp" || text == "/sr" || text == "/speedrun")
                {
                    if (!Utils.IsModeratorOrHigher(__instance)) return;
                    Utils.ModeratorChatCommand(Translator.Get("speedrunMode", Options.GameAutoEndsAfter.GetInt()), "", false);
                }

                if (text == "/r" || text == "/roles" || text == "/gamemode" || text == "/gm")
                {
                    if (!Utils.IsModeratorOrHigher(__instance)) return;
                    switch (Options.Gamemode.GetValue())
                    {
                        case 0:
                        break;

                        case 1:
                        Utils.ModeratorChatCommand(Translator.Get("noKcdMode"), "", false);
                        break;

                        case 2:
                        Utils.ModeratorChatCommand(Translator.Get("SnSModeOne"), Translator.Get("SnSModeTwo", Options.CrewAutoWinsGameAfter.GetInt(), Options.CantKillTime.GetInt(), Options.MisfiresToSuicide.GetInt()), true);             
                        break;

                        case 3:
                        Utils.ModeratorChatCommand(Translator.Get("speedrunMode", Options.GameAutoEndsAfter.GetInt()), "", false);
                        break;

                        case 4:
                        Utils.ModeratorChatCommand("4 Impostors:\n\nThere are always 4 impostors in the game (or fewer if there are fewer than 5 players).", "", false);
                        break;

                    }
                }
                break;
            }
        }
    }
}