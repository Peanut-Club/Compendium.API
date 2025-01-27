using System.Collections.Generic;
using System.Linq;
using CentralAuth;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using Respawning;
using Respawning.NamingRules;
using UnityEngine;

namespace Compendium;

public static class UnitHelper
{
	/*
	public const SpawnableTeamType Ntf = SpawnableTeamType.NineTailedFox;

	public static IReadOnlyList<string> NtfUnits
	{
		get
		{
			if (UnitNameMessageHandler.ReceivedNames.TryGetValue(SpawnableTeamType.NineTailedFox, out var value))
			{
				return value;
			}
			return null;
		}
	}

	public static bool TryCreateUnit(string unit)
	{
		if (UnitNameMessageHandler.ReceivedNames.TryGetValue(SpawnableTeamType.NineTailedFox, out var value) && UnitNamingRule.AllNamingRules.TryGetValue(SpawnableTeamType.NineTailedFox, out var value2))
		{
			if (!value.Contains(unit))
			{
				value.Add(unit);
				foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
				{
					if (allHub.Mode == ClientInstanceMode.ReadyClient)
					{
						allHub.connectionToClient.Send(new UnitNameMessage
						{
							Team = SpawnableTeamType.NineTailedFox,
							NamingRule = value2,
							UnitName = unit
						});
					}
				}
			}
			return false;
		}
		return false;
	}

	public static bool TryGetUnitId(ReferenceHub hub, out byte unitId)
	{
		unitId = 0;
		if ((object)hub == null)
		{
			return false;
		}
		if ((object)hub.roleManager == null)
		{
			return false;
		}
		if ((Object)(object)hub.roleManager.CurrentRole == null)
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is HumanRole humanRole) || (Object)(object)humanRole == null)
		{
			return false;
		}
		if (!humanRole.UsesUnitNames)
		{
			return false;
		}
		unitId = humanRole.UnitNameId;
		return true;
	}

	public static bool TryGetUnitName(ReferenceHub hub, out string unitName)
	{
		unitName = null;
		if ((object)hub == null)
		{
			return false;
		}
		if ((object)hub.roleManager == null)
		{
			return false;
		}
		if ((Object)(object)hub.roleManager.CurrentRole == null)
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is HumanRole humanRole) || (Object)(object)humanRole == null)
		{
			return false;
		}
		if (!humanRole.UsesUnitNames)
		{
			return false;
		}
		unitName = UnitNameMessageHandler.ReceivedNames[humanRole.AssignedSpawnableTeam][humanRole.UnitNameId];
		return !string.IsNullOrWhiteSpace(unitName);
	}

	public static bool TrySetUnitId(ReferenceHub hub, byte unitId)
	{
		if ((object)hub == null)
		{
			return false;
		}
		if ((object)hub.roleManager == null)
		{
			return false;
		}
		if ((Object)(object)hub.roleManager.CurrentRole == null)
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is HumanRole humanRole) || (Object)(object)humanRole == null)
		{
			return false;
		}
		if (!humanRole.UsesUnitNames)
		{
			return false;
		}
		humanRole.UnitNameId = unitId;
		SynchronizeUnitIdChange(hub, humanRole);
		return true;
	}

	public static bool TrySetUnitName(ReferenceHub hub, string unitName, bool addIfMissing = false)
	{
		if ((object)hub == null)
		{
			return false;
		}
		if ((object)hub.roleManager == null)
		{
			return false;
		}
		if ((Object)(object)hub.roleManager.CurrentRole == null)
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is HumanRole humanRole) || (Object)(object)humanRole == null)
		{
			return false;
		}
		if (!humanRole.UsesUnitNames)
		{
			return false;
		}
		List<string> list = NtfUnits?.ToList() ?? null;
		if (list == null)
		{
			return false;
		}
		int num = list.IndexOf(unitName);
		if (num == -1)
		{
			if (!addIfMissing)
			{
				return false;
			}
			if (!TryCreateUnit(unitName))
			{
				return false;
			}
			list = NtfUnits.ToList();
			num = list.IndexOf(unitName);
			if (num == -1)
			{
				return false;
			}
			return TrySetUnitId(hub, (byte)num);
		}
		return TrySetUnitId(hub, (byte)num);
	}

	public static bool TrySynchronizeUnits(ReferenceHub target, ReferenceHub source)
	{
		if (!TryGetUnitId(source, out var unitId))
		{
			return false;
		}
		return TrySetUnitId(target, unitId);
	}

	private static void SynchronizeUnitIdChange(ReferenceHub target, HumanRole role)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteUShort(38952);
		networkWriterPooled.WriteUInt(target.netId);
		networkWriterPooled.WriteRoleType(target.GetRoleId());
		networkWriterPooled.WriteByte(role.UnitNameId);
		if (target.GetRoleId() != RoleTypeId.Spectator && target.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			fpcRole.FpcModule.MouseLook.GetSyncValues(0, out var syncH, out var _);
			networkWriterPooled.WriteRelativePosition(new RelativePosition(target.transform.position));
			networkWriterPooled.WriteUShort(syncH);
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.Mode == ClientInstanceMode.ReadyClient)
			{
				allHub.connectionToClient.Send(networkWriterPooled.ToArraySegment());
			}
		}
		NetworkWriterPool.Return(networkWriterPooled);
	}
	*/
}
