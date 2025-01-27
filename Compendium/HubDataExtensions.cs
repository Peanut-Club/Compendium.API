using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CentralAuth;
using Compendium.PlayerData;
using Compendium.Staff;
using helpers;
using helpers.Extensions;
using PlayerRoles;
using PluginAPI.Core;

namespace Compendium;

public static class HubDataExtensions
{
	public static string Nick(this ReferenceHub hub, string newNick = null)
	{
		if (string.IsNullOrWhiteSpace(newNick))
		{
			return hub.nicknameSync.Network_myNickSync;
		}
		hub.nicknameSync.SetNick(newNick);
		return newNick;
	}

	public static string DisplayNick(this ReferenceHub hub, string newDisplayNick = null)
	{
		if (string.IsNullOrWhiteSpace(newDisplayNick))
		{
			return hub.nicknameSync._cleanDisplayName;
		}
		hub.nicknameSync.Network_displayName = newDisplayNick;
		return newDisplayNick;
	}

	public static void ResetDisplayNick(this ReferenceHub hub)
	{
		hub.nicknameSync.Network_displayName = null;
	}

	public static bool IsPlayer(this ReferenceHub hub)
	{
		return hub.Mode == ClientInstanceMode.ReadyClient;
	}

	public static bool IsServer(this ReferenceHub hub)
	{
		if (hub.Mode != ClientInstanceMode.DedicatedServer)
		{
			return hub.Mode == ClientInstanceMode.Host;
		}
		return true;
	}

	public static bool IsVerified(this ReferenceHub hub)
	{
		return hub.Mode != ClientInstanceMode.Unverified;
	}

	public static bool IsNorthwoodStaff(this ReferenceHub hub)
	{
		return hub.authManager.NorthwoodStaff;
	}

	public static bool IsNorthwoodModerator(this ReferenceHub hub)
	{
		return hub.authManager.RemoteAdminGlobalAccess;
	}

	public static bool IsStaff(this ReferenceHub hub, bool countNwStaff = true)
	{
		if (hub.IsNorthwoodStaff())
		{
			return countNwStaff;
		}
		if (Plugin.Config.ApiSetttings.ConsiderRemoteAdminAccessAsStaff && hub.serverRoles.RemoteAdmin)
		{
			return true;
		}
		StaffGroup value2;
		if (StaffHandler.Members.TryGetValue(hub.UserId(), out var value3))
		{
			return value3.Any((string g) => StaffHandler.Groups.TryGetValue(g, out value2) && value2.GroupFlags.Contains(StaffGroupFlags.IsStaff));
		}
		return false;
	}

	public static string UserId(this ReferenceHub hub)
	{
		return hub.authManager.UserId;
	}

	public static string UniqueId(this ReferenceHub hub)
	{
		return PlayerDataRecorder.GetData(hub)?.Id ?? "";
	}

	public static UserIdValue ParsedUserId(this ReferenceHub hub)
	{
		if (!UserIdValue.TryParse(hub.UserId(), out var value))
		{
			throw new Exception("Failed to parse user ID of " + hub.UserId());
		}
		return value;
	}

	public static string Ip(this ReferenceHub hub)
	{
		return hub.connectionToClient.address;
	}

	public static bool HasReservedSlot(this ReferenceHub hub, bool countBypass = false)
	{
		if (!ReservedSlot.HasReservedSlot(hub.UserId(), out var bypass))
		{
			return countBypass && bypass;
		}
		return true;
	}

	public static void AddReservedSlot(this ReferenceHub hub, bool isTemporary, bool addNick = false)
	{
		if (hub.HasReservedSlot())
		{
			return;
		}
		Plugin.Debug("Added temporary reserved slot to " + hub.GetLogName(includeIp: true, includeRole: false));
		ReservedSlot.Users.Add(hub.UserId());
		if (!isTemporary)
		{
			List<string> list = new List<string>();
			if (addNick)
			{
				list.Add("# Player: " + hub.Nick() + " (" + hub.Ip() + ")");
			}
			list.Add(hub.UserId() ?? "");
			File.AppendAllLines(ReservedSlots.FilePath, list);
			ReservedSlot.Reload();
			Plugin.Debug("Added permanent reserved slot to " + hub.GetLogName(includeIp: true, includeRole: false));
		}
	}

	public static void RemoveReservedSlot(this ReferenceHub hub)
	{
		if (hub.HasReservedSlot())
		{
			string[] array = File.ReadAllLines(ReservedSlots.FilePath);
			int num = array.FindIndex((string l) => l.Contains(hub.UserId()));
			if (num != -1)
			{
				array[num] = "# Removed by Compendium's API: " + array[num];
				File.WriteAllLines(ReservedSlots.FilePath, array);
			}
			ReservedSlot.Reload();
			Plugin.Debug("Removed reserved slot for " + hub.GetLogName(includeIp: true, includeRole: false));
		}
	}

	public static int PlyId(this ReferenceHub hub, int? playerId)
	{
		if (playerId.HasValue)
		{
			hub.Network_playerId = new RecyclablePlayerId(playerId.Value);
			return playerId.Value;
		}
		return hub.Network_playerId.Value;
	}

	public static uint NetId(this ReferenceHub hub)
	{
		return hub.netId;
	}

	public static int ObjectId(this ReferenceHub hub)
	{
		return hub.GetInstanceID();
	}

	public static string GetLogName(this ReferenceHub hub, bool includeIp = false, bool includeRole = true)
	{
		return string.Format("[{0}]{1}{2} {3}{4}", hub.PlayerId, includeRole ? (" " + hub.GetRoleId().ToString().SpaceByPascalCase() + " ") : "  ", hub.Nick(), hub.UserId(), includeIp ? (" " + hub.Ip()) : "");
	}
}
