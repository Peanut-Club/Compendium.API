using System;
using System.Linq;
using System.Text;
using BetterCommands;
using BetterCommands.Permissions;
using Compendium.Custom.Parsers.PlayerList;
using Compendium.Mutes;
using Compendium.PlayerData;
using Compendium.Positions;
using helpers;
using helpers.Extensions;
using helpers.Time;
using PluginAPI.Core;
using UnityEngine;

namespace Compendium.Custom.Commands;

public static class ModerationCommands
{
	[Command("postp", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Teleports a player to a predefined position.")]
	[Permission(PermissionLevel.Lowest)]
	public static string PosTpCommand(ReferenceHub sender, ReferenceHub target, string position)
	{
		for (int i = 0; i < PositionHelper.Positions.Count; i++)
		{
			if (PositionHelper.Positions[i].Name.GetSimilarity(position) >= 0.9)
			{
				target.Position(PositionHelper.Positions[i].GetPosition());
				return "Teleported " + target.Nick() + " to position: " + PositionHelper.Positions[i].Name;
			}
		}
		return "That position doesn't exist. Use listpos to view all positions.";
	}

	[Command("listpos", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Lists all predefined positions.")]
	public static string ListPosCommand(ReferenceHub sender)
	{
		if (PositionHelper.Positions.Count <= 0)
		{
			return "There aren't any positions. You need to create some using the addpos command.";
		}
		StringBuilder stringBuilder = Pools.PoolStringBuilder();
		stringBuilder.AppendLines(PositionHelper.Positions, (Position pos) => $"{pos.Name} ({pos.Description}) {pos.GetPosition()}");
		return stringBuilder.ReturnStringBuilderValue();
	}

	[Command("addpos", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Adds a position.")]
	[Permission(PermissionLevel.Lowest)]
	public static string AddPosCommand(ReferenceHub sender, string name, string description = "Výchozí popis.")
	{
		if (PositionHelper.Positions.Any((Position p) => p.Name.GetSimilarity(name) >= 0.9))
		{
			return "There already is a position with a similar name. Use delpos to remove it.";
		}
		Vector3 vector = sender.Position();
		PositionHelper.Positions.Add(new Position
		{
			Name = name,
			Description = description,
			X = vector.x,
			Y = vector.y + 1f,
			Z = vector.z
		});
		PositionHelper.Config?.Save();
		return "Position saved.";
	}

	[Command("delpos", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Deletes a predefined position.")]
	[Permission(PermissionLevel.Lowest)]
	public static string DelPosCommand(ReferenceHub sender, string name)
	{
		if (PositionHelper.Positions.RemoveAll((Position p) => p.Name.GetSimilarity(name) >= 0.9) > 0)
		{
			PositionHelper.Config?.Save();
			return "Position deleted.";
		}
		return "Failed to find/remove position.";
	}

	[Command("oban", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Issues an offline ban.")]
	[Permission(PermissionLevel.Lowest)]
	public static string OfflineBanCommand(ReferenceHub sender, string target, string duration, string reason, bool banIp = true, bool banId = true)
	{
		if (!TimeUtils.TryParseTime(duration, out var result))
		{
			return "Failed to parse ban duration! Correct formatting is 'xY', where 'x' is the amount and 'Y' is the time unit:\nSupported units:\n- s - seconds\n- m - minutes\n- h - hours\n- d - days\n- M - months\n- y - years\n\nExample: 5s (5 seconds)\nExample: 10h (10 hours)\n\nThese can be combined: 10h 5m (10 hours and 5 minutes)";
		}
		BanHelper.OfflineBanResult offlineBanResult = BanHelper.TryOfflineBan(target, reason, DateTime.Now + result, sender, banIp, banId);
		if (offlineBanResult == BanHelper.OfflineBanResult.Failed || offlineBanResult == BanHelper.OfflineBanResult.None)
		{
			return "Offline ban failed - unknown reason.";
		}
		string text = "Offline ban succeeded!\n";
		if ((offlineBanResult & BanHelper.OfflineBanResult.IdBanned) != 0)
		{
			text += "- ID ban issued\n";
		}
		if ((offlineBanResult & BanHelper.OfflineBanResult.IpBanned) != 0)
		{
			text += "- IP ban issued";
		}
		return text;
	}

	[Command("unban", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Unbans a player.")]
	[Permission(PermissionLevel.Lowest)]
	public static string UnbanCommand(ReferenceHub sender, string target, bool unbanIp = true, bool unbanId = true)
	{
		BanHelper.UnbanResult unbanResult = BanHelper.TryUnban(target, unbanIp, unbanId);
		if (unbanResult == BanHelper.UnbanResult.Failed || unbanResult == BanHelper.UnbanResult.None)
		{
			return "Unban failed - unknown reason.";
		}
		string text = "Unban succeeded!\n";
		if ((unbanResult & BanHelper.UnbanResult.IpRemoved) != 0)
		{
			text += "- IP ban removed\n";
		}
		if ((unbanResult & BanHelper.UnbanResult.IdRemoved) != 0)
		{
			text += "- ID ban removed\n";
		}
		if ((unbanResult & BanHelper.UnbanResult.IdNotBanned) != 0)
		{
			text += "- ID was not banned\n";
		}
		if ((unbanResult & BanHelper.UnbanResult.IpNotBanned) != 0)
		{
			text += "- IP was not banned";
		}
		return text;
	}

	[Command("tmute", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Issues a temporary mute.")]
	[Permission(PermissionLevel.Lowest)]
	public static string TemporaryMuteCommand(ReferenceHub sender, ReferenceHub target, string duration, string reason)
	{
		if (!TimeUtils.TryParseTime(duration, out var result))
		{
			return "Failed to parse mute duration! Correct formatting is 'xY', where 'x' is the amount and 'Y' is the time unit:\nSupported units:\n- s - seconds\n- m - minutes\n- h - hours\n- d - days\n- M - months\n- y - years\n\nExample: 5s (5 seconds)\nExample: 10h (10 hours)\n\nThese can be combined: 10h 5m (10 hours and 5 minutes)";
		}
		if (!MuteManager.Issue(sender, target, reason, result))
		{
			return "Failed to issue temporary mute.";
		}
		return "Issued a temporary mute to '" + target.Nick() + "' for '" + reason + "' (expires in " + result.UserFriendlySpan() + ")";
	}

	/*
	[Command("querybans", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Lists all bans for a player.")]
	[Permission(PermissionLevel.Lowest)]
	public static string QueryActiveBansCommand(ReferenceHub sender, string target, bool includeHistory = true)
	{
		BanHelper.BanInfo[] array = (from b in BanHelper.QueryBans(target, includeHistory)
			orderby b.ExpiresAt
			select b).ToArray();
		if (array.Length == 0)
		{
			return "No bans were found for '" + target + "'";
		}
		string text = $"All bans for player '{array[0].TargetName}' ({array.Length}):\n";
		for (int i = 0; i < array.Length; i++)
		{
			text = ((!(DateTime.Now < array[i].ExpiresAt)) ? (text + "[" + array[i].Type.ToString().ToUpper() + " - " + array[i].TargetId + "]: " + array[i].Reason + " - by: " + (array[i].IsParsed ? (array[i].IssuerName + " (" + array[i].IssuerId + ")") : (array[i].IssuerName ?? array[i].IssuerId)) + ", (EXPIRED)\n") : (text + "[" + array[i].Type.ToString().ToUpper() + " - " + array[i].TargetId + "]: " + array[i].Reason + " - by: " + (array[i].IsParsed ? (array[i].IssuerName + " (" + array[i].IssuerId + ")") : (array[i].IssuerName ?? array[i].IssuerId)) + ", expires at: " + array[i].ExpiresAt.ToString("G") + " (" + TimeSpan.FromMilliseconds((array[i].ExpiresAt - DateTime.Now).TotalMilliseconds).UserFriendlySpan() + " left)\n"));
		}
		return text;
	}
	*/

	[Command("tomute", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Issues a temporary offline mute.")]
	[Permission(PermissionLevel.Lowest)]
	public static string TemporaryOfflineMuteCommand(ReferenceHub sender, PlayerDataRecord target, string duration, string reason)
	{
		if (!TimeUtils.TryParseTime(duration, out var result))
		{
			return "Failed to parse mute duration! Correct formatting is 'xY', where 'x' is the amount and 'Y' is the time unit:\nSupported units:\n- s - seconds\n- m - minutes\n- h - hours\n- d - days\n- M - months\n- y - years\n\nExample: 5s (5 seconds)\nExample: 10h (10 hours)\n\nThese can be combined: 10h 5m (10 hours and 5 minutes)";
		}
		if (!MuteManager.Issue(sender, target, reason, result))
		{
			return "Failed to issue temporary mute.";
		}
		return "Issued a temporary mute to '" + target.NameTracking.LastValue + "' for '" + reason + "' (expires in " + result.UserFriendlySpan() + ")";
	}

	[Command("mutes", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Displays a list of active temporary mutes.")]
	[Permission(PermissionLevel.Lowest)]
	public static string MutesCommand(ReferenceHub sender, PlayerDataRecord target)
	{
		Mute[] array = MuteManager.Query(target);
		if (array.Length == 0)
		{
			return target.NameTracking.LastValue + " does not have any active temporary mutes.";
		}
		PlayerDataRecord record;
		return $"Active mutes ({array.Length}):\n" + string.Join("\n", array.Select((Mute m) => "[" + m.Id + "]: Issued by " + ((PlayerDataRecorder.TryQuery(m.IssuerId, queryNick: false, out record) && record.NameTracking.LastValue != null) ? (record.NameTracking.LastValue + " (" + record.UserId + ")") : (m.IssuerId ?? "")) + " for '" + m.Reason + "' (expires at: " + new DateTime(m.ExpiresAt).ToString("G") + ")"));
	}

	[Command("rmute", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Removes a mute using it's ID.")]
	[Permission(PermissionLevel.Lowest)]
	public static string RemoveMuteCommand(ReferenceHub sender, string muteId)
	{
		Mute mute = MuteManager.Query(muteId);
		if (mute == null)
		{
			return "Failed to find a mute with that ID";
		}
		if (!MuteManager.Remove(mute))
		{
			return "Failed to remove that mute.";
		}
		return "Mute removed.";
	}

	[Command("rmutes", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Removes all mutes for a specified player.")]
	[Permission(PermissionLevel.Lowest)]
	public static string RemoveMutesCommand(ReferenceHub sender, PlayerDataRecord target)
	{
		Mute[] array = MuteManager.Query(target);
		if (array.Length == 0)
		{
			return "Player '" + target.NameTracking.LastValue + "' doesn't have any active mutes.";
		}
		for (int i = 0; i < array.Length; i++)
		{
			MuteManager.Remove(array[i]);
		}
		return $"Removed {array.Length} mute(s).";
	}

	[Command("redirect", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Redirects the selected players on to another server.")]
	[Permission(PermissionLevel.Administrator)]
	public static string RedirectCommand(ReferenceHub sender, PlayerListData players, ushort port)
	{
		for (int i = 0; i < players.Count; i++)
		{
			Player.Get(players.Matched[i])?.RedirectToServer(port);
		}
		return $"Redirected all selected players to {port}";
	}
}
