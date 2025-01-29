using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Compendium.PlayerData;
using helpers.Extensions;
using helpers.Pooling.Pools;
using MEC;
using PluginAPI.Events;
using static PlayerRoles.Spectating.SpectatableModuleBase;

namespace Compendium;

public static class BanHelper
{
	[Flags]
	public enum UnbanResult : byte
	{
		None = 2,
		IpRemoved = 4,
		IdRemoved = 8,
		IpNotBanned = 0x10,
		IdNotBanned = 0x20,
		Failed = 0x40,
		NotBanned = 0x80
	}

	[Flags]
	public enum OfflineBanResult : byte
	{
		None = 2,
		IpBanned = 4,
		IdBanned = 8,
		Failed = 0x10
	}

	[Flags]
	public enum BanType
	{
		Ip = 2,
		Id = 4
	}

	/*
	public class BanInfo
	{
		public BanType Type { get; set; }

		public DateTime ExpiresAt { get; set; }

		public DateTime IssuedAt { get; set; }

		public string IssuerId { get; set; }

		public string IssuerName { get; set; }

		public string TargetId { get; set; }

		public string TargetName { get; set; }

		public string Reason { get; set; }

		public bool IsParsed { get; set; }

		public static BanInfo Get(long expiresTicks, long issuedTicks, string issuerString, string targetId, string targetName, string reason, BanType type)
		{
			try
			{
				BanInfo banInfo = new BanInfo();
				banInfo.ExpiresAt = new DateTime(expiresTicks);
				banInfo.IssuedAt = new DateTime(issuedTicks);
				banInfo.TargetId = targetId;
				banInfo.TargetName = targetName;
				banInfo.Reason = reason;
				banInfo.Type = type;
				if (!TryParseIssuer(issuerString, out var name, out var id))
				{
					banInfo.IssuerName = issuerString;
					banInfo.IssuerId = issuerString;
					banInfo.IsParsed = false;
				}
				else
				{
					banInfo.IssuerName = name;
					banInfo.IssuerId = id;
					banInfo.IsParsed = true;
				}
				return banInfo;
			}
			catch (Exception ex)
			{
				Plugin.Error("Problem in BanHelper.BanInfo.Get():");
				Plugin.Error($"{ex.GetType()}{ex.Message}");
				Plugin.Error("Info:");
				Plugin.Error($"expiresTicks: {expiresTicks}");
				Plugin.Error($"issuedTicks: {issuedTicks}");
				Plugin.Error("issuerString: " + issuerString);
				Plugin.Error("targetId: " + targetId);
				Plugin.Error("targetName: " + targetName);
				Plugin.Error("reason: " + reason);
				Plugin.Error($"type: {type}");
				return null;
			}
		}

		private static bool TryParseIssuer(string issuerString, out string name, out string id)
		{
			name = null;
			id = null;
			if (string.IsNullOrWhiteSpace(issuerString) || !issuerString.Contains("(") || !issuerString.Contains(")"))
			{
				return false;
			}
			try
			{
				int num = issuerString.LastIndexOf('(');
				int num2 = issuerString.LastIndexOf(')');
				name = issuerString.GetBeforeIndex(num).Trim();
				id = issuerString.Between(num - 1, num2 + 2);
			}
			catch (Exception message)
			{
				Plugin.Error(message);
				return false;
			}
			if (!string.IsNullOrWhiteSpace(name))
			{
				return !string.IsNullOrWhiteSpace(id);
			}
			return false;
		}
	}

	public static event Action<BanInfo> OnUnbanned;

	public static event Action<ReferenceHub, BanInfo> OnBanned;
	*/

