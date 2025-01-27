using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using helpers;
using helpers.Attributes;
using helpers.IO.Watcher;
using helpers.Random;
using PluginAPI.Events;

namespace Compendium.Staff;

public static class StaffHandler
{
	private static bool _saved;

	private static bool _firstSave;

	private static readonly Dictionary<string, string[]> _members = new Dictionary<string, string[]>();

	private static readonly Dictionary<string, StaffGroup> _groups = new Dictionary<string, StaffGroup>();

	private static readonly Dictionary<string, UserGroup> _groupsById = new Dictionary<string, UserGroup>();

	private static readonly Dictionary<ReferenceHub, string> _usersById = new Dictionary<ReferenceHub, string>();

	public static string RolesFilePath => Directories.GetDataPath("Staff Groups.txt", "groups");

	public static string MembersFilePath => Directories.GetDataPath("Staff Members.txt", "members");

	public static IReadOnlyDictionary<string, string[]> Members => _members;

	public static IReadOnlyDictionary<string, StaffGroup> Groups => _groups;

	public static IReadOnlyDictionary<ReferenceHub, string> UserRoundIds => _usersById;

	public static IReadOnlyDictionary<string, UserGroup> GroupRoundIds => _groupsById;

	[Load]
	[Reload]
	private static void OnLoad()
	{
        StaticWatcher.AddHandler(RolesFilePath, typeof(StaffHandler).Method(nameof(ReloadFromFile)), null, NotifyFilters.LastWrite, WatcherChangeTypes.Changed);
        StaticWatcher.AddHandler(MembersFilePath, typeof(StaffHandler).Method(nameof(ReloadFromFile)), null, NotifyFilters.LastWrite, WatcherChangeTypes.Changed);

        if (!File.Exists(MembersFilePath)) {
            File.WriteAllText(MembersFilePath, "# Syntax: ID: groupKey1,groupKey2,groupKey3\n# Example: 776561198456564: owner,developer");
        }

        if (!File.Exists(RolesFilePath)) {
            File.WriteAllText(RolesFilePath, "# Group Syntax: groupKey=text;color;kickPower;requiredKickPower;badgeFlags;groupFlags\n# Group Colors: " + string.Join(", ", Enum.GetValues(typeof(StaffColor)).Cast<StaffColor>().Select(delegate (StaffColor c) {
                StaffColor staffColor = c;
                return staffColor.ToString();
            })) + "\n# Group Flags: " + string.Join(", ", Enum.GetValues(typeof(StaffGroupFlags)).Cast<StaffGroupFlags>().Select(delegate (StaffGroupFlags f) {
                StaffGroupFlags staffGroupFlags = f;
                return staffGroupFlags.ToString();
            })) + "\n# Group Badge Flags: " + string.Join(", ", Enum.GetValues(typeof(StaffBadgeFlags)).Cast<StaffBadgeFlags>().Select(delegate (StaffBadgeFlags f) {
                StaffBadgeFlags staffBadgeFlags = f;
                return staffBadgeFlags.ToString();
            })) + "\n\n# Permission Syntax: permissionNode=groupKey1,groupKey2\n# Permission Nodes: " + string.Join(", ", Enum.GetValues(typeof(StaffPermissions)).Cast<StaffPermissions>().Select(delegate (StaffPermissions p) {
                StaffPermissions staffPermissions = p;
                return staffPermissions.ToString();
            })));
        }

		ReloadFromFile();
    }

	private static void ReloadFromFile() {
		if (_saved) {
			return;
		}
        Calls.Delay(0.1f, delegate {
            Plugin.Info($"Reading Staff files");
			try {
				_members.Clear();
				_groups.Clear();
				StaffReader.GroupsBuffer = File.ReadAllLines(RolesFilePath);
				StaffReader.MembersBuffer = File.ReadAllLines(MembersFilePath);
				StaffReader.ReadMembers(_members);
				StaffReader.ReadGroups(_groups);
				ReassignGroups();
				StaffActivity.Reload();

			} catch (Exception ex) {
                Plugin.Error($"Ex: {ex.ToString()}");
            } finally {
				Plugin.Info($"Loaded {_members.Count} member(s)");
				Plugin.Info($"Loaded {_groups.Count} group(s)");
			}
        });
    }


