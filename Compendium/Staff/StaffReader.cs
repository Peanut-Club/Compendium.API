using System;
using System.Collections.Generic;
using helpers;
using helpers.Extensions;

namespace Compendium.Staff;

public static class StaffReader
{
	internal static string[] MembersBuffer;

	internal static string[] GroupsBuffer;

	public static void ReadMembers(Dictionary<string, string[]> membersDict)
	{
		membersDict.Clear();
		if (MembersBuffer == null || MembersBuffer.IsEmpty())
		{
			return;
		}
		string[] membersBuffer = MembersBuffer;
		string[] array = membersBuffer;
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text) || text.StartsWith("#"))
			{
				continue;
			}
			if (!text.TrySplit(':', removeEmptyOrWhitespace: true, 2, out var splits))
			{
				Plugin.Warn("Failed to parse line \"" + text + "\"!");
				continue;
			}
			string text2 = splits[0].Trim();
			string text3 = splits[1].Trim();
			string[] array2 = text3.Split(new char[1] { ',' });
			if (!UserIdValue.TryParse(text2, out var value))
			{
				Plugin.Warn("Failed to parse ID \"" + text2 + "\"!");
				continue;
			}
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = array2[j].Trim();
			}
			membersDict[value.Value] = array2;
		}
		MembersBuffer = null;
	}

	public static void ReadGroups(Dictionary<string, StaffGroup> groupsDict)
	{
		groupsDict.Clear();
		if (GroupsBuffer == null || GroupsBuffer.IsEmpty())
		{
			return;
		}
		Dictionary<StaffPermissions, List<string>> permsDict = new Dictionary<StaffPermissions, List<string>>();
		string[] groupsBuffer = GroupsBuffer;
		string[] array = groupsBuffer;
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text) || text.StartsWith("#"))
			{
				continue;
			}
			if (text.TrySplit('=', removeEmptyOrWhitespace: true, 2, out var splits))
			{
				string key = splits[0];
				string[] splits5;
				if (splits[1].TrySplit(';', removeEmptyOrWhitespace: true, 6, out var splits2))
				{
					string text2 = splits2[0].Trim();
					string value = splits2[1].Trim();
					string s = splits2[2].Trim();
					string s2 = splits2[3].Trim();
					string line = splits2[4].Trim();
					string line2 = splits2[5].Trim();
					if (!byte.TryParse(s, out var result))
					{
						Plugin.Warn("Failed to parse the kick power of \"" + key + "\"");
						continue;
					}
					if (!byte.TryParse(s2, out var result2))
					{
						Plugin.Warn("Failed to parse the required kick power of \"" + key + "\"");
						continue;
					}
					if (!Enum.TryParse<StaffColor>(value, ignoreCase: true, out var result3))
					{
						Plugin.Warn("Failed to parse the color of \"" + key + "\"");
						continue;
					}
					if (!line.TrySplit(',', removeEmptyOrWhitespace: true, null, out var splits3))
					{
						Plugin.Warn("Failed to parse a list of badge flags of role \"" + key + "\"");
						continue;
					}
					if (!line2.TrySplit(',', removeEmptyOrWhitespace: true, null, out var splits4))
					{
						Plugin.Warn("Failed to parse a list of group flags of role \"" + key + "\"");
						continue;
					}
					List<StaffBadgeFlags> badgeFlags = new List<StaffBadgeFlags>();
					List<StaffGroupFlags> groupFlags = new List<StaffGroupFlags>();
					splits3.ForEach(delegate(string str)
					{
						if (!Enum.TryParse<StaffBadgeFlags>(str.Trim(), ignoreCase: true, out var result5))
						{
							Plugin.Warn("Failed to parse badge flag \"" + str + "\" of role \"" + key + "\"");
						}
						else
						{
							badgeFlags.Add(result5);
						}
					});
					splits4.ForEach(delegate(string str)
					{
						if (!Enum.TryParse<StaffGroupFlags>(str.Trim(), ignoreCase: true, out var result4))
						{
							Plugin.Warn("Failed to parse group flag \"" + str + "\" of role \"" + key + "\"");
						}
						else
						{
							groupFlags.Add(result4);
						}
					});
					groupsDict[key] = new StaffGroup(key, text2, result, result2, result3, badgeFlags, groupFlags);
				}
				else if (splits[1].TrySplit(',', removeEmptyOrWhitespace: true, null, out splits5))
				{
					if (!Enum.TryParse<StaffPermissions>(key.Trim(), ignoreCase: true, out var permFlag))
					{
						Plugin.Warn("Failed to parse permission flag \"" + key + "\"");
						continue;
					}
					permsDict[permFlag] = new List<string>();
					splits5.ForEach(delegate(string str)
					{
						if (!groupsDict.TryGetValue(str.Trim(), out var value3))
						{
							Plugin.Warn($"Failed to find group for permission flag \"{permFlag}\": \"{str}\"");
						}
						else
						{
							permsDict[permFlag].Add(value3.Key);
						}
					});
				}
				else
				{
					Plugin.Warn("Found an unknown line: \"" + text + "\"");
				}
			}
			else
			{
				Plugin.Warn("Found an unknown line: \"" + text + "\"");
			}
		}
		permsDict.ForEach(delegate(KeyValuePair<StaffPermissions, List<string>> p)
		{
			p.Value.ForEach(delegate(string k)
			{
				if (groupsDict.TryGetValue(k, out var value2))
				{
					value2.Permissions.Add(p.Key);
				}
			});
		});
		GroupsBuffer = null;
	}
}
