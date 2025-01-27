using System;
using System.Collections.Generic;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Extensions;
using helpers;
using helpers.Patching;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Enums;
using UnityEngine;

namespace Compendium.Prefabs;

public static class PrefabHelper
{
	private static readonly Dictionary<PrefabName, DoorVariant> m_Doors = new Dictionary<PrefabName, DoorVariant>();

	private static readonly Dictionary<PrefabName, string> m_Names = new Dictionary<PrefabName, string>
	{
		[PrefabName.Player] = "Player",
		[PrefabName.AntiScp207] = "AntiSCP207Pickup",
		[PrefabName.Adrenaline] = "AdrenalinePrefab",
		[PrefabName.Ak] = "AkPickup",
		[PrefabName.A7] = "A7Pickup",
		[PrefabName.Ammo12ga] = "Ammo12gaPickup",
		[PrefabName.Ammo44cal] = "Ammo44calPickup",
		[PrefabName.Ammo556mm] = "Ammo556mmPickup",
		[PrefabName.Ammo762mm] = "Ammo762mmPickup",
		[PrefabName.Ammo9mm] = "Ammo9mmPickup",
		[PrefabName.ChaosKeycard] = "ChaosKeycardPickup",
		[PrefabName.Coin] = "CoinPickup",
		[PrefabName.Com15] = "Com15Pickup",
		[PrefabName.Com18] = "Com18Pickup",
		[PrefabName.Com45] = "Com45Pickup",
		[PrefabName.CombatArmor] = "Combat Armor Pickup",
		[PrefabName.Crossvec] = "CrossvecPickup",
		[PrefabName.Disruptor] = "DisruptorPickup",
		[PrefabName.Epsilon11SR] = "E11SRPickup",
		[PrefabName.FlashbangPickup] = "FlashbangPickup",
		[PrefabName.FlashbangProjectile] = "FlashbangProjectile",
		[PrefabName.Flashlight] = "FlashlightPickup",
		[PrefabName.Fsp9] = "Fsp9Pickup",
		[PrefabName.FrMg0] = "FRMG0Pickup",
		[PrefabName.HeavyArmor] = "Heavy Armor Pickup",
		[PrefabName.HegPickup] = "HegPickup",
		[PrefabName.HegProjectile] = "HegProjectile",
		[PrefabName.Jailbird] = "JailbirdPickup",
		[PrefabName.LightArmor] = "Light Armor Pickup",
		[PrefabName.Logicer] = "LogicerPickup",
		[PrefabName.Medkit] = "MedkitPickup",
		[PrefabName.MicroHid] = "MicroHidPickup",
		[PrefabName.Painkillers] = "PainkillersPickup",
		[PrefabName.Radio] = "RadioPickup",
		[PrefabName.RegularKeycard] = "RegularKeycardPickup",
		[PrefabName.Revolver] = "RevolverPickup",
		[PrefabName.Scp1576] = "SCP1576Pickup",
		[PrefabName.Scp1853] = "SCP1853Pickup",
		[PrefabName.Scp207] = "SCP207Pickup",
		[PrefabName.Scp244a] = "SCP244APickup Variant",
		[PrefabName.Scp244b] = "SCP244BPickup Variant",
		[PrefabName.Scp268] = "SCP268Pickup",
		[PrefabName.Scp500] = "SCP500Pickup",
		[PrefabName.Scp018] = "Scp018Projectile",
		[PrefabName.Scp2176] = "Scp2176Projectile",
		[PrefabName.Scp330] = "Scp330Pickup",
		[PrefabName.Shotgun] = "ShotgunPickup",
		[PrefabName.HealthBox] = "AdrenalineMedkitStructure",
		[PrefabName.Generator] = "GeneratorStructure",
		[PrefabName.LargeGunLocker] = "LargeGunLockerStructure",
		[PrefabName.MiscLocker] = "MiscLocker",
		[PrefabName.MedkitBox] = "RegularMedkitStructure",
		[PrefabName.RifleRack] = "RifleRackStructure",
		[PrefabName.Scp018Pedestal] = "Scp018PedestalStructure Variant",
		[PrefabName.Scp1853Pedestal] = "Scp1853PedestalStructure Variant",
		[PrefabName.Scp207Pedestal] = "Scp207PedestalStructure Variant",
		[PrefabName.Scp2176Pedestal] = "Scp2176PedestalStructure Variant",
		[PrefabName.Scp244Pedestal] = "Scp244PedestalStructure Variant",
		[PrefabName.Scp268Pedestal] = "Scp268PedestalStructure Variant",
		[PrefabName.Scp500Pedestal] = "Scp500PedestalStructure Variant",
		[PrefabName.Scp1576Pedestal] = "Scp1576PedestalStructure Variant",
		[PrefabName.AmnesticCloud] = "Amnestic Cloud Hazard",
		[PrefabName.WorkStation] = "Spawnable Work Station Structure",
		[PrefabName.Tantrum] = "TantrumObj",
		[PrefabName.SportTarget] = "sportTargetPrefab",
		[PrefabName.ClassDTarget] = "dboyTargetPrefab",
		[PrefabName.BinaryTarget] = "binaryTargetPrefab",
		[PrefabName.PrimitiveObject] = "PrimitiveObjectToy",
		[PrefabName.LightSource] = "LightSourceToy",
		[PrefabName.Lantern] = "LanternPickup",
		[PrefabName.Scp3114Ragdoll] = "Scp3114_Ragdoll",
		[PrefabName.Ragdoll1] = "Ragdoll_1",
		[PrefabName.Ragdoll4] = "Ragdoll_4",
		[PrefabName.Ragdoll6] = "Ragdoll_6",
		[PrefabName.Ragdoll7] = "Ragdoll_7",
		[PrefabName.Ragdoll8] = "Ragdoll_8",
		[PrefabName.Ragdoll10] = "Ragdoll_10",
		[PrefabName.Ragdoll12] = "Ragdoll_12",
		[PrefabName.Scp096Ragdoll] = "SCP-096_Ragdoll",
		[PrefabName.Scp106Ragdoll] = "SCP-106_Ragdoll",
		[PrefabName.Scp173Ragdoll] = "SCP-173_Ragdoll",
		[PrefabName.Scp939Ragdoll] = "SCP-939_Ragdoll",
		[PrefabName.TutorialRagdoll] = "Ragdoll_Tut"
	};

