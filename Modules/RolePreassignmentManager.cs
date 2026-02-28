using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AmongUsRevamped;

/// <summary>
/// Gestiona preasignaciones de rol por color. Solo el host las modifica; al iniciar la partida se aplican y se sincronizan a todos los jugadores.
/// </summary>
public static class RolePreassignmentManager
{
    /// <summary>Preasignaciones: ClientId del jugador -> RoleTypes. Así si cambia de color sigue teniendo el rol.</summary>
    private static readonly Dictionary<int, RoleTypes> Preassignments = new();

    private static readonly HashSet<RoleTypes> ImpostorTeamRoles = new()
    {
        RoleTypes.Impostor,
        RoleTypes.Shapeshifter,
        RoleTypes.Viper,
        RoleTypes.Phantom
    };

    private static readonly Dictionary<string, RoleTypes> RoleNameToType = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Crewmate", RoleTypes.Crewmate },
        { "Impostor", RoleTypes.Impostor },
        { "Scientist", RoleTypes.Scientist },
        { "Engineer", RoleTypes.Engineer },
        { "GuardianAngel", RoleTypes.GuardianAngel },
        { "Shapeshifter", RoleTypes.Shapeshifter },
        { "Noisemaker", RoleTypes.Noisemaker },
        { "Phantom", RoleTypes.Phantom },
        { "Tracker", RoleTypes.Tracker },
        { "Detective", RoleTypes.Detective },
        { "Viper", RoleTypes.Viper }
    };

    /// <summary>Roles especiales de tripulante que pueden repartirse según la config (sin incluir Crewmate base ni GuardianAngel, que es solo de fantasma).</summary>
    private static readonly RoleTypes[] CrewmateSpecialRoles =
    {
        RoleTypes.Scientist,
        RoleTypes.Engineer,
        RoleTypes.Tracker,
        RoleTypes.Detective,
        RoleTypes.Noisemaker
    };

    private static readonly System.Random Random = new System.Random();

    /// <summary>Durante la asignación al iniciar partida: cuántos de cada rol se han asignado ya (preasignados + ya dados en esta ronda).</summary>
    private static Dictionary<RoleTypes, int> AssignedThisRun;

    private static bool _runStarted;

    /// <summary>Huecos de impostor que quedan para que el juego asigne aleatoriamente (maxImpostors - preasignados).</summary>
    private static int _remainingImpostorSlots;

    /// <summary>Cuántos impostores no preasignados hemos dejado quedarse ya (no los convertimos a tripulante).</summary>
    private static int _allowedNonPreassignedImpostors;

    public static bool HasAny => Preassignments.Count > 0;

    public static int GetPreassignmentsCount() => Preassignments.Count;

    /// <summary>True si hay al menos una preasignación de rol de impostor (Impostor/Shapeshifter/Viper/Phantom).</summary>
    private static bool HasAnyImpostorPreassignment()
    {
        foreach (var kv in Preassignments)
            if (ImpostorTeamRoles.Contains(kv.Value)) return true;
        return false;
    }

    /// <summary>Jugadores en lobby con este color (ClientId).</summary>
    private static List<int> GetClientIdsWithColor(byte colorId)
    {
        var list = new List<int>();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data == null || p.PlayerId >= 254) continue;
            if ((byte)p.Data.DefaultOutfit.ColorId != colorId) continue;
            list.Add(p.Data.ClientId);
        }
        return list;
    }

    /// <summary>Nombre actual del jugador por ClientId (en lobby o en partida).</summary>
    public static string GetPlayerNameByClientId(int clientId)
    {
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data == null) continue;
            if (p.Data.ClientId == clientId)
                return p.Data.PlayerName ?? "?";
        }
        return null;
    }

    /// <summary>Nombres de los jugadores que tienen actualmente ese color (para el mensaje de confirmación).</summary>
    public static List<string> GetPlayerNamesWithColor(byte colorId)
    {
        var names = new List<string>();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data == null || p.PlayerId >= 254) continue;
            if ((byte)p.Data.DefaultOutfit.ColorId != colorId) continue;
            names.Add(p.Data.PlayerName ?? "?");
        }
        return names;
    }

    public static void Clear()
    {
        Preassignments.Clear();
        EndRoleAssignmentRun();
    }

    /// <summary>Elimina la preasignación de un jugador por ClientId. Devuelve true si había una y se quitó; roleName es el rol que tenía.</summary>
    public static bool RemoveByClientId(int clientId, out string roleName)
    {
        roleName = null;
        if (!Preassignments.TryGetValue(clientId, out var role))
            return false;
        Preassignments.Remove(clientId);
        roleName = GetRoleName(role);
        return true;
    }

    /// <summary>Elimina la preasignación de un jugador por nombre. Devuelve true si se encontró y se eliminó.</summary>
    public static bool RemoveByPlayerName(string playerName, out string roleName)
    {
        roleName = null;
        if (string.IsNullOrWhiteSpace(playerName)) return false;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data == null || p.PlayerId >= 254) continue;
            if (!p.Data.PlayerName.Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            if (!RemoveByClientId(p.Data.ClientId, out roleName)) return false;
            return true;
        }
        return false;
    }

    /// <summary>Inicia la ronda de asignación: cuenta preasignados por rol y huecos restantes para impostores. Llamar al empezar a aplicar roles.</summary>
    public static void BeginRoleAssignmentRun()
    {
        AssignedThisRun = new Dictionary<RoleTypes, int>();
        foreach (var role in CrewmateSpecialRoles)
            AssignedThisRun[role] = CountPreassignedSlotsForRole(role);
        // Contar también preasignaciones de roles especiales de impostor (no el impostor base)
        foreach (var role in ImpostorTeamRoles)
        {
            if (role == RoleTypes.Impostor) continue;
            AssignedThisRun[role] = CountPreassignedSlotsForRole(role);
        }
        int playerCount = GetLobbyPlayerCount();
        int maxImpostors = GetAdjustedNumImpostors(playerCount);
        int preassignedImpostors = CountPreassignedImpostorSlots();
        _remainingImpostorSlots = Math.Max(0, maxImpostors - preassignedImpostors);
        _allowedNonPreassignedImpostors = 0;
        _runStarted = true;
    }

    /// <summary>Termina la ronda de asignación. Llamar al acabar de asignar a todos.</summary>
    public static void EndRoleAssignmentRun()
    {
        AssignedThisRun = null;
        _runStarted = false;
        _remainingImpostorSlots = 0;
        _allowedNonPreassignedImpostors = 0;
    }

    private static void RecordRoleAssigned(RoleTypes role)
    {
        if (AssignedThisRun == null) return;
        if (!AssignedThisRun.TryGetValue(role, out var n))
            AssignedThisRun[role] = 1;
        else
            AssignedThisRun[role] = n + 1;
    }

    private static int GetAssignedCount(RoleTypes role)
    {
        return AssignedThisRun != null && AssignedThisRun.TryGetValue(role, out var n) ? n : 0;
    }

    /// <summary>Devuelve roles de tripulante con plaza libre según la config (Crewmate siempre; demás si max &gt; 0 y aún quedan plazas).</summary>
    private static List<RoleTypes> GetAvailableCrewmateRolesForRandom()
    {
        var list = new List<RoleTypes> { RoleTypes.Crewmate };
        foreach (var role in CrewmateSpecialRoles)
        {
            int max = GetMaxForRole(role);
            if (max <= 0) continue;
            if (GetAssignedCount(role) < max)
                list.Add(role);
        }
        return list;
    }

    private static RoleTypes PickRandomCrewmateRole()
    {
        var available = GetAvailableCrewmateRolesForRandom();
        return available[Random.Next(available.Count)];
    }

    /// <summary>Obtiene el rol preasignado para un jugador (ClientId), o null si no hay.</summary>
    public static RoleTypes? GetRoleForClient(int clientId)
    {
        return Preassignments.TryGetValue(clientId, out var role) ? role : null;
    }

    /// <summary>Texto con todas las preasignaciones para el host (nombre de jugador → rol).</summary>
    public static string GetPreassignmentsList()
    {
        if (Preassignments.Count == 0) return string.Empty;
        var lines = new List<string>();
        foreach (var kv in Preassignments.OrderBy(x => x.Key))
        {
            var name = GetPlayerNameByClientId(kv.Key) ?? $"Client {kv.Key}";
            var roleName = GetRoleName(kv.Value);
            lines.Add($"{name} → {roleName}");
        }
        return string.Join("\n", lines);
    }

    private static string GetRoleName(RoleTypes role)
    {
        foreach (var kv in RoleNameToType)
            if (kv.Value == role) return kv.Key;
        return role.ToString();
    }

    /// <summary>Intenta añadir o actualizar una preasignación. Devuelve true si se aplicó; si no, envía el error con Logger.SendInGame.</summary>
    public static bool TrySet(byte colorId, string roleNameInput, out string errorTag)
    {
        errorTag = null;
        if (string.IsNullOrWhiteSpace(roleNameInput))
        {
            errorTag = "Role name required (e.g. Impostor, Shapeshifter, Scientist).";
            return false;
        }

        var roleName = roleNameInput.Trim();
        if (!RoleNameToType.TryGetValue(roleName, out var roleType))
        {
            errorTag = $"Unknown role: {roleName}. Use: {string.Join(", ", RoleNameToType.Keys)}.";
            return false;
        }

        // GuardianAngel solo debe obtenerse al morir; no se puede preasignar como rol inicial.
        if (roleType == RoleTypes.GuardianAngel)
        {
            errorTag = "Guardian Angel cannot be preassigned; it is a ghost-only role gained after death.";
            return false;
        }

        var opts = Main.NormalOptions;
        if (opts == null)
        {
            errorTag = "Game options not available.";
            return false;
        }

        int playerCount = GetLobbyPlayerCount();
        int maxImpostors = GetAdjustedNumImpostors(playerCount);

        var clientIdsWithColor = GetClientIdsWithColor(colorId);
        if (clientIdsWithColor.Count == 0)
        {
            errorTag = "No player in the lobby has that color.";
            return false;
        }

        bool isImpostorRole = ImpostorTeamRoles.Contains(roleType);
        if (isImpostorRole && maxImpostors == 0)
        {
            errorTag = "With fewer than 4 players there are 0 impostors; cannot preassign impostor role.";
            return false;
        }

        var roleOptions = opts.RoleOptions;
        if (roleOptions == null)
        {
            errorTag = "Role options not available.";
            return false;
        }

        int maxForRole = roleOptions.GetNumPerGame(roleType);
        // Todos los roles especiales (tripulantes e impostores) deben respetar que 0 = desactivado.
        if (maxForRole <= 0 && roleType != RoleTypes.Crewmate && roleType != RoleTypes.Impostor)
        {
            errorTag = $"Role {roleName} is disabled in game options.";
            return false;
        }

        int impostorSlotsAfter = CountPreassignedImpostorSlots();
        int sameRoleCount = 0;
        foreach (int cid in clientIdsWithColor)
        {
            if (Preassignments.TryGetValue(cid, out var oldRole))
            {
                if (oldRole == roleType) { sameRoleCount++; continue; }
                if (ImpostorTeamRoles.Contains(oldRole)) impostorSlotsAfter--;
            }
            if (isImpostorRole) impostorSlotsAfter++;
        }
        int slotsForRoleTypeAfter = CountPreassignedSlotsForRole(roleType) + clientIdsWithColor.Count - sameRoleCount;

        if (impostorSlotsAfter > maxImpostors)
        {
            errorTag = $"Preassigning {roleName} would exceed max impostors ({maxImpostors}).";
            return false;
        }

        if (roleType != RoleTypes.Crewmate && roleType != RoleTypes.Impostor && maxForRole > 0 && slotsForRoleTypeAfter > maxForRole)
        {
            errorTag = $"Preassigning {roleName} would exceed max count for that role ({maxForRole}).";
            return false;
        }

        foreach (int cid in clientIdsWithColor)
            Preassignments[cid] = roleType;
        return true;
    }

    private static int GetLobbyPlayerCount()
    {
        if (GameData.Instance != null) return GameData.Instance.PlayerCount;
        int c = 0;
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p != null && p.Data != null && p.PlayerId < 254) c++;
        return c;
    }

    private static int GetAdjustedNumImpostors(int playerCount)
    {
        if (playerCount < 4) return 0;
        var opts = Main.NormalOptions;
        if (opts == null) return 0;
        int max = opts.NumImpostors;
        if (max <= 0) return 0;
        return Math.Min(max, playerCount - 1);
    }

    private static int CountPlayersWithColor(byte colorId)
    {
        int n = 0;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p?.Data == null || p.PlayerId >= 254) continue;
            if ((byte)p.Data.DefaultOutfit.ColorId == colorId) n++;
        }
        return n;
    }

    private static int CountPreassignedImpostorSlots()
    {
        int n = 0;
        foreach (var kv in Preassignments)
            if (ImpostorTeamRoles.Contains(kv.Value)) n++;
        return n;
    }

    private static int CountPreassignedSlotsForRole(RoleTypes role)
    {
        int n = 0;
        foreach (var kv in Preassignments)
            if (kv.Value == role) n++;
        return n;
    }

    private static int GetMaxForRole(RoleTypes role)
    {
        var opts = Main.NormalOptions;
        if (opts?.RoleOptions == null) return 0;
        return opts.RoleOptions.GetNumPerGame(role);
    }

    /// <summary>True si las plazas de este rol ya están cubiertas por preasignaciones (no puede tocar por suerte a nadie más).</summary>
    private static bool IsRoleFilledByPreassignments(RoleTypes role)
    {
        if (role == RoleTypes.Crewmate || role == RoleTypes.Impostor) return false;
        int maxForRole = GetMaxForRole(role);
        if (maxForRole <= 0) return false;
        // Usar el total asignado en esta ronda (preasignados + dados aleatoriamente)
        return GetAssignedCount(role) >= maxForRole;
    }

    /// <summary>Devuelve el rol que debe tener este jugador al iniciar (por ClientId). Preasignación tiene prioridad; si hay que reemplazar, se asigna un rol aleatorio según la config.</summary>
    public static RoleTypes GetRoleToApply(int clientId, RoleTypes gameAssignedRole)
    {
        if (Preassignments.Count == 0) return gameAssignedRole;

        if (!_runStarted)
        {
            BeginRoleAssignmentRun();
        }

        if (Preassignments.TryGetValue(clientId, out var preassigned))
            return preassigned;

        // No permitir que nadie empiece como GuardianAngel: si el juego lo asigna, convertirlo a otro rol de tripulante.
        if (gameAssignedRole == RoleTypes.GuardianAngel)
        {
            RoleTypes pickedGa = PickRandomCrewmateRole();
            RecordRoleAssigned(pickedGa);
            return pickedGa;
        }

        bool isImpostorRole = ImpostorTeamRoles.Contains(gameAssignedRole);
        if (HasAnyImpostorPreassignment() && isImpostorRole)
        {
            if (_allowedNonPreassignedImpostors < _remainingImpostorSlots)
            {
                // Hay huecos de impostor, pero debemos respetar el cupo de roles especiales (Shapeshifter, Viper, Phantom, etc.).
                if (IsRoleFilledByPreassignments(gameAssignedRole))
                {
                    // Si el rol especial está lleno, intentar degradar a impostor base si hay hueco.
                    if (gameAssignedRole != RoleTypes.Impostor && !IsRoleFilledByPreassignments(RoleTypes.Impostor))
                    {
                        _allowedNonPreassignedImpostors++;
                        RecordRoleAssigned(RoleTypes.Impostor);
                        return RoleTypes.Impostor;
                    }

                    // Si tampoco hay hueco para más impostores, convertir a tripulante.
                    RoleTypes pickedCrew = PickRandomCrewmateRole();
                    RecordRoleAssigned(pickedCrew);
                    return pickedCrew;
                }

                _allowedNonPreassignedImpostors++;
                RecordRoleAssigned(gameAssignedRole);
                return gameAssignedRole;
            }
            RoleTypes picked = PickRandomCrewmateRole();
            RecordRoleAssigned(picked);
            return picked;
        }

        if (IsRoleFilledByPreassignments(gameAssignedRole))
        {
            RoleTypes picked = PickRandomCrewmateRole();
            RecordRoleAssigned(picked);
            return picked;
        }

        RecordRoleAssigned(gameAssignedRole);
        return gameAssignedRole;
    }
}
