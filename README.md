# AmongUsRevamped Enhanced – Role Pre-Assignment Edition! 🚀

<p align="center">
  <a href="https://github.com/YOUR_USERNAME/AmongUsRevamped-Enhanced/releases/latest" target="_blank">
    <img src="https://img.shields.io/badge/Download%20Latest-Release-blue?style=for-the-badge&logo=github&logoColor=white"/>
  </a>
  <br><br>
  <i>A powerful host-only enhancement for Among Us – now with full <b>role pre-assignment</b> for ultimate lobby control!</i>
</p>

## 🔥 What's New in This Enhanced Version?
This is an **independent modification** of the original AmongUsRevamped, focused on adding advanced **role pre-assignment** features while keeping everything else smooth and compatible.

### ⭐ Brand New: Role Pre-Assignment System (v2.0.0)
Take full control of role distribution before the game starts!

- **/role <Color> <RoleName>** → Pre-assign a specific role to a player by their color (e.g., `/role Red Shapeshifter`).
- **/roles** → List all current pre-assignments (shows player names/colors and their assigned roles – local view only).
- **/unrole <PlayerName>** → Remove pre-assignment for a specific player.
- **/unrole** (no args) → Clear **all** pre-assignments at once.

**Smart & Balanced Behavior:**
- Respects your current game settings (e.g., if 2 Impostors + 1 Shapeshifter + 1 Phantom are enabled → no duplicates allowed).
- If a pre-assigned Impostor slot is left empty, the game will randomly assign another player to fill it (no broken games!).
- Works perfectly in public lobbies – host-only power!

All other original features remain intact and fully functional.

### ⭐ Improved: Moderator System (v2.1.0)
The moderator system has been overhauled for easier management and clearer behavior.

- **/moderator &lt;Color&gt;** (host only) → Toggle moderator status by player color (e.g. `/moderator Red`). Adds the player as moderator if they aren’t one, or removes them if they already are. Feedback appears **only in your local chat** (e.g. “PlayerName added as moderator” / “PlayerName removed from moderators”).
- **Same commands as host** → Moderators can use the same chat commands as the host (kick/ban, /help, /lastgame, /0kc, /sns, /speedrun, /roles, etc.), **respecting System settings**: e.g. if “Who can use Color Commands” is set to *Nobody*, only the host can use `/color`; if *Moderators* or *Everyone*, moderators can use it too. “Moderators can use commands” must be enabled for moderator-only commands.
- **Kick/ban accountability** → When a moderator kicks or bans someone via command, the host sees in the notifier who did it (e.g. “PlayerName (moderator) kicked TargetName”).
- **Role pre-assignment** → Moderators can use **/role** and **/unrole** in the lobby; the action is applied with **no chat message**. They can see the current preassignments with **/roles** (the list is sent to them privately). The host still sees the list in chat when using **/roles**.

All moderator list changes are saved to `AUR-DATA/ModeratorList.txt` (one Friend Code per line).

## 🎮 Full Feature List (Original + Enhancements)
### Client-Side Tweaks
- Game Master mode
- Unlock / Show FPS
- Dark Theme
- Toggle Lobby Music
- Customizable strings

### Technical & Moderation Tools
- Auto-kick/ban low-level players, invalid FriendCodes, "start" spammers...
- DenyName system
- **Moderator & Banlist systems** (in-game `/moderator <Color>` to add/remove moderators; moderators share host commands and can preassign roles; host sees moderator kick/ban actions)
- No Game End option
- Custom countdown

### Automation Magic
- Auto-start game with conditions
- Auto-rejoin lobby
- Auto-send winner info

### Gameplay Customizations
- Dead Impostors can sabotage
- Disable specific sabotages / doors / meetings / bodies / devices
- Override decontamination
- Enable/disable tasks individually
- Same tasks for everyone
- Hide and Seek: Custom Impostor count

### Custom Gamemodes
- 0 Kill Cooldown
- Shift and Seek
- Speedrun

### Extra Polish
- ehT dlekS (reverse Skeld map)
- Cancel countdown
- Improved menu & faster startup
- Custom anticheat
- Zoom out (lobby/dead)
- Unlimited option ranges
- Better logging
- **And yes – everything works in public lobbies!** 🎉

### Hotkeys & Commands
(Full list from original – plus /role, /roles, /unrole, and **/moderator &lt;Color&gt;** for moderator management.)

## 📥 How to Install
<<<<<<< HEAD
- **With BepInEx already installed**: Drop the `AUR.v2.1.5.dll` (or your renamed .dll) into `Among Us/BepInEx/plugins/`
=======
- **With BepInEx already installed**: Drop the `AUR.v2.1.8.dll` (or your renamed .dll) into `Among Us/BepInEx/plugins/`
>>>>>>> 9d5fda6 (Updated version and kick and ban commands now are visible in the chat)
- **Clean install**: Download the latest .zip from Releases → Extract directly into your Among Us folder

Check the original guide for exact folder locations (Steam, Epic, Itch, etc.).

## ⚠️ Important Notes
- Host-only mod – no modded protocol needed.
- Use responsibly in public lobbies.
- This mod is **not affiliated** with Innersloth LLC. Among Us © Innersloth LLC.

## ❤️ Credits & Special Thanks
This enhanced version is built independently on top of the amazing foundation created by **ApeMV**.

**Original Project:**  
[AmongUsRevamped by ApeMV](https://github.com/apemv/AmongUsRevamped)  
Thank you for the incredible base mod that made all of this possible! 🙌

**Additional credits from original (included here):**  
- [AUnlocker](https://github.com/astra1dev/AUnlocker) – NumberOptionsPatch  
- [EHR (Formerly TOHE+)](https://github.com/Gurge44/EndlessHostRoles) – Task Patching  
- [TOH](https://github.com/tukasa0001/TownOfHost) – Ban Manager & Options  
- [Super New Roles (SNR)](https://github.com/ykundesu/SuperNewRoles) – Credentials menu  
- [TOHY](https://github.com/Yumenopai/TownOfHost_Y) – Zoom  
- [MoreGamemodes](https://github.com/Rabek009/MoreGamemodes) – Chat Patches & OptionItem System  

Licensed under the **GNU General Public License v3.0** (same as original).  
See the [LICENSE](LICENSE) file for full details.

Enjoy controlling your lobbies like never before!