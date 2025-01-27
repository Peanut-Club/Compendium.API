using System.Collections.Generic;
using System.Linq;
using Compendium.Extensions;
using GameCore;
using helpers;
using helpers.Extensions;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.Ragdolls;
using UnityEngine;
using Utils.Networking;

namespace Compendium;

public static class World
{
	public static Vector3 EscapePosition => Escape.WorldPos;

	public static IEnumerable<ItemPickupBase> Pickups => Object.FindObjectsOfType<ItemPickupBase>();

	public static IEnumerable<ItemBase> Items => Hub.Hubs.SelectMany((ReferenceHub hub) => hub.GetItems());

	public static IEnumerable<BasicRagdoll> Ragdolls => Object.FindObjectsOfType<BasicRagdoll>();

	public static IEnumerable<DoorVariant> Doors => DoorVariant.AllDoors;

	public static IEnumerable<DoorVariant> Gates => Doors.Where((DoorVariant d) => d.IsGate());

	public static IEnumerable<ElevatorChamber> Elevators => Object.FindObjectsOfType<ElevatorChamber>();

	public static IEnumerable<Scp079Camera> Cameras => Scp079InteractableBase.AllInstances.Where<Scp079Camera>();

	public static IEnumerable<Scp079Generator> Generators => Object.FindObjectsOfType<Scp079Generator>();

	public static string ServerName => ServerConsole._serverName;

	public static string ClearServerName => ServerConsole._serverName.RemoveHtmlTags().FilterWhiteSpaces();

	public static string AlternativeServerName => Plugin.Config.ApiSetttings.AlternativeServerName;

