using System;
using helpers.Extensions;
using PlayerRoles;
using UnityEngine;

namespace Compendium;

public static class HubRoleExtensions
{
	/*
	public static byte UnitId(this ReferenceHub hub)
	{
		if (!UnitHelper.TryGetUnitId(hub, out var unitId))
		{
			return 0;
		}
		return unitId;
	}

	public static string UnitName(this ReferenceHub hub)
	{
		if (!UnitHelper.TryGetUnitName(hub, out var unitName))
		{
			return null;
		}
		return unitName;
	}

	public static void SetUnitId(this ReferenceHub hub, byte id)
	{
		UnitHelper.TrySetUnitId(hub, id);
	}

	public static void SetUnitName(this ReferenceHub hub, string name)
	{
		UnitHelper.TrySetUnitName(hub, name);
	}

	public static void SyncUnit(this ReferenceHub hub, ReferenceHub other)
	{
		if (UnitHelper.TryGetUnitId(other, out var unitId))
		{
			hub.SetUnitId(unitId);
		}
	}
	*/

	public static string RoleName(this ReferenceHub hub)
	{
		if (!string.IsNullOrWhiteSpace(hub.Role()?.RoleName))
		{
			return hub.Role().RoleName;
		}
		return hub.GetRoleId().ToString().SpaceByPascalCase();
	}

	public static RoleTypeId RoleId(this ReferenceHub hub, RoleTypeId? newRole = null, RoleSpawnFlags flags = RoleSpawnFlags.All)
	{
		if (newRole.HasValue)
		{
			hub.roleManager.ServerSetRole(newRole.Value, RoleChangeReason.RemoteAdmin, flags);
			return newRole.Value;
		}
		return hub.GetRoleId();
	}

	public static PlayerRoleBase Role(this ReferenceHub hub, PlayerRoleBase newRole = null)
	{
		if ((UnityEngine.Object)(object)newRole != null)
		{
			hub.roleManager.ServerSetRole(newRole.RoleTypeId, newRole.ServerSpawnReason, newRole.ServerSpawnFlags);
		}
		return hub.roleManager.CurrentRole;
	}

	public static string GetRoleColorHex(this ReferenceHub hub)
	{
		return hub.RoleId().GetRoleColorHex();
	}

	public static string GetRoleColorHexPrefixed(this ReferenceHub hub)
	{
		return hub.RoleId().GetRoleColorHexPrefixed();
	}

	public static string GetRoleColorHexPrefixed(this RoleTypeId role)
	{
		try
		{
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out var result))
			{
				return "#90FF33";
			}
			return result.RoleColor.ToHex();
		}
		catch (Exception message)
		{
			Plugin.Error(message);
			return "#90FF33";
		}
	}

	public static string GetRoleColorHex(this RoleTypeId role)
	{
		try
		{
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out var result))
			{
				return "#90FF33".Remove("#");
			}
			return result.RoleColor.ToHex().Remove("#");
		}
		catch (Exception message)
		{
			Plugin.Error(message);
			return "#90FF33".Remove("#");
		}
	}
}
