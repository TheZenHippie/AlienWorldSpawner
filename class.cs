using GTA;
using GTA.Native;
using GTA.Math;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

public class AlienWorldSpawner : Script
{
    // --- LEMONUI CONFIGURATIONS ---
    private readonly ObjectPool _pool = new ObjectPool();
    private readonly NativeMenu _mainMenu = new NativeMenu("Xenomorph Spawner", "Invasion Configuration");

    // Interactive Menu Components
    private readonly NativeCheckboxItem _activationToggle = new NativeCheckboxItem("Start Invasion", "Tick to begin the Xenomorph invasion.", false);
    private readonly NativeSliderItem _scanRadiusSlider = new NativeSliderItem("Scan Radius: 100m", "The detection sweep distance around the player (20m - 200m).", 200, 100);
    private readonly NativeSliderItem _healthSlider = new NativeSliderItem("Alien Health: 2000", "Adjust the health for spawned Xenomorphs (100 to 5000).", 5000, 2000);
    private readonly NativeSliderItem _armorSlider = new NativeSliderItem("Alien Armor: 1000", "Adjust the body armor for spawned Xenomorphs (100 to 3000).", 3000, 1000);
    private readonly NativeCheckboxItem _corpseCleanupToggle = new NativeCheckboxItem("Instant Corpse Removal", "Instantly vaporize dead Xenomorphs to free engine entity pools.", true);
    private readonly NativeCheckboxItem _ignoreSittingPedsToggle = new NativeCheckboxItem("Ignore Sedentary Peds", "When enabled, peds using world scenarios or chairs will not convert.", false);

    // --- ALIEN ASSETS & STATE ---
    private readonly List<string> alienModelNames = new List<string> {
        "Regular_Xenomorph",
        "Tall_Xenomorph",
        "Tallest_Xenomorph"
    };

    private readonly List<Model> loadedAlienModels = new List<Model>();
    private readonly Random rand = new Random();
    private readonly HashSet<int> processedPeds = new HashSet<int>();
    private bool isScriptActive = false;
    private RelationshipGroup alienRelationshipGroup;

    // Track custom peds to safely clean them up later
    private readonly List<Ped> activeAliens = new List<Ped>();

    // Throttling intervals (in milliseconds)
    private const int CleanupInterval = 1000;    // Dead alien corpse cleanup & garbage collection
    private const int ConversionInterval = 350;  // Nearby ped evaluation & Xenomorph conversion

    // Throttle clock tracking variables
    private int _nextCleanupTime = 0;
    private int _nextConversionTime = 0;

    // Configurable menu keybind (Loaded from AlienWorldSpawner.ini, default: PageDown)
    private Keys _menuKey = Keys.PageDown;

    public AlienWorldSpawner()
    {
        alienRelationshipGroup = World.AddRelationshipGroup("XENOMORPHS");

        LoadConfiguration();
        InitializeMenu();

        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += OnAborted;
    }

    private void LoadConfiguration()
    {
        string iniPath = "scripts\\AlienWorldSpawner.ini";
        if (!File.Exists(iniPath))
        {
            iniPath = "AlienWorldSpawner.ini";
        }

        ScriptSettings config = ScriptSettings.Load(iniPath);
        string keyStr = config.GetValue("SETTINGS", "MenuKey", "PageDown");

        if (Enum.TryParse<Keys>(keyStr, true, out Keys parsedKey))
        {
            _menuKey = parsedKey;
        }
        else
        {
            _menuKey = Keys.PageDown;
        }

        // Save default ini if not present
        if (!File.Exists(iniPath))
        {
            config.SetValue("SETTINGS", "MenuKey", _menuKey.ToString());
            config.Save();
        }
    }