	private static readonly Dictionary<PrefabName, GameObject> m_Prefabs = new Dictionary<PrefabName, GameObject>();

	public static bool DoorPrefabsLoaded { get; private set; }

    /* disabled
	public static bool TryReplaceDoor(DoorVariant targetDoor, PrefabName replacementType, bool replaceClose, out DoorVariant replacement)
	{
		if (replacementType != PrefabName.EntranceZoneDoor && replacementType != PrefabName.HeavyContainmentZoneDoor && replacementType != PrefabName.LightContainmentZoneDoor)
		{
			Plugin.Warn($"Called TryReplaceDoor with a prefab that's not a door! ({replacementType})");
			replacement = null;
			return false;
		}
		if (!TryGetDoorPrefab(replacementType, out var _))
		{
			Plugin.Warn($"Failed to get prefab for door type '{replacementType}'");
			replacement = null;
			return false;
		}
		try
		{
			Vector3 position = targetDoor.transform.position;
			Quaternion rotation = targetDoor.transform.rotation;
			Vector3 localScale = targetDoor.transform.localScale;
			DoorNametagExtension result;
			string name = (targetDoor.gameObject.TryGet<DoorNametagExtension>(out result) ? result.GetName : string.Empty);
			NetworkServer.Destroy(targetDoor.gameObject);
			return TrySpawnDoor(replacementType, position, localScale, rotation, name, shouldSpawn: true, out replacement);
		}
		catch (Exception message)
		{
			Plugin.Error(message);
		}
		replacement = null;
		return false;
	}

	public static bool TrySpawnDoor(PrefabName doorType, Vector3 position, Vector3 scale, Quaternion rotation, string name, bool shouldSpawn, out DoorVariant door)
	{
		if (doorType != PrefabName.EntranceZoneDoor && doorType != PrefabName.HeavyContainmentZoneDoor && doorType != PrefabName.LightContainmentZoneDoor)
		{
			Plugin.Warn($"Called TrySpawnDoor with a prefab that's not a door! ({doorType})");
			door = null;
			return false;
		}
		if (!TryGetDoorPrefab(doorType, out var prefab))
		{
			Plugin.Warn($"Failed to get prefab for door type '{doorType}'");
			door = null;
			return false;
		}
		try
		{
			DoorVariant doorInstance = UnityEngine.Object.Instantiate(prefab, position, rotation);
			RoomIdentifier roomIdentifier = RoomIdUtils.RoomAtPosition(position);
			if ((object)doorInstance == null)
			{
				Plugin.Warn($"Failed to instantiate door prefab '{doorType}'!");
				door = null;
				return false;
			}
			if (roomIdentifier != null && roomIdentifier.ApiRoom != null)
			{
				Facility.RegisterDoor(roomIdentifier.ApiRoom, doorInstance);
			}
			if (!string.IsNullOrWhiteSpace(name))
			{
				doorInstance.GetOrAddComponent<DoorNametagExtension>().UpdateName(name);
			}
			if (shouldSpawn)
			{
				if (scale != Vector3.zero)
				{
					doorInstance.transform.localScale = scale;
				}
				NetworkServer.Spawn(doorInstance.gameObject);
				Calls.NextFrame(delegate
				{
					doorInstance.transform.rotation = rotation;
					doorInstance.transform.position = position;
				});
			}
			door = doorInstance;
			return true;
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed to spawn door:\n{arg}");
		}
		door = null;
		return false;
	}*/

