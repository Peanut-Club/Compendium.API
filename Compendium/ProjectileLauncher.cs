using System.Collections.Generic;
using System.Linq;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Extensions;
using Compendium.Value;
using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Events;
using UnityEngine;

namespace Compendium;
/* disabled
public static class ProjectileLauncher
{
	public class LauncherConfig
	{
		public Vector3 Scale;

		public ItemType Item;

		public ItemType Ammo;

		public float FuseTime;

		public float Force;
	}

	public static readonly Dictionary<ushort, LauncherConfig> Launchers = new Dictionary<ushort, LauncherConfig>();

	public static ushort AddLauncher(ReferenceHub hub, LauncherConfig config)
	{
		Firearm firearm = hub.AddItem<Firearm>(config.Item);
		firearm.Status = new FirearmStatus(byte.MaxValue, firearm.Status.Flags | FirearmStatusFlags.MagazineInserted, firearm.GetCurrentAttachmentsCode());
		if ((object)firearm == null)
		{
			return 0;
		}
		Launchers[firearm.ItemSerial] = config;
		return firearm.ItemSerial;
	}

	public static void RemoveLauncher(ushort serial, bool deleteItem = true)
	{
		Launchers.Remove(serial);
		if (!deleteItem)
		{
			return;
		}
		try
		{
			if (InventorySystem.InventoryExtensions.ServerTryGetItemWithSerial(serial, out var ib))
			{
				ib.OwnerInventory.ServerRemoveItem(ib.ItemSerial, ib.PickupDropModel);
				return;
			}
			ItemPickupBase[] source = Object.FindObjectsOfType<ItemPickupBase>();
			ItemPickupBase itemPickupBase = source.FirstOrDefault((ItemPickupBase p) => p.Info.Serial == serial);
			if (itemPickupBase != null)
			{
				itemPickupBase.DestroySelf();
			}
		}
		catch
		{
		}
	}

	[Event]
	private static void OnShooting(PlayerShotWeaponEvent ev, ValueReference isAllowed)
	{
		if (Launchers.TryGetValue(ev.Firearm.ItemSerial, out var value))
		{
			isAllowed.Value = false;
			if (value.Ammo.IsExplosive())
			{
				ev.Player.ReferenceHub.ThrownProjectile<ThrownProjectile>(value.Ammo, value.Scale, value.Force, value.FuseTime);
			}
			else
			{
				ev.Player.ReferenceHub.ThrowItem<ItemPickupBase>(value.Ammo, value.Scale, (value.Force != -1f) ? new Vector3(value.Force, 0f, 0f) : ev.Player.ReferenceHub.GetVelocity());
			}
			ev.Firearm.Status = new FirearmStatus(byte.MaxValue, ev.Firearm.Status.Flags, ev.Firearm.GetCurrentAttachmentsCode());
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		Launchers.Clear();
	}
}
*/