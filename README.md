# Alien World Spawner - GTA V Xenomorph Invasion Mod

[![Platform](https://img.shields.io/badge/Platform-Grand%20Theft%20Auto%20V-blue.svg)](https://www.rockstargames.com/gta-v)
[![Framework](https://img.shields.io/badge/Framework-SHVDN3-orange.svg)](https://github.com/scripthookvdotnet/scripthookvdotnet)
[![UI Library](https://img.shields.io/badge/UI-LemonUI%20SHVDN3-yellow.svg)](https://github.com/Lemon-UI/LemonUI)
[![Runtime](https://img.shields.io/badge/.NET-Framework%204.8-purple.svg)](https://dotnet.microsoft.com/)

**Alien World Spawner** is a high-performance, dynamic Xenomorph outbreak mod for Grand Theft Auto V. Built on **Script Hook V .NET v3 (SHVDN3)** and **LemonUI**, it converts ambient pedestrians into aggressive, fast-moving Xenomorphs that hunt the player, attack civilians, and pull drivers out of moving vehicles.

---

## 🚀 Key Features

* **Interactive LemonUI Menu:** Press **`PgDn`** (configurable via `.ini`) at any time to configure outbreak settings in real time.
* **Customizable Keybind (`.ini`):** Choose your preferred menu activation key via `AlienWorldSpawner.ini` (defaults to `PageDown`).
* **Dynamic Invasion Toggle:** A single checkbox starts the invasion (**"Start Invasion"**) and dynamically transitions to **"End Invasion"** while active.
* **Zero Frame-Drop Architecture:** Powered by a multi-tiered `Game.GameTime` throttling loop instead of heavy per-frame sweeps, eliminating micro-stutters.
* **Aggressive Multi-Target Combat AI:** 
  * Xenomorphs charge relentlessly with `WillRush` combat movement.
  * Mutually hostile toward all civilian demographics, emergency personnel, police, gangs, and the player.
  * Xenomorphs target sedentary pedestrians (benches, chairs, scenario animations) and drag drivers out of cars.
* **Configurable Conversion Radius:** Use the LemonUI slider to adjust how far away NPCs are converted into aliens (from 20m up to 200m).
* **Tunable Health & Armor Sliders:** Customize Xenomorph durability from standard (100 HP) up to juggernaut raid-boss levels (5,000 HP / 3,000 Armor).
* **Ignore Sedentary Pedestrians:** Choose whether pedestrians sitting on benches or using world scenarios are converted or left human for the aliens to hunt.
* **Instant Corpse Removal Option:** Vaporize dead Xenomorphs immediately to optimize memory and keep engine entity pools fresh.
* **Complete Cleanup on Deactivation:** Unchecking the invasion immediately deletes all living and dead Xenomorphs, restoring normal city life.

---

## ⚡ Performance Tip: Managing Entity Pools

> [!TIP]
> **Enable "Instant Corpse Removal" when spawning many aliens!**
> GTA V has an internal engine pool limit of approximately 120–140 active mission/script ped entities. In high-density areas (such as Legion Square or Downtown Vinewood), intense combat can leave dozens of dead Xenomorph corpses on the ground.
> 
> * **With Corpse Removal ON (Checked):** Dead Xenomorphs are vaporized immediately upon death, freeing engine slots for fresh ambient spawning and maximizing FPS during massive invasions.
> * **With Corpse Removal OFF (Unchecked):** Dead Xenomorph corpses remain on the ground for realistic post-battle aftermath, but may consume entity slots until naturally despawned by the game.

---

## 🎮 In-Game Controls & Menu Options

Press **`Page Down`** (`PgDn`) by default to toggle the LemonUI menu.

### ⚙️ Customizing the Menu Keybind (`.ini`)
You can configure the menu toggle key to any key of your choice by editing `scripts/AlienWorldSpawner.ini`:

```ini
[SETTINGS]
; Keybind to toggle the Alien World Spawner menu (Default: PageDown)
; Valid options: PageDown, F10, F9, F8, F7, F6, F5, F3, Insert, Delete, Home, End, I, O, K, L, etc.
MenuKey=PageDown
```

### 📋 Menu Options

| Menu Item | Type | Description |
| :--- | :--- | :--- |
| **Start Invasion / End Invasion** | Checkbox | Toggles the outbreak. Preloads 3D models into memory and sets up group hatred. When unchecked, wipes all living and dead Xenomorphs. |
| **Scan Radius (m)** | Slider | Adjusts the conversion radius around the player (20m up to 200m, default 100m). |
| **Alien Health** | Slider | Adjusts the maximum health for spawned Xenomorphs (100 up to 5,000 HP, default 2,000 HP). |
| **Alien Armor** | Slider | Adjusts the body armor for spawned Xenomorphs (100 up to 3,000 Armor, default 1,000 Armor). |
| **Instant Corpse Removal** | Checkbox | Immediately deletes dead Xenomorph corpses to optimize game performance and prevent ped pool congestion. |
| **Ignore Sedentary Peds** | Checkbox | When enabled, seated NPCs and scenario users remain human (aliens will still attack them). |

---

## 📦 Requirements & Dependencies

1. **[Script Hook V](http://www.dev-c.com/gtav/scripthookv/)** (Latest version by Alexander Blade)
2. **[Script Hook V .NET v3](https://github.com/scripthookvdotnet/scripthookvdotnet/releases)** (`v3.6.0` or newer)
3. **[LemonUI for SHVDN3](https://github.com/Lemon-UI/LemonUI/releases)** (`LemonUI.SHVDN3.dll`)
4. **Xenomorph Add-On Ped Models** installed in your GTA V `mods` folder:
   * `Regular_Xenomorph`
   * `Tall_Xenomorph`
   * `Tallest_Xenomorph`

---

## 🛠️ Installation

1. Ensure **Script Hook V**, **Script Hook V .NET v3**, and **LemonUI.SHVDN3.dll** are installed in your GTA V root and `scripts/` directories.
2. Ensure your Xenomorph add-on ped models are installed in your `mods` folder via OpenIV.
3. Copy `AlienWorldSpawner.dll` and `AlienWorldSpawner.ini` into your GTA V `scripts/` folder:
   ```text
   Grand Theft Auto V/
   └── scripts/
       ├── LemonUI.SHVDN3.dll
       ├── AlienWorldSpawner.dll
       └── AlienWorldSpawner.ini
   ```
4. Launch the game and press **`PgDn`** (or your configured key) to open the menu and start the invasion!

---

## 🔧 Building from Source

### Prerequisites
* Visual Studio 2022 or .NET SDK 8.0+
* Targeting **.NET Framework 4.8**
* Platform: **x64**

### Build Command
```powershell
dotnet build "AlienWorldSpawner.csproj" -c Release /p:Platform=x64
```
*(The project includes a post-build event that automatically copies the compiled DLL directly to your GTA V `scripts` folder).*

---

## 📜 License
This project is open-source and intended for non-commercial modding use with Grand Theft Auto V.