	/*
	public static BanInfo[] QueryBans(string target, bool includeHistory = true)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		PlayerDataRecord targetRecord = null;
		IPAddress address;
		if (UserIdValue.TryParse(target, out var userId))
		{
			flag = true;
			if (!PlayerDataRecorder.TryQuery(userId.Value, queryNick: false, out targetRecord))
			{
				flag3 = false;
				Plugin.Warn("Failed to find target's record by user ID: " + userId.Value);
			}
			else
			{
				flag3 = true;
				Plugin.Info("Found target's record by user ID: " + targetRecord.NameTracking.LastValue + " (" + targetRecord.UserId + " - " + targetRecord.Ip + ")");
			}
		}
		else if (IPAddress.TryParse(target, out address))
		{
			flag2 = true;
			if (!PlayerDataRecorder.TryQuery(address.ToString(), queryNick: false, out targetRecord))
			{
				flag3 = false;
				Plugin.Warn($"Failed to find target's record by IP: {address}");
			}
			else
			{
				flag3 = true;
				Plugin.Info("Found target's record by IP: " + targetRecord.NameTracking.LastValue + " (" + targetRecord.UserId + " - " + targetRecord.Ip + ")");
			}
		}
		else if (PlayerDataRecorder.TryQuery(target, queryNick: true, out targetRecord))
		{
			flag3 = true;
			flag2 = false;
			flag = false;
		}
		BanInfo[] source = ReadBans(BanType.Id);
		BanInfo[] source2 = ReadBans(BanType.Ip);
		if (flag3)
		{
			source = source.Where((BanInfo b) => b.TargetId == targetRecord.UserId).ToArray();
			source2 = source2.Where((BanInfo b) => b.TargetId == targetRecord.Ip).ToArray();
			if (includeHistory)
			{
				source = source.Concat(BanHistory.History.Where((BanInfo b) => b.Type == BanType.Id && b.TargetId == targetRecord.UserId)).ToArray();
				source2 = source2.Concat(BanHistory.History.Where((BanInfo b) => b.Type == BanType.Ip && b.TargetId == targetRecord.Ip)).ToArray();
			}
			return source.Concat(source2).ToArray();
		}
		source = ((!flag) ? Array.Empty<BanInfo>() : source.Where((BanInfo b) => b.TargetId == userId.Value).ToArray());
		source2 = ((!flag2) ? Array.Empty<BanInfo>() : source.Where((BanInfo b) => b.TargetId == target).ToArray());
		if (includeHistory)
		{
			if (flag)
			{
				source = source.Concat(BanHistory.History.Where((BanInfo b) => b.Type == BanType.Id && b.TargetId == userId.Value)).ToArray();
			}
			if (flag2)
			{
				source2 = source2.Concat(BanHistory.History.Where((BanInfo b) => b.Type == BanType.Ip && b.TargetId == target)).ToArray();
			}
		}
		return source.Concat(source2).ToArray();
	}
	*/

