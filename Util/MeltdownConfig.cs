﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using System.Runtime.CompilerServices;
using LethalSettings.UI;
using LethalSettings.UI.Components;
using FacilityMeltdown.Networking;
using Unity.Collections;
using Unity.Netcode;
using RuntimeNetcodeRPCValidator;
using GameNetcodeStuff;
using HarmonyLib;
using System.Runtime.Serialization;

namespace FacilityMeltdown.Util {
    [Serializable]
    internal class MeltdownConfig : SyncedInstance<MeltdownConfig> {
        [NonSerialized]
        private ConfigEntry<int> CFG_MONSTER_SPAWN_AMOUNT, CFG_APPARATUS_VALUE;

        [NonSerialized]
        private ConfigEntry<bool> CFG_OVERRIDE_APPARATUS_VALUE;

        [NonSerialized]
        internal ConfigEntry<float> CFG_MUSIC_VOLUME;
        [NonSerialized]
        internal ConfigEntry<bool> CFG_SCREEN_SHAKE, CFG_MUSIC_PLAYS_OUTSIDE;

        [DataMember]
        private string MOD_VERSION = MeltdownPlugin.modVersion;

        [DataMember]
        internal int MONSTER_SPAWN_AMOUNT, APPARATUS_VALUE;
        [DataMember]
        internal bool OVERRIDE_APPARATUS_VALUE;

        internal MeltdownConfig(ConfigFile file) {
            MeltdownPlugin.logger.LogInfo("Setting up config...");
            InitInstance(this);         

            CFG_OVERRIDE_APPARATUS_VALUE = file.Bind("GameBalance", "OverrideAppartusValue", true, "Whether or not FacilityMeltdown should override appartus value. Only use for compatibility reasons");
            OVERRIDE_APPARATUS_VALUE = CFG_OVERRIDE_APPARATUS_VALUE.Value;
            CFG_APPARATUS_VALUE = file.Bind("GameBalance", "AppartusValue", 240, "What the value of the appartus should be set as IF override appartus value is `true`");
            APPARATUS_VALUE = CFG_APPARATUS_VALUE.Value;
            CFG_MONSTER_SPAWN_AMOUNT = file.Bind("GameBalance", "MonsterSpawnAmount", 5, "How many monsters should spawn during the meltdown sequence? Set to 0 to disable.");
            MONSTER_SPAWN_AMOUNT = CFG_MONSTER_SPAWN_AMOUNT.Value;

            CFG_MUSIC_VOLUME = file.Bind("Audio", "MusicVolume", 100f, "What volume the music plays at. Should be between 0 and 100");
            CFG_MUSIC_PLAYS_OUTSIDE = file.Bind("Audio", "MusicPlaysOutside", true, "Does the music play outside the facility?");
            CFG_SCREEN_SHAKE = file.Bind("Visuals", "ScreenShake", true, "Whether or not to shake the screen during the meltdown sequence.");
        }
        public static void RequestSync() {
            if (!IsClient) return;

            FastBufferWriter stream = new FastBufferWriter(IntSize, Allocator.Temp);
            MessageManager.SendNamedMessage("FacilityMeltdown_OnRequestConfigSync", 0uL, stream);
        }

        public static void OnRequestSync(ulong clientId, FastBufferReader _) {
            if (!IsHost) return;

            MeltdownPlugin.logger.LogInfo($"Config sync request received from client: {clientId}");

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            FastBufferWriter stream = new FastBufferWriter(value + IntSize, Allocator.Temp);

            try {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                MessageManager.SendNamedMessage("FacilityMeltdown_OnReceiveConfigSync", clientId, stream);
            } catch (Exception e) {
                MeltdownPlugin.logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader) {
            if (!reader.TryBeginRead(IntSize)) {
                MeltdownPlugin.logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val)) {
                MeltdownPlugin.logger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            SyncInstance(data);

            if (MeltdownPlugin.modVersion != Instance.MOD_VERSION) {
                HUDManager.Instance.AddTextToChatOnServer("FacilityMeltdown versions do not match! Please make sure all clients are running the latest version. The ability to play together on mismatched versions will be removed in later versions of FacilityMeltdown!");
            } else {
                MeltdownPlugin.logger.LogInfo("Successfully synced config with host.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal void InitLethalConfig() { // i really do not like this syntax :sob:
            MeltdownPlugin.logger.LogInfo("Setting up LethalConfig settings");

            LethalConfigManager.SetModDescription("Maybe taking the appartus isn't such a great idea...");

            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CFG_OVERRIDE_APPARATUS_VALUE, false));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(
                CFG_APPARATUS_VALUE,
                new IntSliderOptions {
                    Min = 80,
                    Max = 500,
                    RequiresRestart = false
                }
            ));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(
                CFG_MONSTER_SPAWN_AMOUNT,
                new IntSliderOptions {
                    Min = 0,
                    Max = 10,
                    RequiresRestart = false
                }
            ));

