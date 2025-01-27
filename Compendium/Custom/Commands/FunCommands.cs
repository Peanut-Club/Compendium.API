using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BetterCommands;
using BetterCommands.Permissions;
using Compendium.Enums;
using Compendium.Extensions;
using Compendium.Prefabs;
using Compendium.Processors;
using Compendium.Updating;
using CustomPlayerEffects;
using Footprinting;
using helpers;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using MapGeneration;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;

namespace Compendium.Custom.Commands;

public static class FunCommands
{
	public static UnityEngine.Color? DefaultLightColor;

	public static readonly Dictionary<ReferenceHub, Tuple<ReferenceHub, bool>> PlayerGrabs;

	public static readonly Dictionary<ReferenceHub, ItemPickupBase> ItemGrabs;

	[BetterCommands.Command("bones", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[Description("Spawns a skeleton at the specified player.")]
	[Permission(PermissionLevel.Medium)]
	public static string BonesCommand(ReferenceHub sender, ReferenceHub target)
	{
		if (!(target.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return "The targeted player is not playing as a first-person role.";
		}
		HalloweenUtils.SpawnBones(fpcRole.FpcModule.Position);
		return "Spawned a skeleton at " + target.Nick();
	}

	[BetterCommands.Command("rocket", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Sends a player into space.")]
	[Permission(PermissionLevel.High)]
	public static string RocketCommand(ReferenceHub sender, ReferenceHub target)
	{
		if (RocketProcessor.IsActive(target))
		{
			RocketProcessor.Remove(target);
			return "Rocket of " + target.Nick() + " disabled.";
		}
		RocketProcessor.Add(target);
		return "Sent " + target.Nick() + " into space.";
	}

	[BetterCommands.Command("scale", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Multiplies the targeted player's current scale.")]
	[Permission(PermissionLevel.High)]
	public static string ScaleCommand(ReferenceHub sender, ReferenceHub target, float scale)
	{
		target.SetScale(scale);
		return $"Scaled player '{target.Nick()}' by {scale}";
	}

	[BetterCommands.Command("size", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Sets the targeted player's current model size.")]
	[Permission(PermissionLevel.High)]
	public static string SizeCommand(ReferenceHub sender, ReferenceHub target, Vector3 size)
	{
		target.SetSize(size);
		return $"Set size of '{target.Nick()}' to {size}";
	}

	[BetterCommands.Command("facilitycolor", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[CommandAliases(new object[] { "fcolor" })]
	[Description("Changes the color of all facility lights. Use 'reset' as color to reset all lights to their default color.")]
	[Permission(PermissionLevel.Low)]
	public static string FacilityColorCommand(ReferenceHub sender, string color)
	{
		if (color.ToLower() == "reset")
		{
			UnityEngine.Color? defaultLightColor = DefaultLightColor;
			if (!defaultLightColor.HasValue)
			{
				return "Can't reset light color - never changed.";
			}
			foreach (RoomLightController instance in RoomLightController.Instances)
			{
				instance.NetworkOverrideColor = DefaultLightColor.Value;
			}
			return "Reset color of all lights in the facility.";
		}
		if (color.StartsWith("#"))
		{
			try
			{
				System.Drawing.Color color2 = ColorTranslator.FromHtml(color);
				UnityEngine.Color color3 = new UnityEngine.Color((float)(int)color2.R / 255f, (float)(int)color2.G / 255f, (float)(int)color2.B / 255f);
				foreach (RoomLightController instance2 in RoomLightController.Instances)
				{
					UnityEngine.Color? defaultLightColor2 = DefaultLightColor;
					if (!defaultLightColor2.HasValue)
					{
						DefaultLightColor = instance2.NetworkOverrideColor;
					}
					instance2.NetworkOverrideColor = color3;
				}
				return $"Set color of all lights to {color} ({color2} - {color3}) VIA HEX";
			}
			catch (Exception arg)
			{
				return $"Caught an exception while parsing HEX color:\n{arg}";
			}
		}
		string[] array = color.Split(new char[1] { ' ' });
		if (array.Length != 3)
		{
			return "The RGB color format must be spaced like this: \"R G B\"";
		}
		if (!byte.TryParse(array[0], out var result) || !byte.TryParse(array[1], out var result2) || !byte.TryParse(array[2], out var result3))
		{
			return "Failed to parse RGB color.";
		}
		UnityEngine.Color color4 = new UnityEngine.Color((float)(int)result / 255f, (float)(int)result2 / 255f, (float)(int)result3 / 255f);
		foreach (RoomLightController instance3 in RoomLightController.Instances)
		{
			UnityEngine.Color? defaultLightColor3 = DefaultLightColor;
			if (!defaultLightColor3.HasValue)
			{
				DefaultLightColor = instance3.NetworkOverrideColor;
			}
			instance3.NetworkOverrideColor = color4;
		}
		return $"Set color of all lights to {result};{result2};{result3} ({color4}) VIA RGB";
	}

	[BetterCommands.Command("changeperms", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Changes door permissions in a certain room.")]
	[Permission(PermissionLevel.Medium)]
	public static string ChangePermsCommand(ReferenceHub sender, RoomName room, KeycardPermissions permissions)
	{
		foreach (DoorVariant allDoor in DoorVariant.AllDoors)
		{
			if (allDoor.Rooms != null && allDoor.Rooms.Any((RoomIdentifier r) => r.Name == room))
			{
				allDoor.RequiredPermissions.RequiredPermissions = permissions;
			}
		}
		return $"Set permissions of all doors in {room} to {permissions}";
	}
	/*
	[BetterCommands.Command("spawndoor", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Spawns a door.")]
	[Permission(PermissionLevel.Medium)]
	public static string SpawnDoor(ReferenceHub sender, PrefabName doorType)
	{
		if (!PrefabHelper.TrySpawnDoor(doorType, sender.transform.position, Vector3.one, sender.transform.rotation, string.Empty, shouldSpawn: true, out var _))
		{
			return "Failed to spawn door!";
		}
		return "Door spawned.";
	}*/

	[BetterCommands.Command("pitch", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[Description("Changes a player's voice pitch (1 resets pitch to default).")]
	[Permission(PermissionLevel.Low)]
	public static string SetPitchCommand(ReferenceHub sender, ReferenceHub target, float pitch)
	{
		target.SetVoicePitch(pitch);
		return $"Set pitch of '{target.Nick()}' to {pitch}";
	}

	[BetterCommands.Command("spawnitem", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Spawns an item with a custom scale.")]
	[Permission(PermissionLevel.Medium)]
	public static string SpawnItemCommand(ReferenceHub sender, ItemType item, Vector3 scale)
	{
		World.SpawnItem<ItemPickupBase>(item, sender.transform.position, scale, sender.transform.rotation);
		return "Pickup spawned.";
	}

	[BetterCommands.Command("throwitem", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Throws the specified amount of items.")]
	[Permission(PermissionLevel.Medium)]
	public static string ThrowItemCommand(ReferenceHub sender, ItemType item, Vector3 scale, int count = 1, Vector3 velocity = default(Vector3))
	{
		for (int i = 0; i < count; i++)
		{
			sender.ThrowItem<ItemPickupBase>(item, scale, (velocity == default(Vector3)) ? null : new Vector3?(velocity));
		}
		return $"Thrown {count} of {item}";
	}

	[BetterCommands.Command("targetthrowitem", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "ttargetthrowitem" })]
	[Description("Throws the specified amount of items.")]
	[Permission(PermissionLevel.Medium)]
	public static string TargetThrowItemCommand(ReferenceHub sender, ReferenceHub target, ItemType item, Vector3 scale, int count = 1, Vector3 velocity = default(Vector3))
	{
		for (int i = 0; i < count; i++)
		{
			target.ThrowItem<ItemPickupBase>(item, scale, (velocity == default(Vector3)) ? null : new Vector3?(velocity));
		}
		return $"Thrown {count} of {item}";
	}

	[BetterCommands.Command("spawnprojectile", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Spawns a projectile with a custom scale.")]
	[Permission(PermissionLevel.Medium)]
	public static string SpawnProjectileCommand(ReferenceHub sender, ItemType item, Vector3 scale, float force, float fuseTime = 2f)
	{
		sender.ThrownProjectile<ThrownProjectile>(item, scale, force, fuseTime);
		return "Projectile spawned.";
	}
	/* disabled
	[BetterCommands.Command("disruptor", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Literally kills the person you are looking at.")]
	[Permission(PermissionLevel.Medium)]
	private static string DisruptorCommand(ReferenceHub sender)
	{
		if (!Physics.Raycast(new Ray(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward), out var hitInfo, 100f, -1))
		{
			return "No targets were hit.";
		}
		DisruptorHitreg.DisruptorHitMessage message = default(DisruptorHitreg.DisruptorHitMessage);
		message.Position = hitInfo.point + hitInfo.normal * 0.1f;
		message.Rotation = new LowPrecisionQuaternion(Quaternion.LookRotation(-hitInfo.normal));
		message.SendToAuthenticated();
		if (hitInfo.collider.gameObject.TryGet<IDestructible>(out var result) && ReferenceHub.TryGetHubNetID(result.NetworkId, out var hub))
		{
			if (hub != sender)
			{
				hub.characterClassManager.GodMode = false;
				hub.playerStats.KillPlayer(new DisruptorDamageHandler(new Footprint(sender), -1f));
			}
			return "Hit player: " + hub.Nick();
		}
		if (hitInfo.collider.gameObject.TryGet<DoorVariant>(out var result2))
		{
			BreakableDoor breakableDoor = result2 as BreakableDoor;
			if (breakableDoor != null)
			{
				breakableDoor.Network_destroyed = true;
				return "Hit door: " + result2.name;
			}
			Destroy(result2.gameObject);
			return "Hit unbreakable door: " + result2.name;
		}
		if (hitInfo.collider.gameObject.TryGet<NetworkBehaviour>(out var result3))
		{
			Destroy(result3.gameObject);
			return "Hit network behaviour: " + result3.name;
		}
		if (hitInfo.collider.gameObject.TryGet<NetworkIdentity>(out var result4))
		{
			Destroy(result4.gameObject);
			return "Hit network identity: " + result4.name;
		}
		return "Nothing hit.";
	}

	[BetterCommands.Command("targetdisruptor", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "tdisruptor" })]
	[Description("Literally kills the target is looking at.")]
	[Permission(PermissionLevel.Medium)]
	public static string TargetDisruptorCommand(ReferenceHub sender, ReferenceHub target)
	{
		if (!Physics.Raycast(new Ray(target.PlayerCameraReference.position, target.PlayerCameraReference.forward), out var hitInfo, 100f, -1))
		{
			return "No targets were hit.";
		}
		DisruptorHitreg.DisruptorHitMessage message = default(DisruptorHitreg.DisruptorHitMessage);
		message.Position = hitInfo.point + hitInfo.normal * 0.1f;
		message.Rotation = new LowPrecisionQuaternion(Quaternion.LookRotation(-hitInfo.normal));
		message.SendToAuthenticated();
		if (hitInfo.collider.gameObject.TryGet<IDestructible>(out var result) && ReferenceHub.TryGetHubNetID(result.NetworkId, out var hub))
		{
			if (hub != target)
			{
				hub.characterClassManager.GodMode = false;
				hub.playerStats.KillPlayer(new DisruptorDamageHandler(new Footprint(target), -1f));
			}
			return "Hit player: " + hub.Nick();
		}
		if (hitInfo.collider.gameObject.TryGet<DoorVariant>(out var result2))
		{
			BreakableDoor breakableDoor = result2 as BreakableDoor;
			if (breakableDoor != null)
			{
				breakableDoor.Network_destroyed = true;
				return "Hit door: " + result2.name;
			}
			Destroy(result2.gameObject);
			return "Hit unbreakable door: " + result2.name;
		}
		if (hitInfo.collider.gameObject.TryGet<NetworkBehaviour>(out var result3) && !hitInfo.collider.gameObject.TryGet<ReferenceHub>(out var result4) && (!hitInfo.collider.gameObject.TryGet<IDestructible>(out result) || !ReferenceHub.TryGetHubNetID(result.NetworkId, out result4)))
		{
			Destroy(result3.gameObject);
			return "Hit network behaviour: " + result3.name;
		}
		if (hitInfo.collider.gameObject.TryGet<NetworkIdentity>(out var result5) && !hitInfo.collider.gameObject.TryGet<ReferenceHub>(out result4) && (!hitInfo.collider.gameObject.TryGet<IDestructible>(out result) || !ReferenceHub.TryGetHubNetID(result.NetworkId, out result4)))
		{
			Destroy(result5.gameObject);
			return "Hit network identity: " + result5.name;
		}
		return "Nothing hit.";
	}*/

	[BetterCommands.Command("grenade", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[Description("Spawns the specified amount of active grenades on the player.")]
	[Permission(PermissionLevel.Medium)]
	private static string GrenadeCommand(ReferenceHub sender, ReferenceHub target, int count, Vector3 size, float fuseTime = 1f)
	{
		for (int i = 0; i < count; i++)
		{
			target.ThrownProjectile<ThrownProjectile>(ItemType.GrenadeHE, size, 0f, fuseTime);
		}
		Timing.CallDelayed(fuseTime + 1f, delegate
		{
			target.playerEffectsController.DisableEffect<Blindness>();
			target.playerEffectsController.DisableEffect<Deafened>();
			target.playerEffectsController.DisableEffect<Flashed>();
			target.playerEffectsController.DisableEffect<Burned>();
		});
		return $"Spawned {count} grenades on {target.Nick()}";
	}

	[BetterCommands.Command("ball", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[Description("Spawns the specified amount of active balls on the player.")]
	[Permission(PermissionLevel.Medium)]
	private static string BallCommand(ReferenceHub sender, ReferenceHub target, int count, Vector3 size)
	{
		for (int i = 0; i < count; i++)
		{
			target.ThrownProjectile<ThrownProjectile>(ItemType.SCP018, size, 0f);
		}
		return $"Spawned {count} balls on {target.Nick()}";
	}

	[BetterCommands.Command("flash", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[Description("Spawns the specified amount of active flashes on the player.")]
	[Permission(PermissionLevel.Medium)]
	private static string FlashCommand(ReferenceHub sender, ReferenceHub target, int count, Vector3 size, float fuseTime = 1f)
	{
		for (int i = 0; i < count; i++)
		{
			target.ThrownProjectile<ThrownProjectile>(ItemType.GrenadeFlash, size, 0f, fuseTime);
		}
		Timing.CallDelayed(fuseTime + 1f, delegate
		{
			target.playerEffectsController.DisableEffect<Blindness>();
			target.playerEffectsController.DisableEffect<Deafened>();
			target.playerEffectsController.DisableEffect<Flashed>();
		});
		return $"Spawned {count} flashes on {target.Nick()}";
	}
	/* disabled
	[BetterCommands.Command("addprojectilelauncher", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "addlauncher" })]
	[Description("Gives the specified player a projectile launcher.")]
	[Permission(PermissionLevel.Medium)]
	private static string GrenadeLauncherCommand(ReferenceHub sender, ReferenceHub target, ItemType gunType, ItemType ammoType, Vector3 scale, float ammoForce, float ammoFuse = 2f)
	{
		ushort num = ProjectileLauncher.AddLauncher(target, new ProjectileLauncher.LauncherConfig
		{
			Item = gunType,
			Ammo = ammoType,
			Force = ammoForce,
			FuseTime = ammoFuse,
			Scale = scale
		});
		if (num == 0)
		{
			return "Failed to add launcher.";
		}
		return "Added a projetile launcher to " + target.Nick() + "'s inventory:\n" + $"Serial: {num}\n" + $"Gun Type: {gunType}\n" + $"Ammo Type: {ammoType}\n" + $"Scale: {scale}\n" + $"Force: {ammoForce}\n" + $"Fuse: {ammoFuse}";
	}

	[BetterCommands.Command("listprojectilelaunchers", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "listlaunchers" })]
	[Description("Shows a list of spawned projectile launchers.")]
	[Permission(PermissionLevel.Medium)]
	private static string ListProjectileLaunchersCommand(ReferenceHub sender)
	{
		if (!ProjectileLauncher.Launchers.Any())
		{
			return "There aren't any projectile launchers.";
		}
		string text = $"Projectile Launchers ({ProjectileLauncher.Launchers.Count}):\n";
		ItemPickupBase[] source = UnityEngine.Object.FindObjectsOfType<ItemPickupBase>();
		foreach (KeyValuePair<ushort, ProjectileLauncher.LauncherConfig> launcher in new Dictionary<ushort, ProjectileLauncher.LauncherConfig>(ProjectileLauncher.Launchers))
		{
			try
			{
				if (InventorySystem.InventoryExtensions.ServerTryGetItemWithSerial(launcher.Key, out var ib))
				{
					text += $"[{launcher.Key}]: {ib.ItemTypeId} / {launcher.Value.Ammo} (owned by: [{ib.Owner.PlayerId}] {ib.Owner.Nick()})\n";
					continue;
				}
				ItemPickupBase itemPickupBase = source.FirstOrDefault((ItemPickupBase p) => p.Info.Serial == launcher.Key);
				if (itemPickupBase == null)
				{
					ProjectileLauncher.Launchers.Remove(launcher.Key);
					continue;
				}
				string text2 = text;
				string format = "[{0}]: {1} / {2} (spawned at: {3} [Room: {4}])\n";
				object[] array = new object[5]
				{
					launcher.Key,
					itemPickupBase.Info.ItemId,
					launcher.Value.Ammo,
					itemPickupBase.transform.position,
					null
				};
				int num = 4;
				RoomIdentifier roomIdentifier = RoomIdUtils.RoomAtPosition(itemPickupBase.transform.position);
				array[num] = ((roomIdentifier != null) ? roomIdentifier.Name : RoomName.Unnamed);
				text = text2 + string.Format(format, array);
			}
			catch
			{
			}
		}
		return text;
	}

	[BetterCommands.Command("deleteprojectilelauncher", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "deletelauncher" })]
	[Description("Deletes a spawned projectile launcher.")]
	[Permission(PermissionLevel.Medium)]
	private static string DeleteProjectileLauncherCommand(ReferenceHub sender, ushort launcherSerial, bool deleteItem = true)
	{
		if (!ProjectileLauncher.Launchers.ContainsKey(launcherSerial))
		{
			return "There aren't any launchers with the specified item serial.";
		}
		ProjectileLauncher.RemoveLauncher(launcherSerial, deleteItem);
		return $"Launcher with serial {launcherSerial} destroyed.";
	}
	*/

	[BetterCommands.Command("projectilefollow", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "projf" })]
	[Description("Spawns a projectile that follows a player.")]
	[Permission(PermissionLevel.Medium)]
	private static string FlashFollowCommand(ReferenceHub sender, ReferenceHub target, ItemType item, Vector3 scale, float fuse, int count = 1)
	{
		List<TimeGrenade> list = new List<TimeGrenade>();
		for (int i = 0; i < count; i++)
		{
			list.Add(World.SpawnNonActiveProjectile<TimeGrenade>(item, target.transform.position, scale, target.PlayerCameraReference.forward, target.PlayerCameraReference.up, target.transform.rotation, Vector3.zero, 0f));
		}
		Timing.RunCoroutine(FollowThrowable(target, list, fuse));
		return $"Spawned {count} following players at {target.Nick()}.";
	}

	[BetterCommands.Command("firegates", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "fireg" })]
	[Description("Fires all Tesla Gates within radius.")]
	[Permission(PermissionLevel.Medium)]
	private static string FireGatesCommnad(ReferenceHub sender, float distance)
	{
		TeslaGate.AllGates.ForEach(delegate(TeslaGate gate)
		{
			try
			{
				if (gate.transform.position.IsWithinDistance(sender.transform.position, distance))
				{
					gate.ServerSideCode();
				}
			}
			catch
			{
			}
		});
		return "Fired all Tesla Gates";
	}

	[BetterCommands.Command("grabplayer", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "gplayer" })]
	[Description("Grabs a player.")]
	[Permission(PermissionLevel.High)]
	public static string GrabPlayerCommand(ReferenceHub sender, bool rotate = false)
	{
		if (!Physics.Raycast(new Ray(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward), out var hitInfo, 100f, -1) || !hitInfo.collider.gameObject.TryGet<IDestructible>(out var result) || !ReferenceHub.TryGetHubNetID(result.NetworkId, out var hub) || hub == sender)
		{
			return "No targets were hit.";
		}
		PlayerGrabs[sender] = new Tuple<ReferenceHub, bool>(hub, rotate);
		hub.characterClassManager.GodMode = true;
		return "Grabbed player " + hub.Nick();
	}

	[BetterCommands.Command("grabitem", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "gitem" })]
	[Description("Grabs an item.")]
	[Permission(PermissionLevel.High)]
	public static string GrabItemCommand(ReferenceHub sender)
	{
		ItemPickupBase itemPickupBase = null;
		RaycastHit[] array = Physics.RaycastAll(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward);
		RaycastHit[] array2 = array;
		foreach (RaycastHit raycastHit in array2)
		{
			ItemPickupBase componentInParent = raycastHit.transform.gameObject.gameObject.GetComponentInParent<ItemPickupBase>();
			if (componentInParent != null)
			{
				itemPickupBase = componentInParent;
				break;
			}
		}
		if (itemPickupBase == null)
		{
			return "No pickups were hit.";
		}
		ItemGrabs[sender] = itemPickupBase;
		return $"Grabbed item {itemPickupBase.Info.ItemId}";
	}

	[BetterCommands.Command("stopgrab", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "stgrab" })]
	[Description("Stops grabbing.")]
	[Permission(PermissionLevel.High)]
	public static string StopGrabPlayerCommand(ReferenceHub sender)
	{
		if (PlayerGrabs.TryGetValue(sender, out var value))
		{
			value.Item1.characterClassManager.GodMode = false;
		}
		PlayerGrabs.Remove(sender);
		if (ItemGrabs.TryGetValue(sender, out var value2) && value2.PhysicsModule != null && value2.PhysicsModule is PickupStandardPhysics pickupStandardPhysics && pickupStandardPhysics.Rb != null)
		{
			pickupStandardPhysics.Rb.mass = 1f;
			pickupStandardPhysics.Rb.useGravity = true;
		}
		ItemGrabs.Remove(sender);
		return "Grab stopped.";
	}

	[BetterCommands.Command("listbehaviours", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "lbehaviours" })]
	[Description("Lists all network behaviours.")]
	[Permission(PermissionLevel.Lowest)]
	public static string ListBehaviours(ReferenceHub sender)
	{
		NetworkBehaviour[] array = (from b in UnityEngine.Object.FindObjectsOfType<NetworkBehaviour>()
			orderby b.name
			where !b.name.Contains("GameManager") && !b.name.Contains("Player") && !b.name.Contains("RoomLight") && !b.name.Contains("All")
			select b).ToArray();
		string text = $"Behaviours ({array.Length}):\n";
		for (int i = 0; i < array.Length; i++)
		{
			text += $"[{array[i].netId}] {array[i].name} {array[i].tag} ({array[i].GetType().FullName})\n";
		}
		return text;
	}

	[BetterCommands.Command("destroybehaviour", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "dbehaviour" })]
	[Description("Destroys a network behaviour.")]
	[Permission(PermissionLevel.High)]
	public static string DestroyBehaviour(ReferenceHub sender, uint behaviourId)
	{
		NetworkBehaviour networkBehaviour = (from b in UnityEngine.Object.FindObjectsOfType<NetworkBehaviour>()
			orderby b.name
			where !b.name.Contains("GameManager") && !b.name.Contains("Player") && !b.name.Contains("RoomLight") && !b.name.Contains("All")
			select b).FirstOrDefault((NetworkBehaviour b) => b.netId == behaviourId);
		if (networkBehaviour == null)
		{
			return "Unknown identity.";
		}
		Destroy(networkBehaviour.gameObject);
		return "Destroyed behaviour " + networkBehaviour.name;
	}

	[BetterCommands.Command("spawnprefab", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "sprefab" })]
	[Description("Spawns a prefab.")]
	[Permission(PermissionLevel.High)]
	public static string SpawnPrefabCommand(ReferenceHub sender, PrefabName prefabName, Vector3 scale, Quaternion rotation)
	{
		if (PrefabHelper.TryInstantiatePrefab(prefabName, out var instance))
		{
			instance.transform.localScale = scale;
			Vector3 position = sender.transform.position;
			position.y -= 1f;
			instance.transform.position = position;
			instance.transform.rotation = rotation;
			Spawn(instance.gameObject);
			return "Spawned prefab: " + instance.name;
		}
		return $"Failed to instantiate prefab {prefabName}";
	}

	private static void Spawn(GameObject gameObject)
	{
		NetworkServer.Spawn(gameObject);
	}

	private static void UnSpawn(GameObject gameObject)
	{
		NetworkServer.UnSpawn(gameObject);
	}

	private static void Destroy(GameObject gameObject)
	{
		NetworkServer.Destroy(gameObject);
	}

	private static IEnumerator<float> FollowThrowable(ReferenceHub target, IEnumerable<TimeGrenade> grenades, float time)
	{
		float curTime = 0f;
		while (curTime < time)
		{
			yield return Timing.WaitForSeconds(0.01f);
			curTime += 0.01f;
			if (!target.IsAlive())
			{
				grenades.ForEach(delegate(TimeGrenade nade)
				{
					NetworkServer.Destroy(nade.gameObject);
				});
				yield break;
			}
			grenades.ForEach(delegate(TimeGrenade nade)
			{
				nade.transform.position = target.transform.position;
				nade.transform.rotation = target.transform.rotation;
			});
		}
		grenades.ForEach(delegate(TimeGrenade nade)
		{
			nade._fuseTime = 0.1f;
			nade.ServerActivate();
		});
	}

	[Update(Delay = 10, IsUnity = true)]
	private static void OnUpdate()
	{
		foreach (KeyValuePair<ReferenceHub, Tuple<ReferenceHub, bool>> playerGrab in PlayerGrabs)
		{
			if (!(playerGrab.Key != null) || playerGrab.Value == null || !(playerGrab.Value.Item1 != null) || !(playerGrab.Value.Item1.gameObject != null))
			{
				continue;
			}
			try
			{
				if (playerGrab.Value.Item1.IsAlive())
				{
					if (!playerGrab.Value.Item2)
					{
						playerGrab.Value.Item1.TryOverridePosition(playerGrab.Key.PlayerCameraReference.position + playerGrab.Key.PlayerCameraReference.forward * 2f, Vector3.zero);
					}
					else
					{
						playerGrab.Value.Item1.TryOverridePosition(playerGrab.Key.PlayerCameraReference.position + playerGrab.Key.PlayerCameraReference.forward * 2f, new Vector3(3277f, 3277f, 3277f));
					}
				}
			}
			catch
			{
			}
		}
		foreach (KeyValuePair<ReferenceHub, ItemPickupBase> itemGrab in ItemGrabs)
		{
			if (!(itemGrab.Key != null) || !(itemGrab.Value != null))
			{
				continue;
			}
			try
			{
				itemGrab.Value.transform.position = itemGrab.Key.PlayerCameraReference.position + itemGrab.Key.PlayerCameraReference.forward * 2f;
				itemGrab.Value.transform.LookAt(itemGrab.Key.PlayerCameraReference);
				if (itemGrab.Value.PhysicsModule != null && itemGrab.Value.PhysicsModule is PickupStandardPhysics pickupStandardPhysics && pickupStandardPhysics.Rb != null)
				{
					pickupStandardPhysics.Rb.mass = 0f;
					pickupStandardPhysics.Rb.useGravity = false;
					pickupStandardPhysics.Rb.constraints = (RigidbodyConstraints)48;
					pickupStandardPhysics.Rb.MovePosition(itemGrab.Key.PlayerCameraReference.position + itemGrab.Key.PlayerCameraReference.forward * 2f);
					pickupStandardPhysics.Rb.transform.eulerAngles = itemGrab.Key.transform.eulerAngles;
				}
			}
			catch
			{
			}
		}
	}

	static FunCommands()
	{
		PlayerGrabs = new Dictionary<ReferenceHub, Tuple<ReferenceHub, bool>>();
		ItemGrabs = new Dictionary<ReferenceHub, ItemPickupBase>();
	}
}
