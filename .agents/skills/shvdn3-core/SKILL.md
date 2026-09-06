---
name: shvdn3-core
description: Foundational lifecycle management and crash-prevention architectures for GTA V C# scripting.
activation_keywords: ["SHVDN3", "ScriptHookVDotNet3", "GTA.Script", "Aborted event", "Script crash"]
languages: ["csharp"]
---

# SHVDN3 Core Scripting Best Practices Skill

## Description
Core script architecture rules, memory safety guidelines, framework lifecycle models, and baseline threading patterns for Script Hook V .NET v3 development.

## Activation Trigger Keywords
- "Write a GTA V script" / "C# script template"
- "SHVDN3" / "ScriptHookVDotNet3"
- "GTA.Script" / "Script lifecycle"
- "Script crash" / "Fix memory leak" / "Aborted event"

## Core Architecture Rules

### 1. The Explicit Lifecycle Pattern
- **Base Inheritance:** Every primary script class must inherit directly from `GTA.Script`.
- **Constructor Event Hooking:** Hook code entry points (`Tick`, `KeyDown`, `KeyUp`, and `Aborted`) securely inside the class constructor exactly once.
- **The Mandatory Abortion Rule:** Every script must hook the `Aborted` event handler. Failing to implement structural state disposal via the `Aborted` handler causes critical persistent game data leaks and engine-level script crashing on hot reloads.

### 2. Thread Safety & Game Loop Processing
- **Game Freeze Prevention:** Never call blocking system commands like `System.Threading.Thread.Sleep()` inside the execution context of the main tick thread. This stops the active game engine thread completely. 
- **The Structural Yield Pattern:** For asynchronous pauses, incremental delays, or sequence holds inside loops, always invoke `Script.Yield()` instead.
- **Null Safety Assertions:** At the beginning of the `OnTick` routine, validate player objects (`Ped playerPed = Game.Player.Character; if (playerPed == null || !playerPed.Exists()) return;`) before accessing inner game mechanics or transforms.

### 3. Entity & Memory Lifetime Management
- **The Persistent State Clean Rule:** All custom entities spawned or manipulated by the script (Vehicles, Peds, Props, Blips) must be tracked in structural runtime tracking variables (e.g., lists or entity handle buffers).
- **Graceful Destruction:** During script abortion, run sequential checks across your tracked collections to call `.Delete()` or `.MarkAsNoLongerNeeded()` on game assets to free up internal engine structural pools.

---

## Technical Reference Snippet

### Correct Base Infrastructure Framework
```csharp
using System;
using System.Windows.Forms;
using GTA;

public class AntigravityCoreSkillExample : Script
{
    // Global tracking buffers to guarantee persistent engine safety
    private Ped _spawnedCompanionPed = null;
    private int _frameMetricCounter = 0;

    public AntigravityCoreSkillExample()
    {
        // 1. Rigorous lifecycle mapping during initialization
        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += OnAborted;
    }

    private void OnTick(object sender, EventArgs e)
    {
        // 2. Strict framework null-guard pass
        Ped playerPed = Game.Player.Character;
        if (playerPed == null || !playerPed.Exists()) return;

        // Execute frame counters safely
        _frameMetricCounter++;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.I)
        {
            SpawnCompanionPedSecurely();
        }
    }

    private void SpawnCompanionPedSecurely()
    {
        Ped playerPed = Game.Player.Character;
        if (playerPed == null || !playerPed.Exists()) return;

        // Clean up pre-existing entities before generating an over-allocation
        CleanSpawnedCompanion();

        // Spawn a structural asset cleanly adjacent to the player character position
        _spawnedCompanionPed = World.CreatePed(PedHash.Chop, playerPed.Position + (playerPed.ForwardVector * 2.0f));
        
        if (_spawnedCompanionPed != null && _spawnedCompanionPed.Exists())
        {
            _spawnedCompanionPed.IsPersistent = true;
            GTA.UI.Notification.Show("Companion entity loaded and cached securely.");
        }
    }

    private void CleanSpawnedCompanion()
    {
        // Assert state exists before executing memory-level deletions
        if (_spawnedCompanionPed != null && _spawnedCompanionPed.Exists())
        {
            _spawnedCompanionPed.Delete();
            _spawnedCompanionPed = null;
        }
    }

    private void OnAborted(object sender, EventArgs e)
    {
        // 3. Clear all custom structures immediately upon script removal or reload
        CleanSpawnedCompanion();
        
        GTA.UI.Notification.Show("Antigravity Core Skill: Script safely aborted, memory pools cleared.");
    }
}