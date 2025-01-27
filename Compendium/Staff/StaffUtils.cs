using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Staff;

public static class StaffUtils
{
	public static IReadOnlyList<PlayerPermissions> Permissions { get; } = Enum.GetValues(typeof(PlayerPermissions)).Cast<PlayerPermissions>().ToList();


	public static PlayerPermissions ToNwPermissions(StaffGroup group)
	{
		IReadOnlyList<PlayerPermissions> permissions = Permissions;
		PlayerPermissions playerPermissions = (PlayerPermissions)0uL;
		foreach (PlayerPermissions item in permissions)
		{
			if (HasPermission(group, item))
			{
				playerPermissions |= item;
			}
		}
		return playerPermissions;
	}

	public static bool HasPermission(StaffGroup group, PlayerPermissions playerPermissions)
	{
		if (group.Permissions.Contains(StaffPermissions.Override))
		{
			return true;
		}
		switch (playerPermissions)
		{
		case (PlayerPermissions)0uL:
		case PlayerPermissions.KickingAndShortTermBanning:
		case PlayerPermissions.BanningUpToDay:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay:
		case PlayerPermissions.LongTermBanning:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.LongTermBanning:
		case PlayerPermissions.BanningUpToDay | PlayerPermissions.LongTermBanning:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay | PlayerPermissions.LongTermBanning:
		case PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.BanningUpToDay | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.LongTermBanning | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.LongTermBanning | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.BanningUpToDay | PlayerPermissions.LongTermBanning | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay | PlayerPermissions.LongTermBanning | PlayerPermissions.ForceclassSelf:
		case PlayerPermissions.ForceclassToSpectator:
		{
			PlayerPermissions playerPermissions2 = playerPermissions - 1;
			if (playerPermissions2 <= (PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay))
			{
				PlayerPermissions playerPermissions3 = playerPermissions2;
				PlayerPermissions playerPermissions4 = playerPermissions3;
				if (playerPermissions4 <= (PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay))
				{
					switch (playerPermissions4)
					{
					case PlayerPermissions.KickingAndShortTermBanning:
						return group.Permissions.Contains(StaffPermissions.DayBans);
					case (PlayerPermissions)0uL:
						return group.Permissions.Contains(StaffPermissions.ShortBans);
					case PlayerPermissions.KickingAndShortTermBanning | PlayerPermissions.BanningUpToDay:
						return group.Permissions.Contains(StaffPermissions.LongBans);
					case PlayerPermissions.BanningUpToDay:
						goto end_IL_0025;
					}
				}
			}
			switch (playerPermissions)
			{
			case PlayerPermissions.ForceclassSelf:
				return group.Permissions.Contains(StaffPermissions.ForceclassSelf);
			case PlayerPermissions.ForceclassToSpectator:
				return group.Permissions.Contains(StaffPermissions.ForceclassSpectator);
			}
			break;
		}
		case PlayerPermissions.Noclip:
			return group.GroupFlags.Contains(StaffGroupFlags.IsNoClip);
		case PlayerPermissions.AFKImmunity:
			return group.GroupFlags.Contains(StaffGroupFlags.IsAfkImmune);
		case PlayerPermissions.SetGroup:
			return group.Permissions.Contains(StaffPermissions.ServerConfigs);
		case PlayerPermissions.AdminChat:
			return group.GroupFlags.Contains(StaffGroupFlags.IsAdminChat);
		case PlayerPermissions.ServerConfigs:
			return group.Permissions.Contains(StaffPermissions.ServerConfigs);
		case PlayerPermissions.Announcer:
			return group.Permissions.Contains(StaffPermissions.CassieAccess);
		case PlayerPermissions.Broadcasting:
			return group.Permissions.Contains(StaffPermissions.BroadcastAccess);
		case PlayerPermissions.Effects:
			return group.Permissions.Contains(StaffPermissions.PlayerManagement);
		case PlayerPermissions.FacilityManagement:
			return group.Permissions.Contains(StaffPermissions.MapManagement);
		case PlayerPermissions.ForceclassWithoutRestrictions:
			return group.Permissions.Contains(StaffPermissions.ForceclassOthers);
		case PlayerPermissions.FriendlyFireDetectorImmunity:
			return group.GroupFlags.Contains(StaffGroupFlags.IsFriendlyFireImmune);
		case PlayerPermissions.FriendlyFireDetectorTempDisable:
			return group.Permissions.Contains(StaffPermissions.ServerConfigs);
		case PlayerPermissions.GameplayData:
			return group.Permissions.Contains(StaffPermissions.GameplayData);
		case PlayerPermissions.GivingItems:
			return group.Permissions.Contains(StaffPermissions.InventoryManagement);
		case PlayerPermissions.Overwatch:
			return group.Permissions.Contains(StaffPermissions.PlayerManagement);
		case PlayerPermissions.PermissionsManagement:
        case PlayerPermissions.ServerLogLiveFeed:
            return group.Permissions.Contains(StaffPermissions.ServerConfigs);
		case PlayerPermissions.ServerConsoleCommands:
			return group.Permissions.Contains(StaffPermissions.ServerCommands);
		case PlayerPermissions.ViewHiddenBadges:
			return group.GroupFlags.Contains(StaffGroupFlags.CanViewHiddenBadges);
		case PlayerPermissions.ViewHiddenGlobalBadges:
			return group.GroupFlags.Contains(StaffGroupFlags.CanViewHiddenGlobalBadges);
		case PlayerPermissions.WarheadEvents:
		case PlayerPermissions.RespawnEvents:
			return group.Permissions.Contains(StaffPermissions.MapManagement);
		case PlayerPermissions.RoundEvents:
			return group.Permissions.Contains(StaffPermissions.RoundManagement);
		case PlayerPermissions.PlayerSensitiveDataAccess:
			return group.Permissions.Contains(StaffPermissions.PlayerData);
		case PlayerPermissions.PlayersManagement:
			{
				return group.Permissions.Contains(StaffPermissions.PlayerManagement);
			}
			end_IL_0025:
			break;
		}
		throw new Exception($"Unrecognized permissions node: {playerPermissions}");
	}

	public static string GetColor(StaffColor color)
	{
		if (1 == 0)
		{
		}
		string result = color switch
		{
			StaffColor.ArmyGreen => "army_green", 
			StaffColor.BlueGreen => "blue_green", 
			StaffColor.DeepPink => "deep_pink", 
			StaffColor.LightGreen => "light_green", 
			_ => color.ToString().ToLowerInvariant(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
