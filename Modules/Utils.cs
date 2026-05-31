using System.Data;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System;
using System.Security.Cryptography;
using System.Text;
using AmongUs.InnerNet.GameDataMessages;

namespace AmongUsRevamped;

public static class Utils
{
    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartTime = DateTime.UtcNow;
    private static readonly long EpochStartSeconds = (long)(StartTime - Epoch).TotalSeconds;
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    public static long TimeStamp => EpochStartSeconds + (long)Stopwatch.Elapsed.TotalSeconds;

    public static int allAlivePlayersCount => AllAlivePlayerControls.Count();
    public static int AliveCrewmates => AllAlivePlayerControls.Count(pc => !pc.Data.Role.IsImpostor);
    public static int AliveImpostors => AllAlivePlayerControls.Count(pc => pc.Data.Role.IsImpostor || pc.isNew);

    public static bool IsLobby => AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined;
    public static bool InGame => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;
    public static bool isHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;
    public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;

    public static bool IsShip => ShipStatus.Instance != null;
    public static bool CanMove => PlayerControl.LocalPlayer?.CanMove is true;
    public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;

    public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool IsMeeting => InGame && (MeetingHud.Instance);
    public static bool GamePastRoleSelection => Main.GameTimer > 10f;
    public static bool HandlingGameEnd;
    public static byte CustomGameOverReason;
    public static bool CanCallMeetings;

    public static string ColorString(Color32 color, string str) => $"<#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    public static string ColorToHex(Color32 color) => $"#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}";
    public static byte GetActiveMapId() => GameOptionsManager.Instance.CurrentGameOptions.MapId;

    public static bool IsPlayerModerator(string friendCode)
    {
        return BanManager.IsInModeratorList(friendCode);
    }

    public static bool IsPlayerAdmin(string friendCode)
    {
        return BanManager.IsInAdminList(friendCode);
    }

    public static bool IsPlayerVip(string friendCode)
    {
        return BanManager.IsInVipList(friendCode);
    }

    /// <summary>
    /// Returns access level: 3=Admin, 2=Moderator, 1=VIP, 0=Everyone.
    /// Host is always treated as Admin (3) for permission purposes.
    /// </summary>
    public static int CheckAccessLevel(string friendCode)
    {
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer?.Data?.FriendCode == friendCode)
            return 3; // Host is always Admin level for our rules