	public static OfflineBanResult TryOfflineBan(string target, string reason, DateTime expiresAt, ReferenceHub issuer, bool banIp = true, bool banId = true)
	{
		try
		{
			Plugin.Info($"Attempting to offline ban '{target}' .. (ban IP: {banIp}, ban ID: {banId})");
			string text = issuer.Nick();
			string text2 = issuer.UserId();
			long ticksNow = DateTime.Now.Ticks;
            bool isID = false;
			bool isIP = false;
			bool foundRecord = false;
			PlayerDataRecord record = null;
			IPAddress address;
			if (UserIdValue.TryParse(target, out var value))
			{
				isID = true;
				if (!PlayerDataRecorder.TryQuery(value.Value, queryNick: false, out record))
				{
					Plugin.Warn("Failed to find target's record by user ID: " + value.Value);
				}
				else
				{
					foundRecord = true;
					Plugin.Info("Found target's record by user ID: " + record.NameTracking.LastValue + " (" + record.UserId + " - " + record.Ip + ")");
				}
			}
			else if (IPAddress.TryParse(target, out address))
			{
				isIP = true;
				if (!PlayerDataRecorder.TryQuery(address.ToString(), queryNick: false, out record))
				{
					Plugin.Warn($"Failed to find target's record by IP: {address}");
				}
				else
				{
					foundRecord = true;
					Plugin.Info("Found target's record by IP: " + record.NameTracking.LastValue + " (" + record.UserId + " - " + record.Ip + ")");
				}
			}
			else if (PlayerDataRecorder.TryQuery(target, queryNick: true, out record))
			{
				foundRecord = true;
			}

			//TryUnban(target, banIp, banId);

			OfflineBanResult offlineBanResult = 0;
            BanDetails banDetails = new BanDetails {
                OriginalName = "Offline Player",
                Id = target,
                Expires = expiresAt.Ticks,
                Reason = reason,
                Issuer = text + " (" + text2 + ")",
                IssuanceTime = ticksNow
            };

            if (foundRecord && record != null) {
				banDetails.OriginalName = record.NameTracking.LastValue;
                banDetails.Id = record.UserId;
			}

			if (banId) {
                Plugin.Info("Issuing ID ban ..");
                BanHandler.IssueBan(banDetails, BanHandler.BanType.UserId, true);
                EventManager.ExecuteEvent(new PlayerBannedEvent(foundRecord ? record.UserId : target, foundRecord ? record.NameTracking.LastValue : "Offline Player", foundRecord ? record.Ip : string.Empty, issuer, reason, expiresAt.Ticks));
				offlineBanResult |= OfflineBanResult.IdBanned;
            }
			if (banIp) {
                Plugin.Info("Issuing IP ban ..");
                banDetails.Id = target;
				if (foundRecord && record != null) {
                    banDetails.Id = record.Ip;
                }
                BanHandler.IssueBan(banDetails, BanHandler.BanType.IP, true);
                EventManager.ExecuteEvent(new PlayerBannedEvent(foundRecord ? record.UserId : string.Empty, foundRecord ? record.NameTracking.LastValue : "Offline Player", foundRecord ? record.Ip : target, issuer, reason, expiresAt.Ticks));
                offlineBanResult |= OfflineBanResult.IpBanned;
            }

			if (offlineBanResult == 0) {
				Plugin.Warn("No bans were issued!");
                return OfflineBanResult.Failed;
            }

            /*
            List<BanInfo> list = new List<BanInfo>();
			if (banIp && (foundRecord || isIP))
			{
				if (foundRecord)
				{
					list.Add(BanInfo.Get(expiresAt.Ticks, DateTime.Now.Ticks, text + " (" + text2 + ")", record.Ip, record.NameTracking.LastValue, reason, BanType.Ip));
				}
				else
				{
					list.Add(BanInfo.Get(expiresAt.Ticks, DateTime.Now.Ticks, text + " (" + text2 + ")", target, "Offline Player", reason, BanType.Ip));
				}
			}
			else
			{
				Plugin.Warn("Skipping IP offline ban ..");
			}
			if (banId && (foundRecord || isID))
			{
				if (foundRecord)
				{
					list.Add(BanInfo.Get(expiresAt.Ticks, DateTime.Now.Ticks, text + " (" + text2 + ")", record.UserId, record.NameTracking.LastValue, reason, BanType.Id));
				}
				else
				{
					list.Add(BanInfo.Get(expiresAt.Ticks, DateTime.Now.Ticks, text + " (" + text2 + ")", target, "Offline Player", reason, BanType.Id));
				}
			}
			else
			{
				Plugin.Warn("Skipping ID offline ban ..");
			}
			if (list.Count < 1)
			{
				Plugin.Warn("No bans were issued!");
				return OfflineBanResult.Failed;
			}
			BanInfo[] curIpBans = ReadBans(BanType.Ip).Concat(list.Where((BanInfo b) => b.Type == BanType.Ip)).ToArray();
			BanInfo[] curIdBans = ReadBans(BanType.Id).Concat(list.Where((BanInfo b) => b.Type == BanType.Id)).ToArray();
			Timing.CallDelayed(0.1f, delegate
			{
				WriteBans(curIdBans, BanType.Id);
				WriteBans(curIpBans, BanType.Ip);
			});
			*/


            record?.TryInvokeHub(delegate(ReferenceHub hub)
			{
				hub.Kick("Dostal jsi ban!\nDůvod: " + reason + "\nExpirace: " + expiresAt.ToString("G"));
			});

			/*
			OfflineBanResult offlineBanResult = OfflineBanResult.None;
			bool flag4 = false;
			if (list.Any((BanInfo b) => b.Type == BanType.Id))
			{
				offlineBanResult |= OfflineBanResult.IdBanned;
				EventManager.ExecuteEvent(new PlayerBannedEvent(foundRecord ? record.UserId : target, foundRecord ? record.NameTracking.LastValue : "Offline Player", foundRecord ? record.Ip : string.Empty, issuer, reason, expiresAt.Ticks));
				EventManager.ExecuteEvent(new BanIssuedEvent(new BanDetails
				{
					Expires = expiresAt.Ticks,
					IssuanceTime = DateTime.Now.Ticks,
					Id = (foundRecord ? record.UserId : target),
					OriginalName = (foundRecord ? record.NameTracking.LastValue : "Offline Player"),
					Reason = reason,
					Issuer = issuer.Nick() + " (" + issuer.UserId() + ")"
				}, BanHandler.BanType.UserId));
				flag4 = true;
			}
			if (list.Any((BanInfo b) => b.Type == BanType.Ip))
			{
				offlineBanResult |= OfflineBanResult.IpBanned;
				EventManager.ExecuteEvent(new BanIssuedEvent(new BanDetails
				{
					Expires = expiresAt.Ticks,
					IssuanceTime = DateTime.Now.Ticks,
					Id = (foundRecord ? record.UserId : target),
					OriginalName = (foundRecord ? record.NameTracking.LastValue : "Offline Player"),
					Reason = reason,
					Issuer = issuer.Nick() + " (" + issuer.UserId() + ")"
				}, BanHandler.BanType.IP));
				if (!flag4)
				{
					EventManager.ExecuteEvent(new PlayerBannedEvent(foundRecord ? record.UserId : string.Empty, foundRecord ? record.NameTracking.LastValue : "Offline Player", foundRecord ? record.Ip : target, issuer, reason, expiresAt.Ticks));
					flag4 = true;
				}
			}
			if (offlineBanResult != OfflineBanResult.None)
			{
				offlineBanResult &= ~OfflineBanResult.None;
			}
			*/


			return offlineBanResult;
		}
		catch (Exception message)
		{
			Plugin.Error(message);
			return OfflineBanResult.Failed;
		}
	}