	public static string CurrentOrAlternativeServerName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(AlternativeServerName) && !(AlternativeServerName == "none"))
			{
				return AlternativeServerName;
			}
			return ServerName;
		}
	}

	public static string CurrentClearOrAlternativeServerName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(AlternativeServerName) && !(AlternativeServerName == "none"))
			{
				return AlternativeServerName;
			}
			return ClearServerName;
		}
	}

	public static string ServerIp => ConfigFile.ServerConfig.GetString("server_ip", "auto");

	public static int ServerPort => ServerStatic.ServerPort;

	public static float TicksPerSecondFull => 1f / Time.smoothDeltaTime;

	public static float FrametimeFull => 1f / Time.deltaTime;

	public static int Ticks => Mathf.RoundToInt(TicksPerSecondFull);

	public static int Frametime => Mathf.RoundToInt(FrametimeFull);

	public static bool CanEscape(ReferenceHub hub, bool useGameLogic = true)
	{
		if (hub.Position().IsWithinDistance(EscapePosition, 156.5f))
		{
			if (!useGameLogic)
			{
				return true;
			}
			return Escape.ServerGetScenario(hub) != Escape.EscapeScenarioType.None;
		}
		return false;
	}

	public static void Broadcast(object message, int duration, bool clear = true)
	{
		Hub.ForEach(delegate(ReferenceHub hub)
		{
			hub.Broadcast(message, duration, clear);
		});
	}

	public static void Hint(object message, float duration)
	{
		Hub.ForEach(delegate(ReferenceHub hub)
		{
			hub.Hint(message, duration);
		});
	}

	public static void ClearPickups()
	{
		Pickups.ForEach(delegate(ItemPickupBase pickup)
		{
			pickup.DestroySelf();
		});
	}

	public static void ClearPickups(ItemType type)
	{
		Pickups.Where((ItemPickupBase p) => p.Info.ItemId == type).ForEach(delegate(ItemPickupBase pickup)
		{
			pickup.DestroySelf();
		});
	}

	public static void ClearItems()
	{
		Items.ForEach(delegate(ItemBase item)
		{
			item.OwnerInventory?.ServerRemoveItem(item.ItemSerial, item.PickupDropModel);
		});
	}

	public static void ClearItems(ItemType item)
	{
		Items.Where((ItemBase i) => i.ItemTypeId == item).ForEach(delegate(ItemBase it)
		{
			it.OwnerInventory?.ServerRemoveItem(it.ItemSerial, it.PickupDropModel);
		});
	}

	public static void ClearRagdolls()
	{
		Ragdolls.ForEach(delegate(BasicRagdoll rag)
		{
			NetworkServer.Destroy(rag.gameObject);
		});
	}

	public static void Clear()
	{
		ClearPickups();
		ClearRagdolls();
	}

	public static List<TItem> SpawnItems<TItem>(ItemType item, Vector3 position, Vector3 scale, Quaternion rotation, int amount, bool spawn = true) where TItem : ItemPickupBase
	{
		List<TItem> list = new List<TItem>();
		for (int i = 0; i < amount; i++)
		{
			TItem val = SpawnItem<TItem>(item, position, scale, rotation, spawn);
			if ((Object)val != (Object)null)
			{
				list.Add(val);
			}
		}
		return list;
	}

	public static TItem SpawnItem<TItem>(ItemType item, Vector3 position, Vector3 scale, Quaternion rotation, bool spawn = true) where TItem : ItemPickupBase
	{
		if (!InventoryItemLoader.TryGetItem<ItemBase>(item, out var result) || (object)result == null || (object)result.PickupDropModel == null)
		{
			return null;
		}
		TItem val = Object.Instantiate((TItem)result.PickupDropModel, position, rotation);
		val.transform.position = position;
		val.transform.rotation = rotation;
		val.transform.localScale = scale;
		val.NetworkInfo = new PickupSyncInfo(item, result.Weight, ItemSerialGenerator.GenerateNext());
		if (spawn)
		{
			NetworkServer.Spawn(val.gameObject);
		}
		return val;
	}

	public static TProjectile SpawnNonActiveProjectile<TProjectile>(ItemType item, Vector3 position, Vector3 scale, Vector3 forward, Vector3 up, Quaternion rotation, Vector3 velocity, float force, float fuseTime = 2f) where TProjectile : ThrownProjectile
	{
		if (!InventoryItemLoader.TryGetItem<ThrowableItem>(item, out var result))
		{
			return null;
		}
		TProjectile val = Object.Instantiate((TProjectile)result.Projectile, position, rotation);
		ThrowableItem.ProjectileSettings fullThrowSettings = result.FullThrowSettings;
		val.transform.localScale = scale;
		val.NetworkInfo = new PickupSyncInfo(item, result.Weight, ItemSerialGenerator.GenerateNext());
		fullThrowSettings.StartVelocity = force;
		fullThrowSettings.StartTorque = velocity;
		NetworkServer.Spawn(val.gameObject);
		return val;
	}

	public static TProjectile SpawnProjectile<TProjectile>(ItemType item, Vector3 position, Vector3 scale, Vector3 forward, Vector3 up, Quaternion rotation, Vector3 velocity, float force, float fuseTime = 2f) where TProjectile : ThrownProjectile
	{
		if (!InventoryItemLoader.TryGetItem<ThrowableItem>(item, out var result))
		{
			return null;
		}
		TProjectile val = Object.Instantiate((TProjectile)result.Projectile, position, rotation);
		ThrowableItem.ProjectileSettings fullThrowSettings = result.FullThrowSettings;
		val.transform.localScale = scale;
		val.NetworkInfo = new PickupSyncInfo(item, result.Weight, ItemSerialGenerator.GenerateNext());
		fullThrowSettings.StartVelocity = force;
		fullThrowSettings.StartTorque = velocity;
		if (val is TimeGrenade timeGrenade)
		{
			timeGrenade._fuseTime = fuseTime;
		}
		NetworkServer.Spawn(val.gameObject);
		if (val.TryGetComponent<Rigidbody>(out var component))
		{
			float num = 1f - Mathf.Abs(Vector3.Dot(forward, Vector3.up));
			Vector3 vector = up * result.FullThrowSettings.UpwardsFactor;
			Vector3 vector2 = forward + vector * num;
			component.centerOfMass = Vector3.zero;
			component.angularVelocity = fullThrowSettings.StartTorque;
			component.velocity = velocity + vector2 * force;
		}
		val.ServerActivate();
		new ThrowableNetworkHandler.ThrowableItemAudioMessage(val.Info.Serial, ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce).SendToAuthenticated();
		return val;
	}
}
