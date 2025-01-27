using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace OSBase.Modules;

// Defines the Damage Report Module
public class DamageReportModule : IModule {
    // Module name property
    public string ModuleName => "DamageReportModule";
    private OSBase? osbase; // Reference to the main OSBase instance

    // 3D arrays to track hitbox-specific data

    private Dictionary<int, HashSet<int>> killedPlayer = new();
    private Dictionary<int, Dictionary<int, Dictionary<int, int>>> hitboxGivenDamage = new();
    private Dictionary<int, Dictionary<int, Dictionary<int, int>>> hitboxTakenDamage = new();
    private Dictionary<int, Dictionary<int, int>> damageGiven = new Dictionary<int, Dictionary<int, int>>();
    private Dictionary<int, Dictionary<int, int>> damageTaken = new Dictionary<int, Dictionary<int, int>>();
    private Dictionary<int, Dictionary<int, int>> hitsGiven = new Dictionary<int, Dictionary<int, int>>();
    private Dictionary<int, Dictionary<int, int>> hitsTaken = new Dictionary<int, Dictionary<int, int>>();
    private Dictionary<int, Dictionary<int, Dictionary<int, int>>> hitboxGiven = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
    private Dictionary<int, Dictionary<int, Dictionary<int, int>>> hitboxTaken = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();

    private Dictionary<int, string> playerNames = new Dictionary<int, string>();


    private HashSet<int> reportedPlayers = new HashSet<int>();

    // Constant to represent environmental kills
    private const int ENVIRONMENT = -1;

    // Names of hit groups for easier identification in reports
    private readonly string[] hitboxName = {
        "Body", "Head", "Chest", "Stomach", "L-Arm", "R-Arm", "L-Leg", "R-Leg", "Neck", "Unknown", "Gear"
    };

    float delay = 3.0f; // Delay in seconds before sending damage reports

    // Module initialization method
    public void Load(OSBase inOsbase, ConfigModule inConfig) {
        osbase = inOsbase; // Set the OSBase reference

        // Register event handlers for various game events
        osbase.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        osbase.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        osbase.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        osbase.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        osbase.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        osbase.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnectEvent);

        Console.WriteLine($"[DEBUG] OSBase[{ModuleName}] loaded successfully!");
    }

    // Event handler for player hurt event
