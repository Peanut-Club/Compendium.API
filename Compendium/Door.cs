using System;
using System.Collections.Generic;
using System.Linq;
using Compendium.Attributes;
using Compendium.Enums;
using helpers;
using helpers.Patching;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PluginAPI.Events;
using UnityEngine;

namespace Compendium;

public static class Door
{
	private static Dictionary<uint, KeycardPermissions> _customPerms = new Dictionary<uint, KeycardPermissions>();

	private static Dictionary<uint, Func<DoorVariant, ReferenceHub, bool>> _customAccessModifiers = new Dictionary<uint, Func<DoorVariant, ReferenceHub, bool>>();

	private static Dictionary<uint, List<uint>> _plyWhitelist = new Dictionary<uint, List<uint>>();

	private static Dictionary<uint, List<uint>> _plyBlacklist = new Dictionary<uint, List<uint>>();

	private static HashSet<uint> _disabled = new HashSet<uint>();

	public static IReadOnlyCollection<DoorVariant> Doors => DoorVariant.AllDoors;

	public static IReadOnlyCollection<DoorLockReason> LockTypes { get; } = (IReadOnlyCollection<DoorLockReason>)(object)new DoorLockReason[10]
	{
		DoorLockReason.Lockdown079,
		DoorLockReason.Lockdown2176,
		DoorLockReason.DecontLockdown,
		DoorLockReason.Isolation,
		DoorLockReason.Warhead,
		DoorLockReason.Regular079,
		DoorLockReason.AdminCommand,
		DoorLockReason.DecontEvacuate,
		DoorLockReason.NoPower,
		DoorLockReason.SpecialDoorFeature
	};


	public static bool IsOpened(this DoorVariant door)
	{
		return door.TargetState;
	}

	public static bool IsClosed(this DoorVariant door)
	{
		return !door.TargetState;
	}

	public static bool IsDisabled(this DoorVariant door)
	{
		return _disabled.Contains(door.netId);
	}

	public static bool IsDestroyed(this DoorVariant door)
	{
		if (door is BreakableDoor breakableDoor)
		{
			return breakableDoor.Network_destroyed;
		}
		return false;
	}

	public static bool IsGate(this DoorVariant door)
	{
		return door is PryableDoor;
	}

	public static bool IsTimed(this DoorVariant door)
	{
		return door is Timed173PryableDoor;
	}

