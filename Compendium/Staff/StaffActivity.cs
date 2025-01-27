using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BetterCommands;
using BetterCommands.Permissions;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.IO.Saving;
using Compendium.PlayerData;
using Compendium.Updating;
using helpers;
using helpers.Attributes;
using helpers.Time;
using PlayerRoles;

namespace Compendium.Staff;

public static class StaffActivity
{
	internal static SaveFile<CollectionSaveData<StaffActivityData>> _storage;

	private static object _lock = new object();

	[Load]
	public static void Load()
	{
		if (_storage != null)
		{
			_storage.Load();
			return;
		}
		_storage = new SaveFile<CollectionSaveData<StaffActivityData>>(Directories.GetDataPath("SavedStaffPlaytime", "staffPlaytime"));
		Plugin.Info($"Loaded {_storage.Data.Count} activity record(s)");
	}

	public static void Reload()
	{
		if (_storage == null)
		{
			Load();
		}
		lock (_lock)
		{
			StaffHandler.Members.ForEach(delegate(KeyValuePair<string, string[]> p)
			{
				if (p.Value.Any((string x) => StaffHandler.Groups.TryGetValue(x, out var value2) && value2.GroupFlags.Contains(StaffGroupFlags.IsStaff)) && !_storage.Data.TryGetFirst((StaffActivityData x) => x.UserId == p.Key, out var _))
				{
					_storage.Data.Add(new StaffActivityData
					{
						Total = 0L,
						TwoWeeks = 0L,
						TwoWeeksStart = TimeUtils.LocalTime,
						UserId = p.Key
					});
				}
			});
			_storage.Save();
		}
	}

	[Update(Delay = 1000)]
	private static void OnUpdate()
	{
		if (_storage == null)
		{
			return;
		}
		lock (_lock)
		{
			for (int i = 0; i < _storage.Data.Count; i++)
			{
				if (Hub.TryGetHub(_storage.Data[i].UserId, out var hub))
				{
					if (hub.RoleId() != RoleTypeId.Overwatch)
					{
						_storage.Data[i].Total++;
						_storage.Data[i].TwoWeeks++;
					}
					else
					{
						_storage.Data[i].TotalOverwatch++;
						_storage.Data[i].TwoWeeksOverwatch++;
					}
				}
			}
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	private static void OnRoundRestart()
	{
		_storage?.Save();
	}

	[Command("resetactivity", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Permission(PermissionLevel.Administrator)]
	[Description("Resets the two-week activity counter for all staff members.")]
	private static string ResetActivityCommand(ReferenceHub sender)
	{
		lock (_lock)
		{
			_storage.Data.ForEach(delegate(StaffActivityData x)
			{
				x.TwoWeeks = 0L;
				x.TwoWeeksOverwatch = 0L;
				x.TwoWeeksStart = TimeUtils.LocalTime;
			});
			string[] value;
			StaffGroup value2;
			IEnumerable<StaffActivityData> enumerable = _storage.Data.Where((StaffActivityData d) => !StaffHandler.Members.TryGetValue(d.UserId, out value) || !value.Any((string g) => StaffHandler.Groups.TryGetValue(g, out value2) && value2.GroupFlags.Contains(StaffGroupFlags.IsStaff)));
			if (enumerable.Any())
			{
				foreach (StaffActivityData item in enumerable)
				{
					_storage.Data.Remove(item);
				}
			}
			_storage.Save();
			if (Plugin.Config.ApiSetttings.ShowActivityDebug)
			{
				Plugin.Debug("Reset two-weeks activity by command.");
			}
		}
		return "Reset activity for all staff members.";
	}

	[Command("totalactivity", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Shows a list of staff members and their total activity.")]
	private static string TotalActivityCommand(ReferenceHub sender)
	{
		StringBuilder sb = Pools.PoolStringBuilder(true, $"Showing a list of {_storage.Data.Count} activity record(s)");
		IOrderedEnumerable<StaffActivityData> values = _storage.Data.OrderByDescending((StaffActivityData x) => x.TwoWeeks);
		values.ForEach(delegate(StaffActivityData x)
		{
			if (PlayerDataRecorder.TryQuery(x.UserId, queryNick: false, out var record))
			{
				sb.AppendLine(record.NameTracking.LastValue + " (" + record.UserId + "): " + TimeSpan.FromSeconds(x.TwoWeeks).UserFriendlySpan() + " (" + TimeSpan.FromSeconds(x.TwoWeeksOverwatch).UserFriendlySpan() + " in OW) / " + TimeSpan.FromSeconds(x.Total).UserFriendlySpan() + " (" + TimeSpan.FromSeconds(x.TotalOverwatch).UserFriendlySpan() + " in OW) (two-weeks counter started at " + x.TwoWeeksStart.ToString("G") + ")");
			}
			else
			{
				sb.AppendLine(x.UserId + ": " + TimeSpan.FromSeconds(x.TwoWeeks).UserFriendlySpan() + " (" + TimeSpan.FromSeconds(x.TwoWeeksOverwatch).UserFriendlySpan() + " in OW) / " + TimeSpan.FromSeconds(x.Total).UserFriendlySpan() + " (" + TimeSpan.FromSeconds(x.TotalOverwatch).UserFriendlySpan() + " in OW) (two-weeks counter started at " + x.TwoWeeksStart.ToString("G") + ")");
			}
		});
		return sb.ReturnStringBuilderValue();
	}

	[Command("staffactivity", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Shows the total activity of a specified staff member.")]
	private static string StaffActivityCommand(ReferenceHub sender, string target)
	{
		StaffActivityData value = null;
		if (!PlayerDataRecorder.TryQuery(target, queryNick: true, out var record) || !_storage.Data.TryGetFirst((StaffActivityData x) => x.UserId == record.UserId, out value) || value == null)
		{
			return "Failed to find any activity records matching your query.";
		}
		return record.NameTracking.LastValue + " (" + record.UserId + "): " + TimeSpan.FromSeconds(value.TwoWeeks).UserFriendlySpan() + " (" + TimeSpan.FromSeconds(value.TwoWeeksOverwatch).UserFriendlySpan() + " in OW) / " + TimeSpan.FromSeconds(value.Total).UserFriendlySpan() + " (" + TimeSpan.FromSeconds(value.TotalOverwatch).UserFriendlySpan() + " in OW) (two-weeks counter started at " + value.TwoWeeksStart.ToString("G") + ")";
	}
}
