using System;
using System.Collections.Generic;
using System.Linq;
using CentralAuth;
using Compendium.Comparison;
using Compendium.Extensions;
using Compendium.PlayerData;
using helpers;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace Compendium;

public static class Hub
{
	public static IReadOnlyList<ReferenceHub> Hubs => ReferenceHub.AllHubs.Where((ReferenceHub hub) => hub.IsPlayer()).ToList();

	public static int Count => Hubs.Count;

	public static IEnumerable<ReferenceHub> GetHubs(Faction faction)
	{
		return Hubs.Where((ReferenceHub h) => h.GetFaction() == faction);
	}

	public static IEnumerable<ReferenceHub> GetHubs(Team team)
	{
		return Hubs.Where((ReferenceHub h) => h.GetTeam() == team);
	}

	public static IEnumerable<ReferenceHub> GetHubs(RoleTypeId role)
	{
		return Hubs.Where((ReferenceHub h) => h.RoleId() == role);
	}

	public static IEnumerable<ReferenceHub> GetHubs(FacilityZone zone)
	{
		return Hubs.Where((ReferenceHub h) => h.Zone() == zone);
	}

	public static IEnumerable<ReferenceHub> GetHubs(RoomName room)
	{
		return Hubs.Where((ReferenceHub h) => h.RoomId() == room);
	}

	public static ReferenceHub GetHub(this PlayerDataRecord record, bool supplyServer = true)
	{
		if (!record.TryGetHub(out var hub))
		{
			if (!supplyServer)
			{
				return null;
			}
			return ReferenceHub.HostHub;
		}
		return hub;
	}

	public static ReferenceHub GetHub(uint netId)
	{
		if (!Hubs.TryGetFirst((ReferenceHub x) => x.netId == netId, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool TryGetHub(this PlayerDataRecord record, out ReferenceHub hub)
	{
		return Hubs.TryGetFirst((ReferenceHub h) => record.UserId == h.UserId() || record.Ip == h.Ip() || record.Id == h.UniqueId(), out hub);
	}

	public static void TryInvokeHub(this PlayerDataRecord record, Action<ReferenceHub> target)
	{
		if (record != null && record.TryGetHub(out var hub) && hub != null)
		{
			Calls.Delegate(target, hub);
		}
	}

	public static bool TryGetHub(string userId, out ReferenceHub hub)
	{
		return Hubs.TryGetFirst((ReferenceHub x) => UserIdComparison.Compare(userId, x.UserId()), out hub);
	}

	public static ReferenceHub[] InRadius(Vector3 position, float radius, FacilityZone[] zoneFilter = null, RoomName[] roomFilter = null)
	{
		List<ReferenceHub> list = new List<ReferenceHub>();
		ForEach(delegate(ReferenceHub hub)
		{
			RoomIdentifier roomIdentifier = hub.Room();
			if ((zoneFilter == null || !zoneFilter.Any() || ((object)roomIdentifier != null && zoneFilter.Contains(roomIdentifier.Zone))) && (roomFilter == null || !roomFilter.Any() || ((object)roomIdentifier != null && roomFilter.Contains(roomIdentifier.Name))) && hub.Position().IsWithinDistance(position, radius))
			{
				list.Add(hub);
			}
		});
		return list.ToArray();
	}

	public static void ForEach(this Action<ReferenceHub> action, Predicate<ReferenceHub> predicate = null)
	{
		ReferenceHub.AllHubs.ForEach(delegate(ReferenceHub hub)
		{
			if (hub.Mode == ClientInstanceMode.ReadyClient && (predicate == null || predicate(hub)))
			{
				action(hub);
			}
		});
	}

	public static void ForEach(this Action<ReferenceHub> action, params RoleTypeId[] roleFilter)
	{
		action.ForEach((ReferenceHub hub) => roleFilter.Length == 0 || roleFilter.Contains(hub.GetRoleId()));
	}

	public static void ForEachZone(this Action<ReferenceHub> action, bool includeUnknown, params FacilityZone[] zoneFilter)
	{
		action.ForEach(delegate(ReferenceHub hub)
		{
			FacilityZone facilityZone = hub.Zone();
			return (facilityZone == FacilityZone.None && includeUnknown) || zoneFilter.Contains(facilityZone);
		});
	}

	public static void ForEachRoom(this Action<ReferenceHub> action, bool includeUnknown, params RoomName[] roomFilter)
	{
		action.ForEach(delegate(ReferenceHub hub)
		{
			RoomName roomName = hub.RoomId();
			return (roomName == RoomName.Unnamed && includeUnknown) || roomFilter.Contains(roomName);
		});
	}
}