	private static void Save() {
        if (_saved) {
            return;
        }
        _saved = true;
		Calls.Delay(2f, delegate
		{
			_saved = false;
		});
		StaffWriter.WriteGroups(_groups);
		StaffWriter.WriteMembers(_members);
		File.WriteAllText(MembersFilePath, StaffWriter.MembersBuffer);
		File.WriteAllText(RolesFilePath, StaffWriter.GroupsBuffer);
		StaffWriter.MembersBuffer = null;
		StaffWriter.GroupsBuffer = null;
	}

	[Unload]
	private static void Unload()
	{
		Save();
		ReassignGroups();
	}

	public static void ReassignGroups()
	{
		_usersById.Clear();
		_groupsById.Clear();
		ServerStatic.PermissionsHandler?._groups.Clear();
		ServerStatic.PermissionsHandler?._members.Clear();
		Hub.Hubs.ForEach(delegate(ReferenceHub hub)
		{
			if (!hub.IsNorthwoodModerator() && !hub.IsNorthwoodStaff())
			{
				SetRole(hub);
			}
		});
	}

	public static void SetGroups(string userId, string[] groups)
	{
		_members[userId] = groups;
		Save();
		ReassignGroups();
	}

	public static void SetGroup(string userId, string group)
	{
		_members[userId] = new string[1] { group };
		Save();
		ReassignGroups();
	}

	public static void RemoveGroup(string userId, string group)
	{
		if (_members.TryGetValue(userId, out var value))
		{
			_members[userId] = value.Where((string g) => g != group).ToArray();
			if (_members[userId].Length == 0)
			{
				_members.Remove(userId);
			}
			Save();
			ReassignGroups();
		}
	}

	public static void RemoveGroups(string userId, string[] groups)
	{
		if (_members.TryGetValue(userId, out var value))
		{
			_members[userId] = value.Where((string g) => !groups.Contains<string>(g)).ToArray();
			if (_members[userId].Length == 0)
			{
				_members.Remove(userId);
			}
			Save();
			ReassignGroups();
		}
	}

	public static void AddGroup(string userId, string group)
	{
		if (!_groups.TryGetValue(group, out var value))
		{
			return;
		}
		if (_members.TryGetValue(userId, out var value2))
		{
			if (!group.Contains(value.Key))
			{
				_members[userId] = value2.Concat(new string[1] { value.Key }).ToArray();
				Save();
				ReassignGroups();
			}
		}
		else
		{
			_members[userId] = new string[1] { value.Key };
			Save();
			ReassignGroups();
		}
	}