    public static bool TryInstantiatePrefab<TComponent>(PrefabName name, out TComponent component) where TComponent : Component
	{
		if (!TryInstantiatePrefab(name, out var instance))
		{
			component = null;
			return false;
		}
		return instance.TryGet<TComponent>(out component);
	}

	public static bool TryInstantiatePrefab(PrefabName name, out GameObject instance)
	{
		if (!TryGetPrefab(name, out var prefab))
		{
			instance = null;
			return false;
		}
		instance = UnityEngine.Object.Instantiate(prefab);
		return instance != null;
	}

	public static bool TryGetPrefab(PrefabName name, out GameObject prefab)
	{
		if (m_Prefabs.Count == 0)
		{
			LoadPrefabs();
			//LoadDoorPrefabs();
		}
		return m_Prefabs.TryGetValue(name, out prefab);
	}
	/* disabled
	public static bool TryGetDoorPrefab(PrefabName doorType, out DoorVariant prefab)
	{
		if (m_Doors.Count == 0 || !DoorPrefabsLoaded)
		{
			LoadDoorPrefabs();
		}
		return m_Doors.TryGetValue(doorType, out prefab);
	}*/

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	public static void ClearPrefabs()
	{
		m_Prefabs.Clear();
		m_Doors.Clear();
		DoorPrefabsLoaded = false;
	}

	[Event(ServerEventType.MapGenerated)]
	public static void ReloadPrefabs()
	{
		LoadPrefabs();
	}
    /* disabled
	private static void LoadDoorPrefabs()
	{
		m_Doors.Clear();
		DoorPrefabsLoaded = false;
		foreach (DoorSpawnpoint allInstance in DoorSpawnpoint.AllInstances)
		{
			try
			{
				if ((object)allInstance != null && (object)allInstance.TargetPrefab != null)
				{
					string name = allInstance.TargetPrefab.name;
					if (1 == 0)
					{
					}
					PrefabName prefabName = name switch
					{
						"LCZ BreakableDoor" => PrefabName.LightContainmentZoneDoor, 
						"HCZ BreakableDoor" => PrefabName.HeavyContainmentZoneDoor, 
						"EZ BreakableDoor" => PrefabName.EntranceZoneDoor, 
						_ => throw new ArgumentOutOfRangeException("name"), 
					};
					if (1 == 0)
					{
					}
					PrefabName prefabName2 = prefabName;
					if (!m_Doors.ContainsKey(prefabName2))
					{
						m_Doors[prefabName2] = allInstance.TargetPrefab;
						Plugin.Info($"Loaded door prefab '{prefabName2}': {allInstance.TargetPrefab.name}");
					}
				}
			}
			catch (Exception message)
			{
				Plugin.Error(message);
			}
		}
		DoorPrefabsLoaded = true;
	}*/

    private static void LoadPrefabs()
	{
		m_Prefabs.Clear();
		m_Prefabs[PrefabName.Player] = NetworkManager.singleton.playerPrefab;
		foreach (GameObject value in NetworkClient.prefabs.Values)
		{
			if (string.IsNullOrEmpty(value?.name) || value.name == "EZ BreakableDoor" || value.name == "HCZ BreakableDoor" || value.name == "LCZ BreakableDoor")
			{
				continue;
			}
			if (m_Names.TryGetKey(value.name, out var key))
			{
				if (!m_Prefabs.ContainsKey(key))
				{
					m_Prefabs[key] = value;
				}
			}
			else
			{
				Plugin.Warn("Failed to retrieve prefab name: " + value.name);
			}
		}
		foreach (KeyValuePair<PrefabName, string> name in m_Names)
		{
			if (name.Key != PrefabName.EntranceZoneDoor && name.Key != PrefabName.HeavyContainmentZoneDoor && name.Key != PrefabName.LightContainmentZoneDoor && !m_Prefabs.ContainsKey(name.Key))
			{
				Plugin.Warn($"Prefab '{name.Key}' ({name.Value}) is missing or has been renamed!");
			}
		}
		Plugin.Info($"Loaded {m_Prefabs.Count} / {m_Names.Count} prefabs.");
	}

    /* disabled
	[Patch(typeof(DoorSpawnpoint), "SetupAllDoors", PatchType.Prefix, new Type[] { })]
	private static bool OnSetupSpawnpoints()
	{
		LoadDoorPrefabs();
		return true;
	}*/
}
