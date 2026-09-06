---
name: lemonui
description: Advanced performance rules and allocation patterns for LemonUI menu setups.
activation_keywords: ["LemonUI", "NativeMenu", "ObjectPool", "NativeCheckboxItem", "Menu UI"]
languages: ["csharp"]
dependencies: ["shvdn3-core"]
---

# LemonUI Best Practices Skill

## Description
Advanced architecture patterns, performance guardrails, and structural blueprints for designing menus, banners, items, and canvas overlays using the LemonUI framework in SHVDN3.

## Activation Trigger Keywords
- "LemonUI" / "NativeMenu"
- "Create a menu" / "Add a submenu"
- "ObjectPool"
- "NativeCheckboxItem" / "NativeListItem" / "NativeItem"

## Core UI Implementation Rules

### 1. Initialization & The Allocation Rule
- **Strict Anti-Pattern:** Never instantiate `NativeMenu`, `NativeItem`, or `ObjectPool` inside the `OnTick` thread loop. This causes immense memory pressure and immediate frame drops.
- **Enforced Setup:** Allocate the `ObjectPool`, parent menus, submenus, and all individual interactive items *exactly once* inside the script's constructor or a dedicated `InitializeUI()` startup method.

### 2. Thread Processing & Visibility Control
- **Continuous Frame Process:** Invoke `_pool.Process();` every single frame at the very beginning of the `OnTick` loop. 
- **Visibility Optimization:** Do not gate `_pool.Process();` behind an `if (menu.Visible)` check. The `ObjectPool` natively handles structural visibility efficiently and needs to process item change states even while fading out.
- **Input Interception:** Use `_pool.HideAll()` during high-priority gameplay events (e.g., player death or cinematic cuts) to clear screen clutter instantly.

### 3. Submenu Hierarchy & Back-Buttons
- **Parent-Child Linkage:** Always use the `.AddSubMenu()` method on the parent menu to attach a child menu. 
- **Navigation Safety:** Do not manually intercept the `Backspace` or controller cancel buttons to go backward. LemonUI automatically configures back-navigation and binding indexes when submenus are added via the proper API method.

### 4. UI Memory Safety & Garbage Disposal
- **Script Abortion Cleanup:** When the script is reloaded (`Aborted` event), you must clear the menu system explicitly.
- **Teardown Sequence:** Force all menu visibilities to `false`, call `_pool.Clear()`, and nullify individual collection properties to prevent hanging engine hooks.

---

## Technical Reference Snippet

### Correct LemonUI Architecture Blueprint
```csharp
using System;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;

public class AntigravityUiSkillExample : Script
{
    // 1. Thread-safe global allocations
    private readonly ObjectPool _pool = new ObjectPool();
    private readonly NativeMenu _mainMenu = new NativeMenu("Antigravity Mod", "Main System Menu");
    private readonly NativeMenu _subMenu = new NativeMenu("Sub Settings", "Advanced Submenu Configurations");

    private readonly NativeItem _triggerActionButton = new NativeItem("Execute Function", "Triggers a native engine script routine.");
    private readonly NativeCheckboxItem _toggleSetting = new NativeCheckboxItem("Enable Feature", "Global switch for active routines.");

    public AntigravityUiSkillExample()
    {
        // 2. Build layout during constructor cycle
        BuildMenuLayout();

        // 3. Attach game runtime event hooks
        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += OnAborted;
    }

    private void BuildMenuLayout()
    {
        // Add items to main menu
        _mainMenu.Add(_triggerActionButton);
        _mainMenu.Add(_toggleSetting);

        // Bind Submenu Hierarchy correctly
        _mainMenu.AddSubMenu(_subMenu);

        // Register menus into the central management pool
        _pool.Add(_mainMenu);
        _pool.Add(_subMenu);

        // Bind item callbacks safely
        _triggerActionButton.Activated += OnActionButtonActivated;
        _toggleSetting.CheckboxChanged += OnSettingToggled;
    }

    private void OnTick(object sender, EventArgs e)
    {
        // Process menu frames natively 
        _pool.Process();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Safe global toggle boundary (Using F10)
        if (e.KeyCode == Keys.F10)
        {
            _mainMenu.Visible = !_mainMenu.Visible;
        }
    }

    private void OnActionButtonActivated(object sender, EventArgs e)
    {
        Ped player = Game.Player.Character;
        if (player == null || !player.Exists()) return;

        GTA.UI.Notification.Show("Action triggered from LemonUI Skill schema!");
    }

    private void OnSettingToggled(object sender, EventArgs e)
    {
        GTA.UI.Notification.Show(\$"Feature Status: {(_toggleSetting.Checked ? "~g~Active" : "~r~Inactive")}");
    }

    private void OnAborted(object sender, EventArgs e)
    {
        // 4. Critical cleanup sequence on script drop/crash
        _mainMenu.Visible = false;
        _subMenu.Visible = false;
        _pool.Clear();
    }
}