	public static void RemoveMember(string userId)
	{
		if (_members.Remove(userId))
		{
			Save();
			ReassignGroups();
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		_usersById.Clear();
		_groupsById.Clear();
		ServerStatic.PermissionsHandler?._groups.Clear();
		ServerStatic.PermissionsHandler?._members.Clear();
	}

	[Event]
	private static void OnPlayerJoined(PlayerJoinedEvent ev)
	{
		Calls.Delay(0.2f, delegate
		{
			SetRole(ev.Player.ReferenceHub);
		});
	}

	[Event]
	private static void OnPlayerLeft(PlayerLeftEvent ev)
	{
		if (ev.Player != null && (object)ev.Player.ReferenceHub != null)
		{
			if (_usersById.TryGetValue(ev.Player.ReferenceHub, out var value))
			{
				_groupsById.Remove(value);
				ServerStatic.PermissionsHandler?._groups.Remove(value);
			}
			_usersById.Remove(ev.Player.ReferenceHub);
			ServerStatic.PermissionsHandler?._members.Remove(ev.Player.UserId);
		}
	}

	private static void SetRole(ReferenceHub target)
	{
		string[] value;
		if (ServerStatic.PermissionsHandler == null)
		{
			Calls.OnFalse(delegate
			{
				SetRole(target);
			}, () => ServerStatic.PermissionsHandler == null);
		}
		else if (_members.TryGetValue(target.UserId(), out value) && value.Any())
		{
			if (!_usersById.TryGetValue(target, out var value2) || !_groupsById.TryGetValue(value2, out var ogGroup))
			{
				ogGroup = new UserGroup();
			}
			ogGroup.Permissions = 0uL;
			ogGroup.BadgeText = string.Empty;
			ogGroup.BadgeColor = string.Empty;
			ogGroup.Cover = false;
			ogGroup.HiddenByDefault = false;
			ogGroup.RequiredKickPower = 0;
			ogGroup.KickPower = 0;
			ogGroup.Shared = false;
			value.ForEach(delegate(string groupKey)
			{
				if (!_groups.TryGetValue(groupKey, out var value4))
				{
					Plugin.Warn("Failed to find group for key \"" + groupKey + "\"");
				}
				else
				{
					if (value4.GroupFlags.Contains(StaffGroupFlags.IsReservedSlot) && !target.HasReservedSlot())
					{
						target.AddReservedSlot(isTemporary: false, addNick: true);
					}
					if (ogGroup.Permissions != 0)
					{
						PlayerPermissions playerPermissions = (PlayerPermissions)ogGroup.Permissions;
						PlayerPermissions permissions = StaffUtils.ToNwPermissions(value4);
						foreach (PlayerPermissions permission in StaffUtils.Permissions)
						{
							if (!PermissionsHandler.IsPermitted(ogGroup.Permissions, permission) && PermissionsHandler.IsPermitted((ulong)permissions, permission))
							{
								playerPermissions |= permission;
								ogGroup.Permissions = (ulong)playerPermissions;
							}
						}
					}
					else
					{
						ogGroup.Permissions = (ulong)StaffUtils.ToNwPermissions(value4);
					}
					if (string.IsNullOrWhiteSpace(ogGroup.BadgeText))
					{
						ogGroup.BadgeText = value4.Text;
					}
					else
					{
						UserGroup userGroup = ogGroup;
						userGroup.BadgeText = userGroup.BadgeText + " | " + value4.Text;
					}
					if (string.IsNullOrWhiteSpace(ogGroup.BadgeColor))
					{
						ogGroup.BadgeColor = StaffUtils.GetColor(value4.Color);
					}
					if (!ogGroup.Cover && value4.BadgeFlags.Contains(StaffBadgeFlags.IsCover))
					{
						ogGroup.Cover = true;
					}
					if (!ogGroup.HiddenByDefault && value4.BadgeFlags.Contains(StaffBadgeFlags.IsHidden))
					{
						ogGroup.HiddenByDefault = true;
					}
					if (value4.RequiredKickPower > ogGroup.RequiredKickPower)
					{
						ogGroup.RequiredKickPower = value4.RequiredKickPower;
					}
					if (value4.KickPower > ogGroup.KickPower)
					{
						ogGroup.KickPower = value4.KickPower;
					}
				}
			});
			if (!_usersById.TryGetValue(target, out value2))
			{
				string text = (_usersById[target] = RandomGeneration.Default.GetReadableString(30));
				string text2 = text;
				value2 = text2;
			}
			_groupsById[value2] = ogGroup;
			ServerStatic.PermissionsHandler._groups[value2] = ogGroup;
			ServerStatic.PermissionsHandler._members[target.UserId()] = value2;
			target.serverRoles.RefreshPermissions();
			target.queryProcessor.GameplayData = PermissionsHandler.IsPermitted(ogGroup.Permissions, PlayerPermissions.GameplayData);
		}
		else
		{
			if (ServerStatic.PermissionsHandler._members.TryGetValue(target.UserId(), out var value3))
			{
				ServerStatic.PermissionsHandler._groups.Remove(value3);
				ServerStatic.PermissionsHandler._members.Remove(target.UserId());
				_groupsById.Remove(value3);
			}
			_usersById.Remove(target);
			target.serverRoles.SetGroup(null, byAdmin: false, disp: true);
			if (target.HasReservedSlot())
			{
				target.RemoveReservedSlot();
			}
		}
	}
}
