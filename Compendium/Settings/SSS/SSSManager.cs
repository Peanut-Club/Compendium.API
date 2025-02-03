using Compendium.Events;
using PluginAPI.Core;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace Compendium.API.Compendium.Settings.SSS {
    public static class SSSManager {
        public static Dictionary<int, List<Action<ReferenceHub, SSKeybindSetting>>> IOkeys = new();

        [helpers.Attributes.Load]
        public static void Init() {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSSInput;
        }

        [Event]
        public static void SendToNewPlayer(PlayerJoinedEvent ev) {
            ServerSpecificSettingsSync.SendToPlayer(ev.Player.ReferenceHub);
        }

        // new SSKeybindSetting(null, $"AMERT - Interactable Object - {(KeyCode)Base.InputKeyCode}", (KeyCode)Base.InputKeyCode, true, "");
        public static void AddNewKeybind(SSKeybindSetting settings, Action<ReferenceHub, SSKeybindSetting> action) {
            int key = (int)settings.SuggestedKey;
            if (IOkeys.ContainsKey(key)) {
                IOkeys[key].Add(action);
            } else {
                IOkeys.Add(key, new List<Action<ReferenceHub, SSKeybindSetting>> { action });
            }

            if (ServerSpecificSettingsSync.DefinedSettings == null) {
                ServerSpecificSettingsSync.DefinedSettings = Array.Empty<ServerSpecificSettingBase>();
            }

            ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings.Append(settings).OrderBy(e => e.Label).ToArray();
            ServerSpecificSettingsSync.SendToAll();
            //Plugin.Info($"Registered {settings.Label} to key {settings.SuggestedKey}");
        }


        public static void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting) {
            SSKeybindSetting originalKeybind = setting.OriginalDefinition as SSKeybindSetting;
            SSKeybindSetting thisKeybind = setting as SSKeybindSetting;
            //Plugin.Info($"on input {setting.OriginalDefinition.GetType()}");
            if (originalKeybind == null || thisKeybind == null || !thisKeybind.SyncIsPressed) {
                return;
            }


            int key = (int)originalKeybind.SuggestedKey;
            //Plugin.Info($"key pressed: {originalKeybind.SuggestedKey}");
            if (IOkeys.TryGetValue(key, out var handlers)) {
                handlers.ForEach(value => value.Invoke(sender, originalKeybind));
            }
        }

    }
}
