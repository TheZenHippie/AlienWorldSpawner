## Reference Examples

### Correct Baseline Structure
using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

public class AntigravityAdvancedScript : Script
{
    // LemonUI fields
    private readonly ObjectPool _pool = new ObjectPool();
    private readonly NativeMenu _mainMenu = new NativeMenu("Antigravity Mod", "SHVDN3 Best Practices");
    private readonly NativeCheckboxItem _godModeCheckbox = new NativeCheckboxItem("Invincibility", "Toggle script-managed health protection.");

    // Native event tracking fields
    private int _nextEventCheckTime = 0;
    private bool _wasPlayerInVehicleLastCheck = false;

    public AntigravityAdvancedScript()
    {
        // 1. Build and configure the UI
        _mainMenu.Add(_godModeCheckbox);
        _pool.Add(_mainMenu);

        // Bind UI events
        _godModeCheckbox.CheckboxChanged += OnGodModeToggle;

        // 2. Bind script lifecycle events
        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += OnAborted;
    }

    private void OnTick(object sender, EventArgs e)
    {
        // Always process LemonUI pools every frame
        _pool.Process();

        Ped playerPed = Game.Player.Character;
        if (playerPed == null || !playerPed.Exists()) return;

        // Apply script state securely
        if (_godModeCheckbox.Checked)
        {
            playerPed.IsInvincible = true;
        }

        // Throttled Native Event Check (Runs every 500ms instead of every frame)
        if (Game.GameTime > _nextEventCheckTime)
        {
            HandleCustomGameEvents(playerPed);
            _nextEventCheckTime = Game.GameTime + 500;
        }
    }

    private void HandleCustomGameEvents(Ped playerPed)
    {
        bool isCurrentlyInVehicle = playerPed.IsInVehicle();

        // Custom Event Logic: Player just entered a vehicle
        if (isCurrentlyInVehicle && !_wasPlayerInVehicleLastCheck)
        {
            Vehicle currentVehicle = playerPed.CurrentVehicle;
            if (currentVehicle != null && currentVehicle.Exists())
            {
                GTA.UI.Notification.Show($"~g~Event:~w~ Entered vehicle: {currentVehicle.LocalizedName}");
            }
        }

        _wasPlayerInVehicleLastCheck = isCurrentlyInVehicle;
    }

    private void OnGodModeToggle(object sender, EventArgs e)
    {
        if (!_godModeCheckbox.Checked)
        {
            // Clean up state immediately when untoggled
            Ped playerPed = Game.Player.Character;
            if (playerPed != null && playerPed.Exists())
            {
                playerPed.IsInvincible = false;
            }
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Explicitly map a menu toggle key safely
        if (e.KeyCode == Keys.F10)
        {
            _mainMenu.Visible = !_mainMenu.Visible;
        }
    }

    private void OnAborted(object sender, EventArgs e)
    {
        // Clean up UI dependencies immediately on script reload or crash
        _mainMenu.Visible = false;
        _pool.Clear();

        Ped playerPed = Game.Player.Character;
        if (playerPed != null && playerPed.Exists())
        {
            playerPed.IsInvincible = false;
        }
    }
}
