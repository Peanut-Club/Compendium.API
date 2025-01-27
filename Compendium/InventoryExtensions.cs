using System.Collections.Generic;
using System.Linq;
using helpers;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace Compendium;

public static class InventoryExtensions
{
	public static ItemType[] GetItemIds(this ReferenceHub hub)
	{
		return hub.inventory.UserInventory.Items.Select((KeyValuePair<ushort, ItemBase> p) => p.Value.ItemTypeId).ToArray();
	}

	public static ItemType GetCurrentItemId(this ReferenceHub hub)
	{
		return hub.inventory._curInstance?.ItemTypeId ?? ItemType.None;
	}

	public static bool SetCurrentItemId(this ReferenceHub hub, ItemType item, bool useInventory = true)
	{
		if (useInventory && hub.inventory.UserInventory.Items.TryGetFirst((KeyValuePair<ushort, ItemBase> p) => p.Value.ItemTypeId == item, out var value) && value.Value != null)
		{
			hub.inventory.ServerSelectItem(value.Key);
			if (hub.inventory._curInstance != null)
			{
				return hub.inventory._curInstance.ItemTypeId == item;
			}
			return false;
		}
		ItemBase itemBase = hub.AddItem<ItemBase>(item, dropIfFull: false);
		if ((object)itemBase == null)
		{
			return false;
		}
		hub.inventory.ServerSelectItem(itemBase.ItemSerial);
		if (hub.inventory._curInstance != null)
		{
			return hub.inventory._curInstance.ItemSerial == itemBase.ItemSerial;
		}
		return false;
	}

	public static TItem AddItem<TItem>(this ReferenceHub hub, ItemType item, bool dropIfFull = true) where TItem : ItemBase
	{
		if (hub.inventory.UserInventory.Items.Count >= 8)
		{
			if (dropIfFull)
			{
				World.SpawnItem<ItemPickupBase>(item, hub.Position(), Vector3.one, hub.Rotation());
			}
			return null;
		}
		return (TItem)hub.inventory.ServerAddItem(item, 0);
	}

	public static ItemBase[] GetItems(this ReferenceHub hub)
	{
		return hub.inventory.UserInventory.Items.Select((KeyValuePair<ushort, ItemBase> p) => p.Value).ToArray();
	}

	public static ItemBase[] GetItems(this ReferenceHub hub, ItemType itemType)
	{
		return hub.inventory.UserInventory.Items.Where(delegate(KeyValuePair<ushort, ItemBase> p)
		{
			KeyValuePair<ushort, ItemBase> keyValuePair2 = p;
			return keyValuePair2.Value.ItemTypeId == itemType;
		}).Select(delegate(KeyValuePair<ushort, ItemBase> p)
		{
			KeyValuePair<ushort, ItemBase> keyValuePair = p;
			return keyValuePair.Value;
		}).ToArray();
	}

	public static void ClearItems(this ReferenceHub hub)
	{
		hub.GetItems().ForEach(delegate(ItemBase item)
		{
			hub.inventory.ServerRemoveItem(item.ItemSerial, item.PickupDropModel);
		});
	}

	public static ushort GetAmmo(this ReferenceHub hub, ItemType ammoType)
	{
		if (!hub.inventory.UserInventory.ReserveAmmo.TryGetValue(ammoType, out var value))
		{
			return 0;
		}
		return value;
	}

	public static void SetAmmo(this ReferenceHub hub, ItemType ammoType, ushort amount)
	{
		hub.inventory.UserInventory.ReserveAmmo[ammoType] = amount;
		hub.inventory.SendAmmoNextFrame = true;
	}

	public static TProjectile ThrownProjectile<TProjectile>(this ReferenceHub hub, ItemType item, Vector3 scale, float force, float fuseTime = 2f) where TProjectile : ThrownProjectile
	{
		return World.SpawnProjectile<TProjectile>(item, hub.PlayerCameraReference.position, scale, hub.PlayerCameraReference.forward, hub.PlayerCameraReference.up, hub.PlayerCameraReference.rotation, hub.GetVelocity() * force, force, fuseTime);
	}

	public static TItem ThrowItem<TItem>(this ReferenceHub hub, ItemType item, Vector3 scale, Vector3? velocity = null) where TItem : ItemPickupBase
	{
		if (!InventoryItemLoader.TryGetItem<ItemBase>(item, out var result))
		{
			return null;
		}
		if ((object)result.PickupDropModel == null)
		{
			return null;
		}
		ItemPickupBase itemPickupBase = Object.Instantiate(result.PickupDropModel);
		if ((object)itemPickupBase == null)
		{
			return null;
		}
		if (!itemPickupBase.TryGetComponent<Rigidbody>(out var component))
		{
			return null;
		}
		PickupSyncInfo networkInfo = new PickupSyncInfo(item, result.Weight, ItemSerialGenerator.GenerateNext());
		if (!velocity.HasValue)
		{
			velocity = hub.GetVelocity();
		}
		itemPickupBase.transform.position = hub.PlayerCameraReference.position;
		itemPickupBase.transform.rotation = hub.PlayerCameraReference.rotation;
		itemPickupBase.transform.localScale = scale;
		itemPickupBase.NetworkInfo = networkInfo;
		Vector3 velocity2 = velocity.Value / 3f + hub.PlayerCameraReference.forward * 6f * (Mathf.Clamp01(Mathf.InverseLerp(7f, 0.1f, component.mass)) + 0.3f);
		velocity2.x = Mathf.Max(Mathf.Abs(velocity.Value.x), Mathf.Abs(velocity2.x)) * (float)((!(velocity2.x < 0f)) ? 1 : (-1));
		velocity2.y = Mathf.Max(Mathf.Abs(velocity.Value.y), Mathf.Abs(velocity2.y)) * (float)((!(velocity2.y < 0f)) ? 1 : (-1));
		velocity2.z = Mathf.Max(Mathf.Abs(velocity.Value.z), Mathf.Abs(velocity2.z)) * (float)((!(velocity2.z < 0f)) ? 1 : (-1));
		component.position = hub.PlayerCameraReference.position;
		component.velocity = velocity2;
		component.angularVelocity = Vector3.Lerp(result.ThrowSettings.RandomTorqueA, result.ThrowSettings.RandomTorqueB, Random.value);
		float magnitude = component.angularVelocity.magnitude;
		if (magnitude > component.maxAngularVelocity)
		{
			component.maxAngularVelocity = magnitude;
		}
		NetworkServer.Spawn(itemPickupBase.gameObject);
		return (TItem)itemPickupBase;
	}
}
