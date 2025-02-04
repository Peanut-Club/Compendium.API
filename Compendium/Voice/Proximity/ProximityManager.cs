using Compendium.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UserSettings.GUIElements.UserSettingDependency.Dependency;
using System.Xml.Linq;
using UnityEngine;
using Mirror;

namespace Compendium.API.Compendium.Voice.Proximity {
    public static class ProximityManager {
        //public static Dictionary<string, ProximitySpeaker> PrSpeakerByName = new Dictionary<string, ProximitySpeaker>();
        public static Dictionary<byte, ProximitySpeaker> PrSpeakerById = new Dictionary<byte, ProximitySpeaker>();

        public static string GetSpeakerName(ReferenceHub owner) {
             return owner.UserId() + "-proximity";
        }

        public static ProximitySpeaker CreateProximitySpeaker(ReferenceHub owner) {
            var name = GetSpeakerName(owner);

            /*
            if (PrSpeakerByName.ContainsKey(name)) {
                ServerConsole.AddLog($"[ProximityManager] Speaker with name {name} already exists!");
                return null;
            }*/

            byte targetId = 255;

            if (targetId == 255) {
                for (byte x = 0; x < byte.MaxValue; x++) {
                    if (PrSpeakerById.ContainsKey(x))
                        continue;

                    targetId = x;
                    break;
                }
            }

            var speaker = ProximitySpeaker.Create(targetId, owner, Vector3.zero, 1f, true, 2f, 18f);
            speaker.Name = name;

            PrSpeakerById.Add(targetId, speaker);
            //PrSpeakerByName.Add(name, speaker);

            return speaker;
        }

        public static void DeleteSpeaker(ProximitySpeaker speaker) {
            if (speaker == null) return;
            NetworkServer.Destroy(speaker.gameObject);
        }
    }
}