    private void InitializeMenu()
    {
        _scanRadiusSlider.Multiplier = 5;
        _healthSlider.Multiplier = 100;
        _armorSlider.Multiplier = 50;

        _mainMenu.Add(_activationToggle);
        _mainMenu.Add(_scanRadiusSlider);
        _mainMenu.Add(_healthSlider);
        _mainMenu.Add(_armorSlider);
        _mainMenu.Add(_corpseCleanupToggle);
        _mainMenu.Add(_ignoreSittingPedsToggle);

        _pool.Add(_mainMenu);

        _activationToggle.CheckboxChanged += OnActivationToggled;

        _scanRadiusSlider.ValueChanged += (s, e) =>
        {
            if (_scanRadiusSlider.Value < 20) _scanRadiusSlider.Value = 20;
            _scanRadiusSlider.Title = $"Scan Radius: {_scanRadiusSlider.Value}m";
        };

        _healthSlider.ValueChanged += (s, e) =>
        {
            if (_healthSlider.Value < 100) _healthSlider.Value = 100;
            _healthSlider.Title = $"Alien Health: {_healthSlider.Value}";
        };

        _armorSlider.ValueChanged += (s, e) =>
        {
            if (_armorSlider.Value < 100) _armorSlider.Value = 100;
            _armorSlider.Title = $"Alien Armor: {_armorSlider.Value}";
        };
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == _menuKey)
        {
            _mainMenu.Visible = !_mainMenu.Visible;
        }
    }

    private void OnActivationToggled(object sender, EventArgs e)
    {
        isScriptActive = _activationToggle.Checked;

        if (isScriptActive)
        {
            _activationToggle.Title = "End Invasion";
            _activationToggle.Description = "Untick to end the invasion and remove all aliens.";

            PreloadAlienModels();

            if (loadedAlienModels.Count == 0)
            {
                Notification.Show("Xenomorph Spawner: ~r~ERROR - No valid Xenomorph models loaded!");
                _activationToggle.Checked = false;
                _activationToggle.Title = "Start Invasion";
                _activationToggle.Description = "Tick to begin the Xenomorph invasion.";
                isScriptActive = false;
                return;
            }

            SetupRelationshipGroups();
            Notification.Show("Xenomorph Spawner: ~r~HOSTILE POPULATION ACTIVE");
        }
        else
        {
            _activationToggle.Title = "Start Invasion";
            _activationToggle.Description = "Tick to begin the Xenomorph invasion.";

            Notification.Show("Xenomorph Spawner: ~g~INACTIVE");
            CleanupAllSpawnedAliens();
        }
    }

    private void SetupRelationshipGroups()
    {
        Ped playerPed = Game.Player.Character;
        if (playerPed != null && playerPed.Exists())
        {
            alienRelationshipGroup.SetRelationshipBetweenGroups(playerPed.RelationshipGroup, Relationship.Hate, true);
        }

        string[] enemyGroupNames = new string[] {
            "PLAYER",
            "COP",
            "CIVMALE",
            "CIVFEMALE",
            "SECURITY_GUARD",
            "MEDIC",
            "FIREMAN",
            "AMBIENT_GANG_LOST",
            "AMBIENT_GANG_BALLAS",
            "AMBIENT_GANG_FAMILY",
            "AMBIENT_GANG_MARABUNTE"
        };

        foreach (string groupName in enemyGroupNames)
        {
            int groupHash = Function.Call<int>(Hash.GET_HASH_KEY, groupName);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Hate, alienRelationshipGroup.Hash, groupHash);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Hate, groupHash, alienRelationshipGroup.Hash);
        }
    }

    private void PreloadAlienModels()
    {
        ReleaseAlienModels();
        foreach (string modelName in alienModelNames)
        {
            Model m = new Model(modelName);
            if (m.IsValid && m.Request(1500))
            {
                loadedAlienModels.Add(m);
            }
            else
            {
                Notification.Show($"Xenomorph Spawner: ~o~Warning: '{modelName}' model not found or invalid.");
            }
        }
    }

    private void ReleaseAlienModels()
    {
        foreach (Model m in loadedAlienModels)
        {
            m.MarkAsNoLongerNeeded();
        }
        loadedAlienModels.Clear();
    }

    private void OnAborted(object sender, EventArgs e)
    {
        _mainMenu.Visible = false;
        _pool.HideAll();
        CleanupAllSpawnedAliens();
    }

    private void CleanupAllSpawnedAliens()
    {
        // 1. Delete all tracked aliens (living or dead)
        for (int i = activeAliens.Count - 1; i >= 0; i--)
        {
            Ped alien = activeAliens[i];
            if (alien != null && alien.Exists())
            {
                alien.Delete();
            }
        }
        activeAliens.Clear();
        processedPeds.Clear();

        // 2. Comprehensive world sweep to guarantee all Xenomorphs (living, ragdolled, or dead) are vaporized
        Ped playerPed = Game.Player.Character;
        if (playerPed != null && playerPed.Exists())
        {
            foreach (Ped p in World.GetNearbyPeds(playerPed.Position, 300.0f))
            {
                if (p != null && p.Exists() && p.RelationshipGroup == alienRelationshipGroup)
                {
                    p.Delete();
                }
            }
        }

        ReleaseAlienModels();
    }

    private void OnTick(object sender, EventArgs e)
    {
        // LemonUI process must run every single frame
        _pool.Process();

        if (!isScriptActive) return;

        Ped playerPed = Game.Player.Character;
        if (playerPed == null || !playerPed.Exists()) return;

        float currentScanRadius = Math.Max(20.0f, (float)_scanRadiusSlider.Value);
        float currentDespawnRadius = Math.Max(150.0f, currentScanRadius + 50.0f);

        // --- TIER 1: THROTTLED CORPSE & DISTANT ENTITY CLEANUP (1000ms) ---
        if (Game.GameTime > _nextCleanupTime)
        {
            _nextCleanupTime = Game.GameTime + CleanupInterval;

            for (int i = activeAliens.Count - 1; i >= 0; i--)
            {
                Ped alien = activeAliens[i];

                if (alien == null || !alien.Exists())
                {
                    activeAliens.RemoveAt(i);
                    continue;
                }

                // If the Xenomorph is dead
                if (alien.IsDead)
                {
                    if (_corpseCleanupToggle.Checked)
                    {
                        alien.Delete();
                    }
                    else
                    {
                        alien.MarkAsNoLongerNeeded();
                    }
                    activeAliens.RemoveAt(i);
                    continue;
                }

                // Despawn aliens that have wandered too far from the player
                if (alien.Position.DistanceTo(playerPed.Position) > currentDespawnRadius)
                {
                    alien.Delete();
                    activeAliens.RemoveAt(i);
                }
            }

            // Keep hashset size controlled
            if (processedPeds.Count > 150)
            {
                processedPeds.Clear();
            }
        }

        // --- TIER 2: THROTTLED POPULATION REPLACEMENT & COMBAT (350ms) ---
        if (Game.GameTime > _nextConversionTime)
        {
            _nextConversionTime = Game.GameTime + ConversionInterval;

            if (loadedAlienModels.Count == 0) return;

            // Maintain combat momentum for active aliens (retarget if idle or previous target died)
            for (int i = 0; i < activeAliens.Count; i++)
            {
                Ped alien = activeAliens[i];
                if (alien != null && alien.Exists() && !alien.IsDead && !alien.IsInCombat)
                {
                    Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, alien.Handle, currentScanRadius, 0);
                }
            }

            Ped[] nearbyPeds = World.GetNearbyPeds(playerPed.Position, currentScanRadius);

            foreach (Ped ped in nearbyPeds)
            {
                // Safety guards: existence, dead check, self, vehicle check, and xenomorph immunity check
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (ped == playerPed || ped.RelationshipGroup == alienRelationshipGroup) continue;
                if (processedPeds.Contains(ped.Handle)) continue;
                if (ped.IsInVehicle()) continue;

                int interiorId = Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, ped.Handle);
                if (interiorId != 0) continue;

                bool isSitting = Function.Call<bool>(Hash.IS_PED_USING_ANY_SCENARIO, ped.Handle) ||
                                 Function.Call<bool>(Hash.IS_PED_ACTIVE_IN_SCENARIO, ped.Handle);

                // If user toggled to ignore sedentary peds, skip conversion so they stay human (aliens will still attack them)
                if (_ignoreSittingPedsToggle.Checked && isSitting) continue;

                if (ped.IsWalking || ped.IsRunning || ped.IsStopped || isSitting)
                {
                    Model alienModel = loadedAlienModels[rand.Next(loadedAlienModels.Count)];

                    Vector3 pos = ped.Position;
                    float heading = ped.Heading;

                    // Safely create alien first before deleting target ped
                    Ped alien = World.CreatePed(alienModel, pos, heading);

                    if (alien != null && alien.Exists())
                    {
                        ped.Delete();

                        int healthVal = Math.Max(100, _healthSlider.Value);
                        int armorVal = Math.Max(100, _armorSlider.Value);

                        alien.MaxHealth = healthVal;
                        alien.Health = healthVal;
                        alien.Armor = armorVal;

                        alien.RelationshipGroup = alienRelationshipGroup;

                        alien.BlockPermanentEvents = true;
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, alien.Handle, 46, true); // BF_AlwaysFight
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, alien.Handle, 5, true);  // BF_CanFightArmedPedsWhenNotArmed
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, alien.Handle, 0, false); // BF_CanUseCover = false
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, alien.Handle, 3, true);  // BF_CanLeaveVehicle
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, alien.Handle, 52, true); // BF_DisableFleeFromCombat
                        Function.Call(Hash.SET_PED_COMBAT_ABILITY, alien.Handle, 2);           // Professional
                        Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, alien.Handle, 3);          // WillRush

                        // Engage all hated targets around ped (civilians in cars, sedentary peds, police, player)
                        Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, alien.Handle, currentScanRadius, 0);

                        // Add to our persistent list tracking system
                        activeAliens.Add(alien);
                        processedPeds.Add(alien.Handle);
                    }
                }
            }
        }
    }
}