	public static UnbanResult TryUnban(string target, bool removeIpBans = true, bool removeIdBans = true)
	{
		try
		{
			Plugin.Info($"Attempting to unban '{target}' .. (remove IP: {removeIpBans}, remove ID: {removeIdBans})");
			string targetUserId = null;
			string targetIp = null;

            if (UserIdValue.TryParse(target, out var userIdValue))
			{
				if (!PlayerDataRecorder.TryQuery(userIdValue.Value, queryNick: false, out var targetRecord))
				{
                    targetUserId = userIdValue.Value;
					Plugin.Warn("Failed to find target's record by user ID: " + userIdValue.Value);
				}
				else
				{
                    targetUserId = targetRecord.UserId;
                    targetIp = targetRecord.Ip;
                    Plugin.Info("Found target's record by user ID: " + targetRecord.NameTracking.LastValue + " (" + targetRecord.UserId + " - " + targetRecord.Ip + ")");
				}
			}
			else if (IPAddress.TryParse(target, out var addressValue))
			{
				if (!PlayerDataRecorder.TryQuery(addressValue.ToString(), queryNick: false, out var targetRecord))
				{
                    targetIp = addressValue.ToString();
					Plugin.Warn($"Failed to find target's record by IP: {addressValue}");
				}
				else {
                    targetUserId = targetRecord.UserId;
                    targetIp = targetRecord.Ip;
                    Plugin.Info("Found target's record by IP: " + targetRecord.NameTracking.LastValue + " (" + targetRecord.UserId + " - " + targetRecord.Ip + ")");
				}
			}
			else if (PlayerDataRecorder.TryQuery(target, queryNick: true, out var targetRecord))
				{
                targetUserId = targetRecord.UserId;
                targetIp = targetRecord.Ip;
                Plugin.Info("Found target's record by Nickname: " + targetRecord.NameTracking.LastValue + " (" + targetRecord.UserId + " - " + targetRecord.Ip + ")");
            }


            UnbanResult unbanResult = 0;
            if (removeIdBans) {
				bool idBanRemoved = false;
				if (targetUserId != null) {
					int bans = FileManager.ReadAllLines(BanHandler.GetPath(BanHandler.BanType.UserId)).Length;
					BanHandler.RemoveBan(targetUserId, BanHandler.BanType.UserId, true);
					if (bans > FileManager.ReadAllLines(BanHandler.GetPath(BanHandler.BanType.UserId)).Length)
						idBanRemoved = true;
				}
				unbanResult |= idBanRemoved ? UnbanResult.IdRemoved : UnbanResult.IdNotBanned;
            }

            if (removeIpBans) {
                bool ipBanRemoved = false;
				if (targetIp != null) {
					int bans = FileManager.ReadAllLines(BanHandler.GetPath(BanHandler.BanType.IP)).Length;
					BanHandler.RemoveBan(targetIp, BanHandler.BanType.IP, true);
					if (bans > FileManager.ReadAllLines(BanHandler.GetPath(BanHandler.BanType.IP)).Length)
						ipBanRemoved = true;
				}
                unbanResult |= ipBanRemoved ? UnbanResult.IpRemoved : UnbanResult.IpNotBanned;
            }

			return unbanResult;

			/*
            BanInfo[] array = ReadBans(BanType.Ip);
			BanInfo[] array2 = ReadBans(BanType.Id);
			Plugin.Info($"Read {array.Length} active IP ban(s).");
			Plugin.Info($"Read {array2.Length} active ID ban(s).");
			int num = array.Length;
			int num2 = array2.Length;
			bool flag4 = false;
			bool flag5 = false;
			if (flag3)
			{
				flag4 = array.Any((BanInfo b) => b.TargetId == targetRecord.Ip);
				flag5 = array2.Any((BanInfo b) => b.TargetId == targetRecord.UserId);
				if (flag4)
				{
					Plugin.Info("Found an active IP ban on target: " + target);
				}
				if (flag5)
				{
					Plugin.Info("Found an active ID ban on target: " + target);
				}
				if (removeIpBans || flag2)
				{
					IEnumerable<BanInfo> enumerable = array.Where((BanInfo b) => b.TargetId == targetRecord.Ip);
					foreach (BanInfo item in enumerable)
					{
						BanHelper.OnUnbanned?.Invoke(item);
					}
					array = array.Where((BanInfo b) => b.TargetId != targetRecord.Ip).ToArray();
				}
				if (removeIdBans || flag)
				{
					IEnumerable<BanInfo> enumerable2 = array2.Where((BanInfo b) => b.TargetId == targetRecord.UserId);
					foreach (BanInfo item2 in enumerable2)
					{
						BanHelper.OnUnbanned?.Invoke(item2);
					}
					array2 = array2.Where((BanInfo b) => b.TargetId != targetRecord.UserId).ToArray();
				}
			}
			else
			{
				flag4 = array.Any((BanInfo b) => b.TargetId == target);
				flag5 = array2.Any((BanInfo b) => b.TargetId == target);
				if (flag4)
				{
					Plugin.Info("Found an active IP ban on target: " + target);
				}
				if (flag5)
				{
					Plugin.Info("Found an active ID ban on target: " + target);
				}
				if (removeIpBans || flag2)
				{
					IEnumerable<BanInfo> enumerable3 = array.Where((BanInfo b) => b.TargetId == target);
					foreach (BanInfo item3 in enumerable3)
					{
						BanHelper.OnUnbanned?.Invoke(item3);
					}
					array = array.Where((BanInfo b) => b.TargetId != target).ToArray();
				}
				if (removeIdBans || flag)
				{
					IEnumerable<BanInfo> enumerable4 = array.Where((BanInfo b) => b.TargetId == target);
					foreach (BanInfo item4 in enumerable4)
					{
						BanHelper.OnUnbanned?.Invoke(item4);
					}
					array2 = array2.Where((BanInfo b) => b.TargetId != target).ToArray();
				}
			}
			WriteBans(array2, BanType.Id);
			WriteBans(array, BanType.Ip);
			if ((!flag5 && !flag4) || (num == array.Length && num2 == array2.Length))
			{
				Plugin.Info("The targeted user (" + target + ") has not been banned.");
				return UnbanResult.NotBanned;
			}
			UnbanResult unbanResult = UnbanResult.None;
			if (!flag5)
			{
				unbanResult |= UnbanResult.IdNotBanned;
			}
			if (!flag4)
			{
				unbanResult |= UnbanResult.IpNotBanned;
			}
			if (array.Length != num)
			{
				unbanResult |= UnbanResult.IpRemoved;
				Plugin.Info("Removed IP ban for target '" + target + "'");
				EventManager.ExecuteEvent(new BanRevokedEvent(flag3 ? targetRecord.Ip : target, BanHandler.BanType.IP));
			}
			else
			{
				Plugin.Warn("Failed to remove IP ban for target '" + target + "'");
			}
			if (array2.Length != num2)
			{
				unbanResult |= UnbanResult.IdRemoved;
				Plugin.Info("Removed ID ban for target '" + target + "'");
				EventManager.ExecuteEvent(new BanRevokedEvent(flag3 ? targetRecord.UserId : target, BanHandler.BanType.UserId));
			}
			else
			{
				Plugin.Warn("Failed to remove ID ban for target '" + target + "'");
			}
			if (unbanResult != UnbanResult.None)
			{
				unbanResult &= ~UnbanResult.None;
			}
			return unbanResult;
			*/
		}
		catch (Exception message)
		{
			Plugin.Error(message);
			return UnbanResult.Failed;
		}
	}