            LethalConfigManager.AddConfigItem(new FloatStepSliderConfigItem(
                CFG_MUSIC_VOLUME,
                new FloatStepSliderOptions() {
                    Min = 0,
                    Max = 100,
                    Step = 1,
                    RequiresRestart = false
                }
            ));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CFG_MUSIC_PLAYS_OUTSIDE, false));

            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(CFG_SCREEN_SHAKE, false));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal void InitLethalSettings() {
            SliderComponent appratusValueSlider = new SliderComponent {
                Value = CFG_APPARATUS_VALUE.Value,
                MinValue = 80,
                MaxValue = 500,
                WholeNumbers = true,
                Text = "Appartus Value",
                Enabled = CFG_OVERRIDE_APPARATUS_VALUE.Value,
                OnValueChanged = (self, value) => { CFG_APPARATUS_VALUE.Value = (int)value; Default.APPARATUS_VALUE = (int)value; }
            };

            ModMenu.RegisterMod(new ModMenu.ModSettingsConfig {
                Name = MeltdownPlugin.modName,
                Id = MeltdownPlugin.modGUID,
                Version = MeltdownPlugin.modVersion,
                Description = "Maybe taking the appartus isn't such a great idea...",
                MenuComponents = new MenuComponent[] {
                    new LabelComponent {
                        Text = "Game Balance Settings"
                    },
                    new ToggleComponent {
                        Text = "Override Appartus Value?",
                        Value = CFG_OVERRIDE_APPARATUS_VALUE.Value,
                        OnValueChanged = (self, value) => {
                            CFG_OVERRIDE_APPARATUS_VALUE.Value = value;
                            OVERRIDE_APPARATUS_VALUE = value;
                            appratusValueSlider.Enabled = value;
                        }
                    },
                    appratusValueSlider,
                    new SliderComponent {
                        Value = CFG_MONSTER_SPAWN_AMOUNT.Value,
                        MinValue = 0,
                        MaxValue = 10,
                        WholeNumbers = true,
                        Text = "Monster Spawn Amount",
                        OnValueChanged = (self, value) => { CFG_MONSTER_SPAWN_AMOUNT.Value = (int)value; Default.MONSTER_SPAWN_AMOUNT = (int)value; }
                    },
                    new LabelComponent {
                        Text = "Audio Settings"
                    },
                    new SliderComponent {
                        Value = CFG_MUSIC_VOLUME.Value,
                        MinValue = 0,
                        MaxValue = 100,
                        WholeNumbers = true,
                        Text = "Music Volume",
                        OnValueChanged = (self, value) => CFG_MUSIC_VOLUME.Value = (int) value
                    },
                    new ToggleComponent {
                        Text = "Play Music Outside?",
                        Value = CFG_MUSIC_PLAYS_OUTSIDE.Value,
                        OnValueChanged = (self, value) => CFG_MUSIC_PLAYS_OUTSIDE.Value = value
                    },
                    new LabelComponent {
                        Text = "Visual Settings"
                    },
                    new ToggleComponent {
                        Text = "Screen Shake",
                        Value = CFG_SCREEN_SHAKE.Value,
                        OnValueChanged = (self, value) => CFG_SCREEN_SHAKE.Value = value
                    },
                }
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        public static void InitializeLocalPlayer() {
            if (IsHost) {
                MessageManager.RegisterNamedMessageHandler("FacilityMeltdown_OnRequestConfigSync", OnRequestSync);
                Synced = true;

                return;
            }

            Synced = false;
            MessageManager.RegisterNamedMessageHandler("FacilityMeltdown_OnReceiveConfigSync", OnReceiveSync);
            RequestSync();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave() {
            RevertSync();
        }
    }
}
