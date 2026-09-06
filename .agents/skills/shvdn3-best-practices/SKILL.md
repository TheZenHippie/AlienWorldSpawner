# SHVDN3 Best Practices Agent Skill

## Description
Provides optimization rules, structuring patterns, and memory safety checklists for writing Grand Theft Auto V scripts using the Script Hook V .NET v3 (SHVDN3) API framework.

## Activation Trigger Keywords
- "Write a GTA V script"
- "SHVDN3" / "ScriptHookVDotNet3"
- "GTA V C# mod"
- "Fix GTA V mod crash"

## Core Implementation Rules

### 1. Script Lifecycle & Architecture
- All core entry points must inherit from `GTA.Script`.
- Hook events (`Tick`, `KeyDown`, `KeyUp`) securely inside the constructor.
- **CRITICAL:** Always hook the `Aborted` event to clean up spawned entities, blips, or persistent states.

### 2. Performance & Thread Optimization
- Keep `OnTick` loops lean. Avoid instantiation or heavy array/linq filtering within the main tick callback.
- Use `Script.Yield()` instead of `System.Threading.Thread.Sleep()` to prevent locking up the entire game frame execution.
- Cache player and entity references natively (e.g., store `Game.Player.Character` globally if called multiple times within a tick frame).

### 3. Safe Memory & Native Cleanups
- Use `Entity.Delete()` or `Entity.MarkAsNoLongerNeeded()` on vehicles, peds, and props when they are out of range or when the script terminates.
- Avoid memory leaks with textures and custom UI elements by explicitly disposing of resource wrappers.

### 4. Native Invocation Patterns
- When the high-level API lacks a feature, fall back to native functions using `GTA.Native.Function.Call`.
- Provide correct Type casting when passing parameters (e.g., use `Hash` or `int` explicitly for hashes, `Model` objects, or entity IDs).

### 5. LemonUI Menu Management & Lifecycle
- **Object Pooling:** Always instantiate a `ObjectPool` at the class level to manage your custom menus, containers, and banners.
- **Tick Processing:** Only process the pool (`_pool.Process();`) inside the main `OnTick` method. Do not recreate menus or buttons inside loops.
- **Constructor Building:** Build menus, add items, and bind event handlers (`Activated`, `CheckboxChanged`, etc.) exactly once inside the script constructor or an initialization method.

### 6. Native Game Events & Structuring
- **Game Events Engine:** For listening to built-in GTA V events (like player death, vehicle entry, or weapon switches), utilize native game event checks or listen directly to entity status changes safely.
- **Conditional Throttling:** Do not execute native event checks every frame. Throttle checks using game time intervals (e.g., `Game.GameTime > _nextCheckTime`) to keep the frame rate smooth.