	/*
	public static BanInfo[] ReadBans(BanType includedTypes)
	{
		List<BanInfo> list = ListPool<BanInfo>.Pool.Get();
		if ((includedTypes & BanType.Id) != 0)
		{
			string path = BanHandler.GetPath(BanHandler.BanType.UserId);
			if (!FileManager.FileExists(path))
			{
				BanInfo[] result = list.ToArray();
				ListPool<BanInfo>.Pool.Push(list);
				return result;
			}
			ProcessBanLines(FileManager.ReadAllLines(path), BanType.Id, list);
		}
		if ((includedTypes & BanType.Ip) != 0)
		{
			string path2 = BanHandler.GetPath(BanHandler.BanType.IP);
			if (!FileManager.FileExists(path2))
			{
				BanInfo[] result2 = list.ToArray();
				ListPool<BanInfo>.Pool.Push(list);
				return result2;
			}
			ProcessBanLines(FileManager.ReadAllLines(path2), BanType.Ip, list);
		}
		BanInfo[] result3 = list.ToArray();
		ListPool<BanInfo>.Pool.Push(list);
		return result3;
	}

	public static void WriteBans(IEnumerable<BanInfo> bans, BanType type)
	{
		Plugin.Info($"Saving {bans.Count()} {type} bans ..");
		string path = BanHandler.GetPath((type != BanType.Id) ? BanHandler.BanType.IP : BanHandler.BanType.UserId);
		List<string> list = ListPool<string>.Pool.Get();
		foreach (BanInfo ban in bans)
		{
			string text = $"{ban.TargetName};{ban.TargetId};{ban.ExpiresAt.Ticks};{ban.Reason};%issuerStr%;{ban.IssuedAt.Ticks}";
			text = !ban.IsParsed ? text.Replace("%issuerStr%", ban.IssuerName ?? ban.IssuerId) : text.Replace("%issuerStr%", ban.IssuerName + " (" + ban.IssuerId + ")");
			list.Add(text);
		}
		FileManager.WriteToFileSafe(list, path, removeempty: true);
		BanHandler.ValidateBans((type != BanType.Id) ? BanHandler.BanType.IP : BanHandler.BanType.UserId);
		ListPool<string>.Pool.Push(list);
	}

	public static void ProcessBanLines(string[] fileLines, BanType type, IList<BanInfo> target)
	{
		for (int i = 0; i < fileLines.Length; i++)
		{
			if (string.IsNullOrWhiteSpace(fileLines[i]) || !fileLines[i].Contains(";"))
			{
				continue;
			}
			string[] array = fileLines[i].Split(';');
			if (array.Length != 6)
			{
				continue;
			}
			string targetName = array[0].Trim();
			string targetId = array[1].Trim();
			string s = array[2].Trim();
			string reason = array[3].Trim();
			string issuerString = array[4].Trim();
			string s2 = array[5].Trim();
			if (long.TryParse(s, out var result) && long.TryParse(s2, out var result2))
			{
				BanInfo banInfo = BanInfo.Get(result, result2, issuerString, targetId, targetName, reason, type);
				if (banInfo != null)
				{
					target.Add(banInfo);
				}
			}
		}
	}
	*/
}
