using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using BetterCommands;
using Compendium.Attributes;
using Compendium.Comparison;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Generation;
using Compendium.IO.Saving;
using helpers;
using helpers.Attributes;
using helpers.Time;
using PluginAPI.Events;

namespace Compendium.PlayerData;

public static class PlayerDataRecorder
{
	private static SaveFile<CollectionSaveData<PlayerDataRecord>> _records;

	private static Dictionary<ReferenceHub, AuthenticationToken> _tokenRecords = new Dictionary<ReferenceHub, AuthenticationToken>();

	private static Dictionary<ReferenceHub, PlayerDataRecord> _activeRecords = new Dictionary<ReferenceHub, PlayerDataRecord>();

	public static event Action<ReferenceHub, PlayerDataRecord> OnRecordUpdated;

	public static bool TryQuery(string query, bool queryNick, out PlayerDataRecord record)
	{
		if (int.TryParse(query, out var plyId) && Hub.Hubs.TryGetFirst((ReferenceHub h) => h.PlayerId == plyId, out var value))
		{
			record = GetData(value);
			return true;
		}
		IPAddress address;
		bool flag = IPAddress.TryParse(query, out address);
		UserIdValue value2;
		bool flag2 = UserIdValue.TryParse(query, out value2);
		foreach (PlayerDataRecord datum in _records.Data)
		{
			if (datum != null)
			{
				if (datum.Id == query)
				{
					record = datum;
					return true;
				}
				if (datum.Ip == query && flag)
				{
					record = datum;
					return true;
				}
				if (flag2 && value2.Value == datum.UserId)
				{
					record = datum;
					return true;
				}
				if (!flag2 && !flag && queryNick && NicknameComparison.Compare(query, datum.NameTracking.LastValue, 0.7))
				{
					record = datum;
					return true;
				}
			}
		}
		record = null;
		return false;
	}

	public static AuthenticationToken GetToken(ReferenceHub hub)
	{
		if (!hub.IsPlayer() || hub.authManager.AuthenticationResponse.SignedAuthToken == null)
		{
			return null;
		}
		if (_tokenRecords.TryGetValue(hub, out var value))
		{
			return value;
		}
		if (!hub.authManager.AuthenticationResponse.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out value, out var _, out var _))
		{
			return null;
		}
		return _tokenRecords[hub] = value;
	}

	public static PlayerDataRecord GetData(AuthenticationToken token)
	{
		if (token == null)
		{
			return null;
		}
		if (!TryQuery(token.UserId, queryNick: false, out var record))
		{
			return null;
		}
		return record;
	}

	public static PlayerDataRecord GetData(ReferenceHub hub)
	{
		if (!_activeRecords.TryGetValue(hub, out var value))
		{
			AuthenticationToken token = GetToken(hub);
			if (token == null)
			{
				return null;
			}
			value = GetData(token);
		}
		if (value == null)
		{
			value = new PlayerDataRecord
			{
				CreationTime = TimeUtils.LocalTime,
				LastActivity = TimeUtils.LocalTime,
				Id = UniqueIdGeneration.Generate()
			};
			_records.Data.Add(value);
			_records.Save();
		}
		return value;
	}

	public static void UpdateData(ReferenceHub hub)
	{
		PlayerDataRecord data = GetData(hub);
		if (data != null)
		{
			_activeRecords[hub] = data;
			data.Ip = hub.Ip();
			data.UserId = hub.UserId();
			data.NameTracking.Compare(hub.Nick().Trim());
			data.LastActivity = TimeUtils.LocalTime;
			_records.Save();
			PlayerDataRecorder.OnRecordUpdated?.Invoke(hub, data);
		}
	}

	[Load]
	[Reload]
	public static void Load()
	{
		if (_records != null)
		{
			_records.Load();
		}
		else
		{
			_records = new SaveFile<CollectionSaveData<PlayerDataRecord>>(Directories.GetDataPath("SavedPlayerData", "playerData"));
		}
	}

	[Unload]
	public static void Unload()
	{
		if (_records != null)
		{
			_records.Save();
		}
	}

	[Event]
	private static void OnPlayerJoined(PlayerJoinedEvent ev)
	{
		UpdateData(ev.Player.ReferenceHub);
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnRestart()
	{
		_tokenRecords.Clear();
		_activeRecords.Clear();
	}

	[Command("query", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Displays all available information about a record.")]
	private static string QueryCommand(ReferenceHub sender, PlayerDataRecord record)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("== Record ID: " + record.Id + " ==");
		sb.AppendLine($" > Tracked Names ({record.NameTracking.AllValues.Count}):");
		record.NameTracking.AllValues.For(delegate(int i, KeyValuePair<DateTime, string> pair)
		{
			sb.AppendLine(string.Format("   -> [{0}] {1} ({2})", i + 1, pair.Value, pair.Key.ToString("F")));
		});
		sb.AppendLine(" > Tracked Account: " + record.UserId);
		sb.AppendLine(" > Tracked IP: " + record.Ip);
		sb.AppendLine(" > Last Seen: " + record.LastActivity.ToString("F"));
		sb.AppendLine(" > Tracked Since: " + record.CreationTime.ToString("F"));
		return sb.ToString();
	}
}