	public static bool IsInteractable(this DoorVariant door)
	{
		DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)door.NetworkActiveLocks);
		if (mode == DoorLockMode.FullLock)
		{
			return false;
		}
		if (door.IsOpened() && !mode.HasFlagFast(DoorLockMode.CanClose))
		{
			return false;
		}
		if (door.IsClosed() && !mode.HasFlagFast(DoorLockMode.CanOpen))
		{
			return false;
		}
		return true;
	}

	public static bool IsBlacklisted(this DoorVariant door, ReferenceHub hub)
	{
		if (_plyBlacklist.TryGetValue(door.netId, out var value))
		{
			return value.Contains(hub.netId);
		}
		return false;
	}

	public static bool IsWhitelisted(this DoorVariant door, ReferenceHub hub)
	{
		if (_plyWhitelist.TryGetValue(door.netId, out var value))
		{
			return value.Contains(hub.netId);
		}
		return false;
	}

	public static bool RequiresWhitelist(this DoorVariant door)
	{
		if (_plyWhitelist.TryGetValue(door.netId, out var value))
		{
			return value.Any();
		}
		return false;
	}

	public static bool HasCustomAccessModifier(this DoorVariant door, out Func<DoorVariant, ReferenceHub, bool> modifier)
	{
		return _customAccessModifiers.TryGetValue(door.netId, out modifier);
	}

	public static bool HasCustomAccessModifier(this DoorVariant door)
	{
		Func<DoorVariant, ReferenceHub, bool> modifier;
		return door.HasCustomAccessModifier(out modifier);
	}

	public static bool HasCustomPermissions(this DoorVariant door, out KeycardPermissions customPermissions)
	{
		return _customPerms.TryGetValue(door.netId, out customPermissions);
	}

	public static bool HasCustomPermissions(this DoorVariant door)
	{
		KeycardPermissions customPermissions;
		return door.HasCustomPermissions(out customPermissions);
	}

	public static void SetCustomPermissions(this DoorVariant door, KeycardPermissions keycardPermissions)
	{
		_customPerms[door.netId] = keycardPermissions;
	}

	public static void Override(this DoorVariant door, Func<DoorVariant, ReferenceHub, bool> modifier)
	{
		_customAccessModifiers[door.netId] = modifier;
	}

	public static void RemoveOverride(this DoorVariant door)
	{
		_customAccessModifiers.Remove(door.netId);
	}

	public static void ClearOverrides()
	{
		_customAccessModifiers.Clear();
	}

	public static void ClearCustomPermissions(this DoorVariant door)
	{
		_customPerms.Remove(door.netId);
	}

	public static void DisableInteracting(this DoorVariant door)
	{
		_disabled.Add(door.netId);
	}

	public static void EnableInteracting(this DoorVariant door)
	{
		_disabled.Remove(door.netId);
	}

	public static void ToggleInteracting(this DoorVariant door)
	{
		if (_disabled.Contains(door.netId))
		{
			_disabled.Remove(door.netId);
		}
		else
		{
			_disabled.Add(door.netId);
		}
	}

	public static void SetOpened(this DoorVariant door, bool isOpened = true)
	{
		door.NetworkTargetState = isOpened;
	}

	public static bool Toggle(this DoorVariant door)
	{
		return door.NetworkTargetState = !door.NetworkTargetState;
	}

	public static void ToggleAfterDelay(this DoorVariant door, float seconds, out bool newState)
	{
		bool state = !door.NetworkTargetState;
		newState = state;
		Calls.Delay(seconds, delegate
		{
			door.SetOpened(state);
		});
	}

	public static void ToggleAfterDelay(this DoorVariant door, float seconds)
	{
		door.ToggleAfterDelay(seconds, out var _);
	}

	public static void Close(this DoorVariant door)
	{
		door.SetOpened(isOpened: false);
	}

	public static void CloseAfterDelay(this DoorVariant door, float seconds)
	{
		Calls.Delay(seconds, door.Close);
	}

	public static void Open(this DoorVariant door)
	{
		door.SetOpened();
	}

	public static void OpenAfterDelay(this DoorVariant door, float seconds)
	{
		Calls.Delay(seconds, door.Open);
	}

	public static void Lock(this DoorVariant door, DoorLockReason lockType = DoorLockReason.AdminCommand)
	{
		door.ServerChangeLock(lockType, newState: true);
	}

	public static void LockAfterDelay(this DoorVariant door, float seconds, DoorLockReason lockType = DoorLockReason.AdminCommand)
	{
		Calls.Delay(seconds, delegate
		{
			door.Lock(lockType);
		});
	}

	public static void Unlock(this DoorVariant door, DoorLockReason lockType = DoorLockReason.AdminCommand)
	{
		door.ServerChangeLock(lockType, newState: false);
	}

	public static void UnlockAfterDelay(this DoorVariant door, float seconds, DoorLockReason lockType = DoorLockReason.AdminCommand)
	{
		Calls.Delay(seconds, delegate
		{
			door.Unlock(lockType);
		});
	}

	public static void UnlockAll(this DoorVariant door)
	{
		LockTypes.ForEach(door.Unlock);
	}

	public static void UnlockAllAfterDelay(this DoorVariant door, float seconds)
	{
		Calls.Delay(seconds, door.UnlockAll);
	}

	public static void Whitelist(this DoorVariant door, ReferenceHub hub)
	{
		if (_plyWhitelist.ContainsKey(door.netId))
		{
			_plyWhitelist[door.netId].Add(hub.netId);
			return;
		}
		_plyWhitelist.Add(door.netId, new List<uint> { hub.netId });
	}

	public static void Blacklist(this DoorVariant door, ReferenceHub hub)
	{
		if (_plyBlacklist.ContainsKey(door.netId))
		{
			_plyBlacklist[door.netId].Add(hub.netId);
			return;
		}
		_plyBlacklist.Add(door.netId, new List<uint> { hub.netId });
	}

	public static void Destroy(this DoorVariant door, bool clearDebris = false)
	{
		if (door is BreakableDoor breakableDoor)
		{
			breakableDoor.Network_destroyed = true;
		}
		if (clearDebris)
		{
			Calls.Delay(1.25f, door.Delete);
		}
	}

	public static void DestroyAfterDelay(this DoorVariant door, float seconds, bool clearDebris = false)
	{
		Calls.Delay(seconds, delegate
		{
			door.Destroy(clearDebris);
		});
	}

	public static float GetHealth(this DoorVariant door)
	{
		if (!(door is BreakableDoor breakableDoor))
		{
			return 0f;
		}
		return breakableDoor.RemainingHealth;
	}

	public static float GetMaxHealth(this DoorVariant door)
	{
		if (!(door is BreakableDoor breakableDoor))
		{
			return 0f;
		}
		return breakableDoor.MaxHealth;
	}

	public static void SetHealth(this DoorVariant door, float health)
	{
		if (door is BreakableDoor breakableDoor)
		{
			breakableDoor.RemainingHealth = health;
		}
	}

	public static void SetMaxHealth(this DoorVariant door, float max, bool healToFull = false)
	{
		if (door is BreakableDoor breakableDoor)
		{
			breakableDoor.MaxHealth = max;
			if (breakableDoor.RemainingHealth > breakableDoor.MaxHealth || healToFull)
			{
				breakableDoor.RemainingHealth = breakableDoor.MaxHealth;
			}
		}
	}

	public static void Damage(this DoorVariant door, float damage, DoorDamageType damageType = DoorDamageType.ServerCommand)
	{
		if (door is BreakableDoor breakableDoor)
		{
			breakableDoor.ServerDamage(damage, damageType);
		}
	}

	public static void Delete(this DoorVariant door)
	{
		NetworkServer.UnSpawn(door.gameObject);
	}

	public static void DeleteAfterDelay(this DoorVariant door, float seconds)
	{
		Calls.Delay(seconds, door.Delete);
	}

	public static void PlayPermissionsDenied(this DoorVariant door, ReferenceHub hub)
	{
		door.PermissionsDenied(hub, 0);
	}

	public static void DisableColliders(this DoorVariant door)
	{
		door._colliders.ForEach(delegate(BoxCollider collider)
		{
			collider.isTrigger = false;
		});
	}

	public static void EnableColliders(this DoorVariant door)
	{
		door._colliders.ForEach(delegate(BoxCollider collider)
		{
			collider.isTrigger = true;
		});
	}

	public static string GetTag(this DoorVariant door)
	{
		if (!DoorNametagExtension.NamedDoors.TryGetFirst((KeyValuePair<string, DoorNametagExtension> d) => d.Value.TargetDoor != null && d.Value.TargetDoor.netId == door.netId, out var value))
		{
			return "Unnamed Door";
		}
		return value.Key;
	}

	public static void SetTag(this DoorVariant door, string newTag)
	{
		if (DoorNametagExtension.NamedDoors.TryGetFirst((KeyValuePair<string, DoorNametagExtension> d) => d.Value.TargetDoor != null && d.Value.TargetDoor.netId == door.netId, out var value))
		{
			value.Value.UpdateName(newTag);
		}
		else
		{
			door.gameObject.AddComponent<DoorNametagExtension>().UpdateName(newTag);
		}
	}

	public static RoomIdentifier Room(this DoorVariant door)
	{
		return RoomIdUtils.RoomAtPosition(door.transform.position);
	}

	public static RoomName RoomId(this DoorVariant door)
	{
		return door.Room()?.Name ?? RoomName.Unnamed;
	}

	public static FacilityZone Zone(this DoorVariant door)
	{
		return door.Room()?.Zone ?? FacilityZone.None;
	}

	public static ReferenceHub[] PlayersInRadius(this DoorVariant door, float radius, FacilityZone[] zoneFilter = null, RoomName[] roomFilter = null)
	{
		return Hub.InRadius(door.transform.position, radius, zoneFilter, roomFilter);
	}

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	private static void OnRoundRestart()
	{
		_customAccessModifiers.Clear();
		_customPerms.Clear();
		_disabled.Clear();
		_plyWhitelist.Clear();
		_plyBlacklist.Clear();
	}

	[Patch(typeof(DoorVariant), "ServerInteract", PatchType.Prefix, new Type[] { })]
	private static bool DoorInteractionPatch(DoorVariant __instance, ReferenceHub ply, byte colliderId)
	{
		if (__instance.HasCustomAccessModifier(out var modifier))
		{
			if (modifier(__instance, ply))
			{
				if (__instance.AllowInteracting(ply, colliderId))
				{
					__instance.Toggle();
					__instance._triggerPlayer = ply;
				}
				return false;
			}
			if (__instance.AllowInteracting(ply, colliderId))
			{
				__instance.PermissionsDenied(ply, colliderId);
				DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
			}
			return false;
		}
		if (__instance.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
		{
			DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)__instance.ActiveLocks);
			if ((!mode.HasFlagFast(DoorLockMode.CanClose) || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP()) && (mode == DoorLockMode.FullLock || (__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) || (!__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
			{
				if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, __instance, canOpen: false)))
				{
					return false;
				}
				__instance.LockBypassDenied(ply, colliderId);
				return false;
			}
		}
		if (!__instance.AllowInteracting(ply, colliderId) || __instance.IsDisabled())
		{
			return false;
		}
		if (__instance.RequiresWhitelist() && !__instance.IsWhitelisted(ply))
		{
			return false;
		}
		if (__instance.IsBlacklisted(ply))
		{
			return false;
		}
		bool flag = true;
		flag = ((!__instance.HasCustomPermissions(out var customPermissions)) ? (ply.GetRoleId() == RoleTypeId.Scp079 || __instance.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply)) : (ply.GetRoleId() == RoleTypeId.Scp079 || customPermissions == KeycardPermissions.None || (customPermissions == KeycardPermissions.ScpOverride && ply.IsSCP()) || (ply.inventory.CurInstance != null && ply.inventory.CurInstance is KeycardItem keycardItem && keycardItem.Permissions.HasFlagFast(customPermissions))));
		if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, __instance, flag)))
		{
			return false;
		}
		if (flag)
		{
			__instance.Toggle();
			__instance._triggerPlayer = ply;
			return false;
		}
		__instance.PermissionsDenied(ply, colliderId);
		DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
		return false;
	}
}
