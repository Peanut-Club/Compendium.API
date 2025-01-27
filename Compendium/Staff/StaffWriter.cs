using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Compendium.PlayerData;
using helpers;

namespace Compendium.Staff;

public static class StaffWriter
{
	internal static string MembersBuffer;

	internal static string GroupsBuffer;

	public static void WriteMembers(Dictionary<string, string[]> membersDict)
	{
		MembersBuffer = null;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# syntax: ID: groupKey1,groupKey2,groupKey3");
		stringBuilder.AppendLine("# example: 776561198456564: owner,developer");
		stringBuilder.AppendLine();
		foreach (KeyValuePair<string, string[]> item in membersDict)
		{
			if (PlayerDataRecorder.TryQuery(item.Key, queryNick: false, out var record))
			{
				stringBuilder.AppendLine("# User: " + record.NameTracking.LastValue + " (" + record.UserId + "; " + record.Ip + ")");
			}
			stringBuilder.AppendLine(item.Key + ": " + string.Join(",", item.Value));
		}
		MembersBuffer = stringBuilder.ToString();
	}

	public static void WriteGroups(Dictionary<string, StaffGroup> groupsDict)
	{
		Dictionary<StaffPermissions, List<string>> permsDict = new Dictionary<StaffPermissions, List<string>>();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("# group syntax: groupKey=text;color;kickPower;requiredKickPower;badgeFlags;groupFlags");
		sb.AppendLine("# group colors: " + string.Join(", ", Enum.GetValues(typeof(StaffColor)).Cast<StaffColor>().Select(delegate(StaffColor c)
		{
			StaffColor staffColor = c;
			return staffColor.ToString();
		})));
		sb.AppendLine("# group flags: " + string.Join(", ", Enum.GetValues(typeof(StaffGroupFlags)).Cast<StaffGroupFlags>().Select(delegate(StaffGroupFlags f)
		{
			StaffGroupFlags staffGroupFlags = f;
			return staffGroupFlags.ToString();
		})));
		sb.AppendLine("# group badge flags: " + string.Join(", ", Enum.GetValues(typeof(StaffBadgeFlags)).Cast<StaffBadgeFlags>().Select(delegate(StaffBadgeFlags f)
		{
			StaffBadgeFlags staffBadgeFlags = f;
			return staffBadgeFlags.ToString();
		})));
		sb.AppendLine();
		sb.AppendLine("# permission syntax: permissionNode=groupKey1,groupKey2");
		sb.AppendLine("# permission nodes: " + string.Join(", ", Enum.GetValues(typeof(StaffPermissions)).Cast<StaffPermissions>().Select(delegate(StaffPermissions p)
		{
			StaffPermissions staffPermissions = p;
			return staffPermissions.ToString();
		})));
		sb.AppendLine();
		sb.AppendLine("# Groups");
		sb.AppendLine();
		groupsDict.ForEach(delegate(KeyValuePair<string, StaffGroup> p)
		{
			p.Value.Permissions.ForEach(delegate(StaffPermissions perm)
			{
				if (!permsDict.ContainsKey(perm))
				{
					permsDict.Add(perm, new List<string> { p.Key });
				}
				else
				{
					permsDict[perm].Add(p.Key);
				}
			});
			sb.AppendLine(string.Format("{0}={1};{2};{3};{4};{5};{6}", p.Key, p.Value.Text, p.Value.Color, p.Value.KickPower, p.Value.RequiredKickPower, string.Join(",", p.Value.BadgeFlags.Select((StaffBadgeFlags f) => f.ToString())), string.Join(",", p.Value.GroupFlags.Select((StaffGroupFlags f) => f.ToString()))));
		});
		sb.AppendLine();
		sb.AppendLine("# Permissions");
		sb.AppendLine();
		if (!permsDict.Any())
		{
			foreach (StaffPermissions item in Enum.GetValues(typeof(StaffPermissions)).Cast<StaffPermissions>())
			{
				sb.AppendLine($"{item}=");
			}
		}
		else
		{
			permsDict.ForEach(delegate(KeyValuePair<StaffPermissions, List<string>> pair)
			{
				sb.AppendLine(string.Format("{0}={1}", pair.Key, string.Join(",", pair.Value)));
			});
		}
		GroupsBuffer = sb.ToString();
	}
}