private HookResult OnPlayerHurt(EventPlayerHurt eventInfo, GameEventInfo gameEventInfo) {
    Console.WriteLine("[OnPlayerHurt] 0");

    try {
        // Validate attacker and victim
        if (eventInfo.Attacker == null || eventInfo.Userid == null) {
            Console.WriteLine("[OnPlayerHurt] 1");
            Console.WriteLine("[ERROR] Attacker or victim is null in OnPlayerHurt.");
            return HookResult.Continue;
        }

        if (eventInfo.Attacker?.UserId == null && eventInfo.Userid?.UserId == null) {
            Console.WriteLine("[OnPlayerHurt] 1.5");
            Console.WriteLine("[ERROR] Both Attacker and Victim UserId are null.");
            return HookResult.Continue;
        }

        // Default attacker and victim IDs
        int attacker = eventInfo.Attacker?.UserId ?? ENVIRONMENT; // Default to ENVIRONMENT if null
        int victim = eventInfo.Userid?.UserId ?? -1;

        if (victim == -1) {
            Console.WriteLine("[ERROR] Victim has invalid UserId in OnPlayerHurt.");
            return HookResult.Continue;
        }

        Console.WriteLine($"[OnPlayerHurt] Attacker={attacker}, Victim={victim}");

        int damage = eventInfo.DmgHealth;
        int hitgroup = eventInfo.Hitgroup;

        // Assign environmental damage
        if (attacker == victim && eventInfo.Weapon == "world") {
            Console.WriteLine("[OnPlayerHurt] Environmental damage detected.");
            attacker = ENVIRONMENT;
        }

        Console.WriteLine("[OnPlayerHurt] Validating data structures.");

        // Initialize dictionaries for attacker and victim
        if (!damageGiven.ContainsKey(attacker)) damageGiven[attacker] = new Dictionary<int, int>();
        if (!damageTaken.ContainsKey(victim)) damageTaken[victim] = new Dictionary<int, int>();
        if (!hitsGiven.ContainsKey(attacker)) hitsGiven[attacker] = new Dictionary<int, int>();
        if (!hitsTaken.ContainsKey(victim)) hitsTaken[victim] = new Dictionary<int, int>();
        if (!hitboxGiven.ContainsKey(attacker)) hitboxGiven[attacker] = new Dictionary<int, Dictionary<int, int>>();
        if (!hitboxTaken.ContainsKey(victim)) hitboxTaken[victim] = new Dictionary<int, Dictionary<int, int>>();
        if (!hitboxGiven[attacker].ContainsKey(victim)) hitboxGiven[attacker][victim] = new Dictionary<int, int>();
        if (!hitboxTaken[victim].ContainsKey(attacker)) hitboxTaken[victim][attacker] = new Dictionary<int, int>();

        Console.WriteLine("[OnPlayerHurt] Tracking damage.");

        // Track damage
        damageGiven[attacker][victim] = damageGiven[attacker].GetValueOrDefault(victim, 0) + damage;
        damageTaken[victim][attacker] = damageTaken[victim].GetValueOrDefault(attacker, 0) + damage;

        // Track hits
        hitsGiven[attacker][victim] = hitsGiven[attacker].GetValueOrDefault(victim, 0) + 1;
        hitsTaken[victim][attacker] = hitsTaken[victim].GetValueOrDefault(attacker, 0) + 1;

        // Track hitbox-specific data
        hitboxGiven[attacker][victim][hitgroup] = hitboxGiven[attacker][victim].GetValueOrDefault(hitgroup, 0) + 1;
        hitboxTaken[victim][attacker][hitgroup] = hitboxTaken[victim][attacker].GetValueOrDefault(hitgroup, 0) + 1;

        Console.WriteLine("[OnPlayerHurt] Successfully processed.");
        return HookResult.Continue;
    } catch (Exception ex) {
        Console.WriteLine("[OnPlayerHurt] Exception occurred.");
        Console.WriteLine($"[ERROR] Exception in OnPlayerHurt: {ex.Message}\n{ex.StackTrace}");
        return HookResult.Continue;
    }
}
    // Event handler for player death event
    private HookResult OnPlayerDeath(EventPlayerDeath eventInfo, GameEventInfo gameEventInfo) {
        Console.WriteLine("[OnPlayerDeath] 0");

        int victimId = eventInfo.Userid?.UserId ?? -1;
        Console.WriteLine("[OnPlayerDeath] 1");
        int attackerId = eventInfo.Attacker?.UserId ?? -1;
        Console.WriteLine("[OnPlayerDeath] 2");

        if (attackerId >= 0 && victimId >= 0) {
        Console.WriteLine("[OnPlayerDeath] 3");
            if (!killedPlayer.ContainsKey(attackerId)) {
        Console.WriteLine("[OnPlayerDeath] 4");
                killedPlayer[attackerId] = new HashSet<int>();
            }
        Console.WriteLine("[OnPlayerDeath] 5");
            killedPlayer[attackerId].Add(victimId);
        }
        Console.WriteLine("[OnPlayerDeath] 6");

        // Schedule damage report
        if (eventInfo.Userid != null) {
        Console.WriteLine("[OnPlayerDeath] 7");
            osbase?.AddTimer(delay, () => DisplayDamageReport(eventInfo.Userid));
        }
        Console.WriteLine("[OnPlayerDeath] 8");
        return HookResult.Continue;
    }
    // Event handler for round start
    private HookResult OnRoundStart(EventRoundStart eventInfo, GameEventInfo gameEventInfo) {
        Console.WriteLine("[OnRoundStart] 0");

        ClearDamageData(); // Reset all damage data
        Console.WriteLine("[OnRoundStart] 1");
        UpdatePlayerNames(); // Refresh player names
        Console.WriteLine("[OnRoundStart] 2");
        return HookResult.Continue;
    }

    // Event handler for round end
    private HookResult OnRoundEnd(EventRoundEnd eventInfo, GameEventInfo gameEventInfo) {
        Console.WriteLine("[OnRoundEnd] 0");

        // Add a delay to allow all post-round damage to be recorded
        osbase?.AddTimer(delay, () => {
        Console.WriteLine("[OnRoundEnd] 1");
            var playersList = Utilities.GetPlayers();
        Console.WriteLine("[OnRoundEnd] 2");
            foreach (var player in playersList) {
        Console.WriteLine("[OnRoundEnd] 3");
                if (player.IsValid &&
                    !player.IsHLTV &&
                    player.UserId.HasValue ) {
        Console.WriteLine("[OnRoundEnd] 4");
                    DisplayDamageReport(player);
                } 
        Console.WriteLine("[OnRoundEnd] 5");
            }
        Console.WriteLine("[OnRoundEnd] 6");
        });

        Console.WriteLine("[OnRoundEnd] 7");
        return HookResult.Continue;
    }


    private HookResult OnPlayerDisconnectEvent(EventPlayerDisconnect eventInfo, GameEventInfo gameEventInfo) {
        Console.WriteLine("[OnPlayerDisconnectEvent] 0");

        if (eventInfo.Userid != null) {
        Console.WriteLine("[OnPlayerDisconnectEvent] 1");
            if (eventInfo.Userid?.UserId != null) {
        Console.WriteLine("[OnPlayerDisconnectEvent] 2");
                OnPlayerDisconnect(eventInfo.Userid.UserId.Value);
            }
        Console.WriteLine("[OnPlayerDisconnectEvent] 3");
        }
        Console.WriteLine("[OnPlayerDisconnectEvent] 4");
        return HookResult.Continue;
    }
    // Event handler for player connect
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull eventInfo, GameEventInfo gameEventInfo) {
        Console.WriteLine("[OnPlayerConnectFull] 0");
        UpdatePlayerNames(); // Refresh player names upon connection
        return HookResult.Continue;
    }

    // Update player names by iterating through active players
    private void UpdatePlayerNames() {
        Console.WriteLine("[UpdatePlayerNames] 0");

        try {
            var playersList = Utilities.GetPlayers();
        Console.WriteLine("[UpdatePlayerNames] 1");

            if (playersList == null) {
        Console.WriteLine("[UpdatePlayerNames] 2");
                Console.WriteLine("[ERROR] Players list is null in UpdatePlayerNames.");
                return;
            }
        Console.WriteLine("[UpdatePlayerNames] 3");

            foreach (var player in playersList) {
        Console.WriteLine("[UpdatePlayerNames] 4");
                if (player == null) {
        Console.WriteLine("[UpdatePlayerNames] 5");
                    Console.WriteLine("[DEBUG] Found null player in players list.");
                    continue;
                }
        Console.WriteLine("[UpdatePlayerNames] 6");

                if (player.UserId.HasValue) {
        Console.WriteLine("[UpdatePlayerNames] 7");
                    int playerId = player.UserId.Value;
        Console.WriteLine("[UpdatePlayerNames] 8");

                    // Add or update the player's name in the dictionary
                    playerNames[playerId] = string.IsNullOrEmpty(player.PlayerName) ? "Bot" : player.PlayerName;
        Console.WriteLine("[UpdatePlayerNames] 9");

                    Console.WriteLine($"[DEBUG] Updated player name: ID={playerId}, Name={playerNames[playerId]}");
        Console.WriteLine("[UpdatePlayerNames] 10");
                } else {
        Console.WriteLine("[UpdatePlayerNames] 11");
                    Console.WriteLine("[DEBUG] Player does not have a UserId.");
                }
        Console.WriteLine("[UpdatePlayerNames] 12");
            }
        Console.WriteLine("[UpdatePlayerNames] 13");
        } catch (Exception ex) {
        Console.WriteLine("[UpdatePlayerNames] 14");
            Console.WriteLine($"[ERROR] Exception in UpdatePlayerNames: {ex.Message}\n{ex.StackTrace}");
        }
        Console.WriteLine("[UpdatePlayerNames] 15");
    }

    // Display damage report for a specific player
    private void DisplayDamageReport(CCSPlayerController player) {
        Console.WriteLine("[DisplayDamageReport] 0");

        if (player == null || player.UserId == null) return;
        Console.WriteLine("[DisplayDamageReport] 1");
        int playerId = player.UserId.Value;
        Console.WriteLine("[DisplayDamageReport] 2");

        if (reportedPlayers.Contains(playerId)) return;
        Console.WriteLine("[DisplayDamageReport] 3");
        reportedPlayers.Add(playerId);
        Console.WriteLine("[DisplayDamageReport] 4");

        bool hasVictimData = damageGiven.ContainsKey(playerId) && damageGiven[playerId].Count > 0;
        Console.WriteLine("[DisplayDamageReport] 5");
        bool hasAttackerData = damageTaken.ContainsKey(playerId) && damageTaken[playerId].Count > 0;
        Console.WriteLine("[DisplayDamageReport] 6");

        if (hasVictimData || hasAttackerData) {
        Console.WriteLine("[DisplayDamageReport] 7");
            Console.WriteLine($"===[ Damage Report for {playerNames.GetValueOrDefault(playerId, "Unknown")} ]===");
//            player.PrintToChat("===[ Damage Report (hits:damage) ]===");
        }
        Console.WriteLine("[DisplayDamageReport] 8");

        if (hasVictimData) {
        Console.WriteLine("[DisplayDamageReport] 9");
            Console.WriteLine($"Victims:");
//            player.PrintToChat($"Victims:");
        Console.WriteLine("[DisplayDamageReport] 10");
            foreach (var victim in damageGiven[playerId]) {
        Console.WriteLine("[DisplayDamageReport] 11");
                string victimName = playerNames.GetValueOrDefault(victim.Key, "Unknown");
                int hits = hitsGiven[playerId].GetValueOrDefault(victim.Key, 0);
                int damage = victim.Value;
                Console.WriteLine($" - {victimName}: {hits} hits, {damage} damage");
//                player.PrintToChat($" - {victimName}: {hits} hits, {damage} damage");
            }
        Console.WriteLine("[DisplayDamageReport] 12");
        }

        Console.WriteLine("[DisplayDamageReport] 13");
        if (hasAttackerData) {
        Console.WriteLine("[DisplayDamageReport] 14");
            Console.WriteLine($"Attackers:");
//            player.PrintToChat($"Attackers:");
            foreach (var attacker in damageTaken[playerId]) {
        Console.WriteLine("[DisplayDamageReport] 15");
                string attackerName = playerNames.GetValueOrDefault(attacker.Key, "Unknown");
                int hits = hitsTaken[playerId].GetValueOrDefault(attacker.Key, 0);
                int damage = attacker.Value;
                Console.WriteLine($" - {attackerName}: {hits} hits, {damage} damage");
//                player.PrintToChat($" - {attackerName}: {hits} hits, {damage} damage");
            }
        Console.WriteLine("[DisplayDamageReport] 16");
        }
        Console.WriteLine("[DisplayDamageReport] 17");
    }


    // Helper method to clear all damage-related data
    private void ClearDamageData() {
        Console.WriteLine("[ClearDamageData] 0");
        damageGiven.Clear();
        Console.WriteLine("[ClearDamageData] 1");
        damageTaken.Clear();
        Console.WriteLine("[ClearDamageData] 2");
        hitsGiven.Clear();
        Console.WriteLine("[ClearDamageData] 3");
        hitsTaken.Clear();
        Console.WriteLine("[ClearDamageData] 4");
        hitboxGiven.Clear();
        Console.WriteLine("[ClearDamageData] 5");
        hitboxTaken.Clear();
        Console.WriteLine("[ClearDamageData] 6");
        killedPlayer.Clear();
        Console.WriteLine("[ClearDamageData] 7");
        reportedPlayers.Clear();
        Console.WriteLine("[ClearDamageData] 8");
    }
    private void OnPlayerDisconnect(int playerId) {
        Console.WriteLine("[OnPlayerDisconnect] 0");
        damageGiven.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 1");
        damageTaken.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 2");
        hitsGiven.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 3");
        hitsTaken.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 4");
        hitboxGiven.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 5");
        hitboxTaken.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 6");
        killedPlayer.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 7");
        playerNames.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 8");
        reportedPlayers.Remove(playerId);
        Console.WriteLine("[OnPlayerDisconnect] 9");
    }
}