        if (BanManager.IsInAdminList(friendCode)) return 3;
        if (BanManager.IsInModeratorList(friendCode)) return 2;
        if (BanManager.IsInVipList(friendCode)) return 1;
        return 0;
    }

    /// <summary>
    /// True if this player can use /color.
    /// Levels: 0 = Nobody (only host), 1 = Ranked Players (applies VIP+ restrictions), 2 = Everyone.
    /// </summary>
    public static bool CanUseColorCommand(PlayerControl player)
    {
        if (player?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && player.Data.ClientId == AmongUsClient.Instance.ClientId) return true;

        int level = Options.ColorCommandLevel.GetValue();
        if (level == 0) return false;           // Nobody
        if (level == 2) return true;            // Everyone

        // Ranked Players (middle option) - apply normal tier restrictions (VIP+)
        return IsVipOrHigher(player);
    }

    public static bool CanUseEjectAndSkipCommand(PlayerControl player)
    {
        if (player?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && player.Data.ClientId == AmongUsClient.Instance.ClientId) return true;

        int level = Options.EjectAndSkipCommandLevel.GetValue();

        if (level == 0) return false;           // Nobody

        // Special rule for dead players: only Host and Admins can use eject/skip when dead.
        // Moderators and normal players are blocked even if the option is "Everyone" or "Ranked Players".
        if (player.Data.IsDead)
        {
            return IsAdmin(player);
        }

        if (level == 2) return true;            // Everyone

        // Ranked Players - apply normal tier restrictions (Moderator+)
        return IsModeratorOrHigher(player);
    }

    /// <summary>
    /// True if this player can use /kill.
    /// Levels: 0 = Nobody (only host), 1 = Ranked Players (Moderator+), 2 = Everyone.
    /// </summary>
    public static bool CanUseKillCommand(PlayerControl player)
    {
        if (player?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && player.Data.ClientId == AmongUsClient.Instance.ClientId) return true;

        int level = Options.KillCommandLevel.GetValue();
        if (level == 0) return false;           // Nobody
        if (level == 2) return true;            // Everyone

        // Ranked Players - Moderator+
        return IsModeratorOrHigher(player);
    }

    // ===== New tier-based helpers matching user's exact rules (v2.2.5) =====

    /// <summary>VIP+ (level >= 1). Host always true.</summary>
    public static bool IsVipOrHigher(PlayerControl player)
    {
        if (player?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && player.Data.ClientId == AmongUsClient.Instance.ClientId) return true;
        return CheckAccessLevel(player.Data.FriendCode) >= 1;
    }

    /// <summary>Moderator+ (level >= 2). Host always true.</summary>
    public static bool IsModeratorOrHigher(PlayerControl player)
    {
        if (player?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && player.Data.ClientId == AmongUsClient.Instance.ClientId) return true;
        return CheckAccessLevel(player.Data.FriendCode) >= 2;
    }

    /// <summary>Admin only (level == 3). Host always true.</summary>
    public static bool IsAdmin(PlayerControl player)
    {
        if (player?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && player.Data.ClientId == AmongUsClient.Instance.ClientId) return true;
        return CheckAccessLevel(player.Data.FriendCode) >= 3;
    }

    /// <summary>
    /// For kick/ban from non-host players: only allowed against targets with level 0 (no rank).
    /// Host can always kick/ban anyone.
    /// </summary>
    public static bool CanKickOrBanTarget(PlayerControl actor, PlayerControl target)
    {
        if (actor?.Data == null || target?.Data == null) return false;
        if (AmongUsClient.Instance.AmHost && actor.Data.ClientId == AmongUsClient.Instance.ClientId) return true;

        int actorLevel = CheckAccessLevel(actor.Data.FriendCode);
        int targetLevel = CheckAccessLevel(target.Data.FriendCode);

        // Only Mod+ or Admin can attempt, and only against level 0 targets
        if (actorLevel < 2) return false;
        return targetLevel == 0;
    }

    public static PlayerControl GetPlayerByColorOrName(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return null;
        arg = arg.ToLower().Trim();

        if (TryGetColorId(arg, out byte colorId))
        {
            List<PlayerControl> playersWithColor = new List<PlayerControl>();
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p?.Data != null && p.Data.DefaultOutfit.ColorId == colorId)
                    playersWithColor.Add(p);
            }

            if (playersWithColor.Count == 0)
            {
                Logger.SendInGame("No player has that color.");
                return null;
            }
            if (playersWithColor.Count > 1)
            {
                Logger.SendInGame("Multiple players have that color. Please use their exact name.");
                return null;
            }
            return playersWithColor[0];
        }

        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data != null && p.Data.PlayerName.Equals(arg, StringComparison.OrdinalIgnoreCase))
                return p;
        }

        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data != null && p.Data.PlayerName.ToLower().Contains(arg))
                return p;
        }

        Logger.SendInGame("Player not found.");
        return null;
    }

    /// <summary>
    /// Strict resolution for /kill and /p commands following user's exact rules:
    /// 1. If arg is a valid color name → must have exactly 1 player with that color, else error.
    /// 2. Else try exact name match.
    /// 3. Else try partial name match → must have exactly 1 result, else error.
    /// Returns the player or null (and sends appropriate error via Logger.SendInGame).
    /// </summary>
    public static PlayerControl GetPlayerForAdminCommand(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return null;
        string lowerArg = arg.Trim().ToLower();

        // 1. Color first
        if (TryGetColorId(lowerArg, out byte colorId))
        {
            var matches = new List<PlayerControl>();
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p?.Data != null && p.Data.DefaultOutfit.ColorId == colorId)
                    matches.Add(p);
            }

            if (matches.Count == 0)
            {
                Logger.SendInGame("No player has that color.");
                return null;
            }
            if (matches.Count > 1)
            {
                Logger.SendInGame("Multiple players have that color. Use exact name instead.");
                return null;
            }
            return matches[0];
        }

        // 2. Exact name
        PlayerControl exact = null;
        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data != null && p.Data.PlayerName.Equals(arg, StringComparison.OrdinalIgnoreCase))
            {
                if (exact != null) // multiple exact? (shouldn't happen normally)
                {
                    Logger.SendInGame("Multiple players match that name exactly. Use color or more specific name.");
                    return null;
                }
                exact = p;
            }
        }
        if (exact != null) return exact;

        // 3. Partial name (must be unique)
        var partialMatches = new List<PlayerControl>();
        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data != null && p.Data.PlayerName.ToLower().Contains(lowerArg))
                partialMatches.Add(p);
        }

        if (partialMatches.Count == 0)
        {
            Logger.SendInGame("Player not found.");
            return null;
        }
        if (partialMatches.Count > 1)
        {
            Logger.SendInGame("Multiple players match that name. Be more specific or use color.");
            return null;
        }
        return partialMatches[0];
    }

    public static string GetTabName(TabGroup tab)
    {
        switch (tab)
        {
            case TabGroup.SystemSettings:
                return "System Settings";
            case TabGroup.CustomRoleSettings:
                return "Custom Roles";
            case TabGroup.ModSettings:
                return "Gameplay Settings";
            case TabGroup.GamemodeSettings:
                return "Gamemode Settings";
            default:
                return "";
        }
    }

    public static bool IsCustomOption(NumberOption option)
    {
        return option.GetComponent<OptionItem>() != null;
    }

    public static void DestroyTranslator(this GameObject obj)
    {
        var translator = obj.GetComponent<TextTranslatorTMP>();
        if (translator != null)
        {
            Object.Destroy(translator);
        }
    }

    public static void DestroyTranslator(this MonoBehaviour obj) => obj.gameObject.DestroyTranslator();

    public static void CustomSettingsChangeMessageLogic(this NotificationPopper notificationPopper, OptionItem optionItem, string text, bool playSound)
    {
        if (notificationPopper.lastMessageKey == 10000 + optionItem.Id && notificationPopper.activeMessages.Count > 0)
        {
            notificationPopper.activeMessages[notificationPopper.activeMessages.Count - 1].UpdateMessage(text);
        }
        else
        {
            notificationPopper.lastMessageKey = 10000 + optionItem.Id;
            LobbyNotificationMessage settingmessage = Object.Instantiate(notificationPopper.notificationMessageOrigin, Vector3.zero, Quaternion.identity, notificationPopper.transform);
            settingmessage.transform.localPosition = new Vector3(0f, 0f, -2f);
            settingmessage.SetUp(text, notificationPopper.settingsChangeSprite, notificationPopper.settingsChangeColor, new Action(() =>
            {
                notificationPopper.OnMessageDestroy(settingmessage);
            }));
            notificationPopper.ShiftMessages();
            notificationPopper.AddMessageToQueue(settingmessage);
        }
        if (playSound)
        {
            SoundManager.Instance.PlaySoundImmediate(notificationPopper.settingsChangeSound, false, 1f, 1f, null);
        }
    }

    public static string GetOptionNameSCM(this OptionItem optionItem)
    {
        if (optionItem.Name == "Enable")
        {
            int id = optionItem.Id;
            while (id % 10 != 0)
                --id;
            var optionItem2 = OptionItem.AllOptions.FirstOrDefault(opt => opt.Id == id);
            return optionItem2 != null ? optionItem2.GetName() : optionItem.GetName();
        }
        else
        return optionItem.GetName();
    }

    public static string GetRegionName(IRegionInfo region = null)
    {
        region ??= ServerManager.Instance.CurrentRegion;

        string name = region.Name;

        // Joining games shows incorrect regions
        if (!AmongUsClient.Instance.AmHost)
        {
            name = "";
            return name;
        }

        if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            name = "Server: Local Game";
            return name;
        }

        if (region.PingServer.EndsWith("among.us", StringComparison.Ordinal))
        {
            // Official servers
            name = name switch
            {
                "North America" => "Server: NA",
                "Europe" => "Server: EU",
                "Asia" => "Server: AS",
                _ => name
            };

            return name;
        }

        string ip = region.Servers.FirstOrDefault()?.Ip ?? string.Empty;

        if (ip.Contains("aumods.us", StringComparison.Ordinal) || ip.Contains("duikbo.at", StringComparison.Ordinal))
        {
            // Modded Servers
            if (ip.Contains("au-eu"))
                name = "Server: MEU";
            else if (ip.Contains("au-as"))
                name = "Server: MAS";
            else
                name = "Server: MNA";

            return name;
        }

        if (name.Contains("Niko", StringComparison.OrdinalIgnoreCase))
            name = name.Replace("233(", "-").TrimEnd(')');

        return name;
    }
    
    public static ClientData GetClientById(int id)
    {
        try { return AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Id == id); }
        catch { return null; }
    }

    public static void ClearLeftoverData()
    {
        RpcSetTasksPatch.GlobalTaskIds = null;
        HandlingGameEnd = false;
        OnGameJoinedPatch.AutoStartCheck = false;
        Main.GameTimer = 0f;
        MurderPlayerPatch.misfireCount.Clear();
        LateTask.Tasks.Clear();
        NormalGameEndChecker.ImpCheckComplete = false;
        CreateOptionsPickerPatch.SetDleks2 = false;
        CanCallMeetings = true;
        PlayerControlSetRolePatch.FirstAssign = true;
        RolePreassignmentManager.Clear();
    }

    public static PlayerControl[] AllAlivePlayerControls
    {
        get
        {
            int count = PlayerControl.AllPlayerControls.Count;
            var result = new PlayerControl[count];
            var i = 0;

            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.PlayerId >= 254 || pc.Data.IsDead || (pc.Data.Disconnected)) continue;

                result[i++] = pc;
            }

            if (i == 0) return [];

            Array.Resize(ref result, i);
            return result;
        }
    }

    public static bool IsActive(SystemTypes type)
    {
        try
        {
            if (Utils.IsLobby || !ShipStatus.Instance || !ShipStatus.Instance.Systems.TryGetValue(type, out ISystemType systemType)) return false;

            int mapId = Main.NormalOptions.MapId;

            switch (type)
            {
                case SystemTypes.Electrical:
                {
                    if (mapId == 5) return false;

                    var switchSystem = systemType.CastFast<SwitchSystem>();
                    return switchSystem is { IsActive: true };
                }
                case SystemTypes.Reactor:
                {
                    switch (mapId)
                    {
                        case 2:
                            return false;
                        case 4:
                            var heliSabotageSystem = systemType.CastFast<HeliSabotageSystem>();
                            return heliSabotageSystem != null && heliSabotageSystem.IsActive;
                        default:
                            var reactorSystemType = systemType.CastFast<ReactorSystemType>();
                            return reactorSystemType is { IsActive: true };
                    }
                }
                case SystemTypes.Laboratory:
                {
                    if (mapId != 2) return false;

                    var reactorSystemType = systemType.CastFast<ReactorSystemType>();
                    return reactorSystemType is { IsActive: true };
                }
                case SystemTypes.LifeSupp:
                {
                    if (mapId is 2 or 4 or 5) return false;

                    var lifeSuppSystemType = systemType.CastFast<LifeSuppSystemType>();
                    return lifeSuppSystemType is { IsActive: true };
                }
                case SystemTypes.Comms:
                {
                    if (mapId is 1 or 5)
                    {
                        var hqHudSystemType = systemType.TryCast<HqHudSystemType>();
                        return hqHudSystemType != null && hqHudSystemType.IsActive;
                    }

                    var hudOverrideSystemType = systemType.CastFast<HudOverrideSystemType>();
                    return hudOverrideSystemType is { IsActive: true };
                }
                case SystemTypes.HeliSabotage:
                {
                    if (mapId != 4) return false;

                    var heliSabotageSystem = systemType.CastFast<HeliSabotageSystem>();
                    return heliSabotageSystem != null && heliSabotageSystem.IsActive;
                }
                case SystemTypes.MushroomMixupSabotage:
                {
                    if (mapId != 5) return false;

                    var mushroomMixupSabotageSystem = systemType.CastFast<MushroomMixupSabotageSystem>();
                    return mushroomMixupSabotageSystem != null && mushroomMixupSabotageSystem.IsActive;
                }
                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Logger.Exception(e, "IsActive");
            return false;
        }
    }

    public static void ShowLastResult(byte playerId = byte.MaxValue)
    {
        if (InGame)
        {
            Logger.SendInGame($"Hi, you're currently in-game. Let's use this command afterwards");
            return;
        }

        if (string.IsNullOrEmpty(NormalGameEndChecker.LastWinReason))
        {
            Logger.SendInGame($"Your command was canceled due to not having the required info");
            return;
        }

        PlayerControl.LocalPlayer.RpcSendChat($"{NormalGameEndChecker.LastWinReason}");
    }

    public static bool TryGetColorId(string input, out byte colorId)
    {
        colorId = 0;

        if (Enum.TryParse<Main.ColorToString>(input, true, out var color))
        {
            colorId = (byte)color;
            return true;
        }

        return false;
    }

    public static void SendPrivateMessage(PlayerControl target, string message)
    {
        if (!AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null || target == null || target.Data.ClientId == 255) return;

        // Send a clean vanilla SendChat RPC (callId 13) targeted only to this client.
        // The receiver will see it as a normal chat message from the sender.
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId,
            (byte)RpcCalls.SendChat,
            SendOption.Reliable,
            target.Data.ClientId);

        writer.Write(message); // vanilla SendChat payload is just the string
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    /// <summary>
    /// Sends a private message with the exact format the user requested for /p.
    /// </summary>
    public static void SendAdminPrivateMessage(PlayerControl target, string senderName, string message)
    {
        if (target == null) return;
        string formatted = $"Mensaje privado de {senderName}: {message}";
        SendPrivateMessage(target, formatted);
    }

    public static void ChatCommand(ChatController __instance, string msg, string msg2, bool multi)
    {
        OnGameJoinedPatch.WaitingForChat = true;

        __instance.freeChatField.textArea.Clear();
        __instance.freeChatField.textArea.SetText(string.Empty); 

        PlayerControl.LocalPlayer.RpcSendChat($"{msg}");

        new LateTask(() =>
        {
            if (multi && msg2 != "") 
            {
                PlayerControl.LocalPlayer.RpcSendChat($"{msg2}");
            }

            if (!multi) 
            {
                OnGameJoinedPatch.WaitingForChat = false;
            }
        }, 2.2f, "ChatCommand1");

        if (!multi || msg2 == "") return;

        new LateTask(() =>
        {
            OnGameJoinedPatch.WaitingForChat = false;
        }, 4.4f, "ChatCommand2");
    }

    public static void ModeratorChatCommand(string msg, string msg2, bool multi)
    {
        OnGameJoinedPatch.WaitingForChat = true;

        new LateTask(() =>
        {
            PlayerControl.LocalPlayer.RpcSendChat($"{msg}");
        }, 2.2f, "ModeratorChatCommand1");

        new LateTask(() =>
        {
            if (multi && msg2 != "") 
            {
                PlayerControl.LocalPlayer.RpcSendChat($"{msg2}");
            }

            if (!multi) 
            {
                OnGameJoinedPatch.WaitingForChat = false;
            }
        }, 4.4f, "ModeratorChatCommand2");

        if (!multi || msg2 == "") return;

        new LateTask(() =>
        {
            OnGameJoinedPatch.WaitingForChat = false;
        }, 6.6f, "ModeratorChatCommand3");
    }

    // 0 = no winner. 1 = solo winner. 2 = add winner.
    public static void CustomWinnerEndGame(PlayerControl winner, int winnerType)
    {
        HandlingGameEnd = true;
        MessageWriter writer = AmongUsClient.Instance.StartEndGame();

        if (winnerType == 0)
        {
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                CustomGameOverReason = (byte)GameOverReason.ImpostorsByVote;
                pc.RpcSetRole(AmongUs.GameOptions.RoleTypes.CrewmateGhost, false);
            }
        }

        if (winnerType == 1)
        {
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                CustomGameOverReason = (byte)GameOverReason.ImpostorsByVote;

                if (pc != winner) 
                {
                    pc.RpcSetRole(AmongUs.GameOptions.RoleTypes.CrewmateGhost, false);
                }
                else
                {
                    pc.RpcSetRole(AmongUs.GameOptions.RoleTypes.ImpostorGhost, false);                    
                }
            }
        }

        new LateTask(() =>
        {
            ContinueEndGame((byte)CustomGameOverReason);
        }, 1f, "CustomWinnerEndGame");     
    }

    public static void ContinueEndGame(byte gameOverReason)
    {
        MessageWriter writer = AmongUsClient.Instance.StartEndGame();
        writer.Write(gameOverReason);
        AmongUsClient.Instance.FinishEndGame(writer);
        HandlingGameEnd = false;
        Logger.Info($"{gameOverReason}", "ContinueEndGame");
        Logger.Info(" -------- GAME ENDED --------", "ContinueEndGame");
    }

    public static void DumpLog()
    {
        string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
#if ANDROID
        var f = $"{BanManager.DataPath}/AUR-Logs/{t}";
#else
        string f = $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/AUR-logs/";
#endif
        string filename = $"{f}AUR-{Main.ModVersion}-{t}.log";
        if (!Directory.Exists(f)) Directory.CreateDirectory(f);
        FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
        file.CopyTo(@filename);

        if (PlayerControl.LocalPlayer != null)
        {
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"/Dump command activated\n\nFile: AUR-{Main.ModVersion}-{t}");
#if !ANDROID
            ProcessStartInfo psi = new("Explorer.exe") { Arguments = "/e,/select," + @filename.Replace("/", "\\") };
            Process.Start(psi);
#endif
        }
    }

    private readonly static Dictionary<string, Sprite> CachedSprites = [];
    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadSprite");
        }
        return null;
    }

    private static unsafe Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            Texture2D texture = new(2, 2, TextureFormat.ARGB32, true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var length = stream.Length;
            var byteTexture = new Il2CppStructArray<byte>(length);
            stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(), (int)length));
            ImageConversion.LoadImage(texture, byteTexture, false);
            return texture;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadTextureFromResources");
        }
        return null;
    }

    // ==================== NUEVO: Modo Rainbow para /color rainbow ====================
    public static void DoRainbowCycle(byte playerId)
    {
        if (!Main.RainbowPlayers.Contains(playerId)) return;

        // Buscar al jugador de forma segura
        PlayerControl player = null;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data?.PlayerId == playerId)
            {
                player = p;
                break;
            }
        }

        if (player == null)
        {
            Main.RainbowPlayers.Remove(playerId);
            return;
        }

        int maxColor = Options.AllowFortegreen.GetBool() ? 19 : 18;

        byte currentColor = (byte)player.Data.DefaultOutfit.ColorId;
        int randomValue = UnityEngine.Random.Range(0, maxColor);
        byte newColor = (byte)randomValue;

        // Evitar repetir el mismo color
        while (newColor == currentColor && maxColor > 1)
        {
            randomValue = UnityEngine.Random.Range(0, maxColor);
            newColor = (byte)randomValue;
        }

        player.RpcSetColor(newColor);

        _ = new LateTask(() => DoRainbowCycle(playerId), 0.5f, $"Rainbow_{playerId}");
    }

    // ==================== Temporary Kill Disguise System (for /kill deception) ====================

    private struct OutfitSnapshot
    {
        public string PlayerName;
        public int ColorId;
        public string HatId;
        public string SkinId;
        public string VisorId;
        public string PetId;
        public string NamePlateId;
    }

    private static OutfitSnapshot? _killDisguisePreviousOutfit = null;

    /// <summary>When /kill is executed, disguise the host as the person who ran the command, perform the kill, then restore whatever the host was wearing before (including active MalumMenu disguises).</summary>
    public static void PerformKillWithDisguise(PlayerControl disguiseSource, PlayerControl targetToKill)
    {
        if (!AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null || disguiseSource == null || targetToKill == null)
            return;

        var local = PlayerControl.LocalPlayer;

        // Guardar posición original antes de disfrazarnos
        Vector2 originalPosition = local.GetTruePosition();

        // Snapshot current appearance (this works even if user is already disguised with Malum or anything else)
        _killDisguisePreviousOutfit = new OutfitSnapshot
        {
            PlayerName = local.Data?.PlayerName ?? "",
            ColorId = local.Data?.DefaultOutfit.ColorId ?? local.CurrentOutfit.ColorId,
            HatId = local.Data?.DefaultOutfit.HatId ?? "",
            SkinId = local.Data?.DefaultOutfit.SkinId ?? "",
            VisorId = local.Data?.DefaultOutfit.VisorId ?? "",
            PetId = local.Data?.DefaultOutfit.PetId ?? "",
            NamePlateId = local.Data?.DefaultOutfit.NamePlateId ?? ""
        };

        ApplyFullOutfit(disguiseSource);

        // Delay de 0.5s para que el disfraz completo (incluyendo color) llegue a los clientes.
        // Kill + restauración inmediata del outfit anterior + regreso a la posición original.
        new LateTask(() =>
        {
            local.RpcMurderPlayer(targetToKill, true);

            // Restaurar apariencia
            RestorePreviousOutfit();

            // Teletransportarnos de vuelta a la posición original después de un pequeño delay
            // (siguiendo el patrón de MalumMenu para mayor estabilidad)
            new LateTask(() =>
            {
                local.NetTransform.RpcSnapTo(originalPosition);
            }, 0.2f, "ReturnPositionAfterKill");
        }, 0.5f, "KillDisguiseDelay");
    }

    private static void ApplyFullOutfit(PlayerControl source)
    {
        if (source?.Data == null) return;
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        var src = source.Data.DefaultOutfit;
        int color = src.ColorId;

        local.SetColor(color);
        local.RpcSetColor((byte)color);   // Fuerza el cambio de color para otros clientes
        local.SetHat(src.HatId ?? "", color);
        local.SetSkin(src.SkinId ?? "", color);
        local.SetVisor(src.VisorId ?? "", color);
        local.SetPet(src.PetId ?? "", color);
        local.SetNamePlate(src.NamePlateId ?? "");

        local.Data.DefaultOutfit.ColorId = color;
        local.Data.DefaultOutfit.HatId = src.HatId ?? "";
        local.Data.DefaultOutfit.SkinId = src.SkinId ?? "";
        local.Data.DefaultOutfit.VisorId = src.VisorId ?? "";
        local.Data.DefaultOutfit.PetId = src.PetId ?? "";
        local.Data.DefaultOutfit.NamePlateId = src.NamePlateId ?? "";
        local.Data.DefaultOutfit.PlayerName = source.Data.PlayerName ?? "";

        if (Utils.IsFreePlay) return;

        local.RpcSetHat(src.HatId ?? "");
        local.RpcSetSkin(src.SkinId ?? "");
        local.RpcSetVisor(src.VisorId ?? "");
        local.RpcSetPet(src.PetId ?? "");
        local.RpcSetNamePlate(src.NamePlateId ?? "");
    }

    private static void RestorePreviousOutfit()
    {
        if (!_killDisguisePreviousOutfit.HasValue) return;

        var snap = _killDisguisePreviousOutfit.Value;
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        int color = snap.ColorId;

        local.SetColor(color);
        local.RpcSetColor((byte)color);   // Fuerza el cambio de color al restaurar
        local.SetHat(snap.HatId, color);
        local.SetSkin(snap.SkinId, color);
        local.SetVisor(snap.VisorId, color);
        local.SetPet(snap.PetId, color);
        local.SetNamePlate(snap.NamePlateId);

        local.Data.DefaultOutfit.ColorId = color;
        local.Data.DefaultOutfit.HatId = snap.HatId;
        local.Data.DefaultOutfit.SkinId = snap.SkinId;
        local.Data.DefaultOutfit.VisorId = snap.VisorId;
        local.Data.DefaultOutfit.PetId = snap.PetId;
        local.Data.DefaultOutfit.NamePlateId = snap.NamePlateId;
        local.Data.DefaultOutfit.PlayerName = snap.PlayerName;

        if (Utils.IsFreePlay)
        {
            _killDisguisePreviousOutfit = null;
            return;
        }

        local.RpcSetHat(snap.HatId);
        local.RpcSetSkin(snap.SkinId);
        local.RpcSetVisor(snap.VisorId);
        local.RpcSetPet(snap.PetId);
        local.RpcSetNamePlate(snap.NamePlateId);

        _killDisguisePreviousOutfit = null;
    }

    // ==================== Rank Management Helpers ====================

    /// <summary>
    /// Removes the player from all three rank lists (VIP, Moderator, Admin).
    /// </summary>
    public static void RemovePlayerFromAllRanks(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return;
        BanManager.RemoveVip(friendCode);
        BanManager.RemoveModerator(friendCode);
        BanManager.RemoveAdmin(friendCode);
    }

    /// <summary>
    /// Sets the player to the specified rank, first removing them from any other ranks.
    /// If they are already exactly in that rank, they get removed from it (set to normal).
    /// Returns a nice message for the actor.
    /// </summary>
    public static string SetPlayerRank(PlayerControl target, int targetRankLevel)
    {
        if (target?.Data == null) return "Invalid target.";

        string fc = target.Data.FriendCode ?? "";
        if (string.IsNullOrEmpty(fc))
            return $"{target.Data.PlayerName} has no FriendCode.";

        string playerName = target.Data.PlayerName ?? "Player";

        // Check current rank
        int currentRank = 0;
        if (BanManager.IsInAdminList(fc)) currentRank = 3;
        else if (BanManager.IsInModeratorList(fc)) currentRank = 2;
        else if (BanManager.IsInVipList(fc)) currentRank = 1;

        // If trying to set to the same rank they already have → remove them (toggle off)
        if (currentRank == targetRankLevel && targetRankLevel > 0)
        {
            RemovePlayerFromAllRanks(fc);
            string rankName = targetRankLevel == 1 ? "VIP" : targetRankLevel == 2 ? "Moderator" : "Admin";
            return $"{playerName} removed from {rankName}s.";
        }

        // Remove from all ranks first (as requested)
        RemovePlayerFromAllRanks(fc);

        // Add to the new rank (if not setting to "normal")
        if (targetRankLevel > 0)
        {
            bool success = false;
            string rankName = "";

            switch (targetRankLevel)
            {
                case 1:
                    success = BanManager.AddVip(fc);
                    rankName = "VIP";
                    break;
                case 2:
                    success = BanManager.AddModerator(fc);
                    rankName = "Moderator";
                    break;
                case 3:
                    success = BanManager.AddAdmin(fc);
                    rankName = "Admin";
                    break;
            }

            if (!success)
                return $"Failed to add {playerName} as {rankName}.";

            return $"{playerName} added as {rankName}.";
        }

        return $"{playerName} is now a normal player.";
    }

    /// <summary>
    /// True if this player is allowed to use the rainbow color.
    /// Blocks rainbow if the player has Shapeshifter pre-assigned (in lobby) or currently has the Shapeshifter role (in-game).
    /// </summary>
    public static bool CanUseRainbow(PlayerControl player)
    {
        if (player?.Data == null) return false;

        // In-game: block if they currently have the Shapeshifter role
        if (InGame && player.Data.Role?.Role == RoleTypes.Shapeshifter)
            return false;

        // In lobby: block if they have Shapeshifter pre-assigned
        if (!InGame)
        {
            if (RolePreassignmentManager.HasShapeshifterPreassignment(player.Data.ClientId))
                return false;
        }

        return true;
    }
}