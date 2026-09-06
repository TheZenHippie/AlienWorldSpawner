---
name: native-events
description: High-performance loop throttling and structured native engine execution wrappers.
activation_keywords: ["Function.Call", "Hash.", "GameTime throttling", "Event emulation"]
languages: ["csharp"]
dependencies: ["shvdn3-core"]
---

# Native Invocation & Game Events Skill

## Description
Guidelines for executing performance-critical native engine calls via Script Hook V .NET v3, safely mapping game event patterns, and implementing conditional loop throttling.

## Activation Trigger Keywords
- "GTA Native Call" / "Function.Call"
- "Hash." / "GET_ENTITY_HEALTH" / "SET_PED_DENSITY"
- "Game event listener" / "Track when player shoots"
- "GameTime throttling" / "Frame rate optimization"

## Core Engineering Rules

### 1. Safe Native Typing & Value Marshalling
- **Strict Handle Passing:** Never hardcode integer handles for active objects. Use structural framework abstractions (`Vehicle.Handle`, `Ped.Handle`, `Prop.Handle`) or safe cast variables when passing objects to native calls.
- **Explicit Enum Typing:** Always pass native functions their matching engine hashes using the built-in `GTA.Native.Hash` enum rather than hexadecimal numbers (e.g., use `Hash.SET_VEHICLE_DIRT_LEVEL` instead of `0x2F361665`).
- **Precision Parameters:** Explicitly match signatures. Pass floats as literals (e.g., `1.0f`) and integers matching expected bit widths to avoid internal native memory crashes.

### 2. High-Performance Loop Throttling
- **Anti-Pattern Prevention:** Do not invoke heavy distance calculation sweeps (`World.GetNearbyPeds`), entity validation loops, or state checks every single frame in the `OnTick` method.
- **Enforced Intermittent Clock:** Throttle non-visual updates using sequential `Game.GameTime` tracking. Define game-time evaluation intervals (e.g., 200ms for tracking distance, 1000ms for environmental cleanups).

### 3. Native Event Emulation & State Buffers
- **Polled Event Strategy:** Because SHVDN3 does not natively support asynchronous, callback-driven native event loops for all game hooks, utilize state-buffering techniques inside your throttled pipelines to trap game events (e.g., comparing current weapons to cached variables to emulate a weapon change event).

---

## Technical Reference Snippet

### Correct Engine Integration & Throttling Blueprint
```csharp
using System;
using GTA;
using GTA.Native;

public class AntigravityNativeSkillExample : Script
{
    // Throttling intervals (in milliseconds)
    private const int PedCheckInterval = 500;
    private const int WeaponTrackInterval = 250;

    // Execution time targets
    private int _nextPedCheckTime = 0;
    private int _nextWeaponTrackTime = 0;

    // State tracking buffers for event emulation
    private WeaponHash _lastCachedWeapon = WeaponHash.Unarmed;

    public AntigravityNativeSkillExample()
    {
        Tick += OnTick;
        Aborted += OnAborted;
    }

    private void OnTick(object sender, EventArgs e)
    {
        Ped playerPed = Game.Player.Character;
        if (playerPed == null || !playerPed.Exists()) return;

        // HIGH PRIORITY FRAME-RATE SENSITIVE PASS
        // (Keep this section empty or completely lean)

        // THROTTLED EVENT PASS 1: Sweep nearby peds every 500ms
        if (Game.GameTime > _nextPedCheckTime)
        {
            _nextPedCheckTime = Game.GameTime + PedCheckInterval;
            EvaluateNearbySurroundings(playerPed);
        }

        // THROTTLED EVENT PASS 2: Track active weapon changes every 250ms
        if (Game.GameTime > _nextWeaponTrackTime)
        {
            _nextWeaponTrackTime = Game.GameTime + WeaponTrackInterval;
            TrackWeaponEvents(playerPed);
        }
    }

    private void EvaluateNearbySurroundings(Ped playerPed)
    {
        // Example: Natively change a setting on all nearby peds safely
        Ped[] nearbyPeds = World.GetNearbyPeds(playerPed.Position, 25.0f);
        
        foreach (Ped ped in nearbyPeds)
        {
            if (ped.Exists() && !ped.IsDead)
            {
                // Safe Native Invocation Pattern using proper casting and Hash enum
                Function.Call(Hash.SET_PED_MAX_HEALTH, ped.Handle, 200);
            }
        }
    }

    private void TrackWeaponEvents(Ped playerPed)
    {
        WeaponHash currentWeapon = playerPed.Weapons.Current.Hash;

        // Custom Event Emulation: Detects exactly when a player switches weapons
        if (currentWeapon != _lastCachedWeapon)
        {
            GTA.UI.Notification.Show(\$"~b~Event:~w~ Equipped weapon changed to {currentWeapon}");
            
            // Clean application of native call tuning parameters
            if (currentWeapon == WeaponHash.RPG)
            {
                // Grant infinite ammo using native function fallback safely
                Function.Call(Hash.SET_PED_INFINITE_AMMO, playerPed.Handle, true, currentWeapon);
            }

            _lastCachedWeapon = currentWeapon;
        }
    }

    private void OnAborted(object sender, EventArgs e)
    {
        // Safe restoration fallback on script crash or unload
        Ped playerPed = Game.Player.Character;
        if (playerPed != null && playerPed.Exists())
        {
            Function.Call(Hash.SET_PED_INFINITE_AMMO, playerPed.Handle, false, _lastCachedWeapon);
        }
    }
}