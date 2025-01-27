using System;
using System.Collections.Generic;
using System.Linq;
using BetterCommands;
using BetterCommands.Permissions;
using helpers.Attributes;
using helpers.Patching;
using PluginAPI.Events;

namespace Compendium;

public static class BanHistory
{
	/*
	public static readonly List<BanHelper.BanInfo> History;

	public static event Action<BanHelper.BanInfo> OnRevoked;

	static BanHistory()
	{
		History = new List<BanHelper.BanInfo>();
		BanHelper.OnUnbanned += OnCustomUnbanned;
		OnRevoked += delegate(BanHelper.BanInfo ban)
		{
			try
			{
				Plugin.Info("\n== Ban Revoked ==\n- Target: " + ban.TargetName + " (" + ban.Type.ToString().ToUpper() + " - " + ban.TargetId + ")\n- Issuer: " + (ban.IsParsed ? (ban.IssuerName + " (" + ban.IssuerId + ")") : (ban.IssuerId ?? ban.IssuerName)) + "\n- Reason: " + ban.Reason + "\n- Issued At: " + ban.IssuedAt.ToString("G") + "\n- Expires At: " + ban.ExpiresAt.ToString("G") + "\n" + $"- Is Forced: {DateTime.Now < ban.ExpiresAt}");
			}
			catch
			{
			}
		};
	}

	[Load]
	public static void Load()
	{
		History.Clear();
		History.AddRange(Directories.GetData("BanHistory.json", "banHistory", useGlobal: true, History));
	}

	public static void Save()
	{
		Directories.SetData("BanHistory.json", "banHistory", useGlobal: true, History);
	}

	private static void OnCustomUnbanned(BanHelper.BanInfo removed)
	{
		History.Add(removed);
		Save();
		BanHistory.OnRevoked?.Invoke(removed);
	}

	[Patch(typeof(BanHandler), "RemoveBan", PatchType.Prefix, new Type[] { })]
	private static bool OnRemoving(string id, BanHandler.BanType banType, bool forced)
	{
		if (!EventManager.ExecuteEvent(new BanRevokedEvent(id, banType)) && !forced)
		{
			return false;
		}
		KeyValuePair<BanDetails, BanDetails> keyValuePair = BanHandler.QueryBan(id, id);
		try
		{
			if (keyValuePair.Key != null && keyValuePair.Value != null)
			{
				BanDetails key = keyValuePair.Key;
				BanDetails value = keyValuePair.Value;
				if (key != null)
				{
					BanHelper.BanInfo banInfo = BanHelper.BanInfo.Get(key.Expires, key.IssuanceTime, key.Issuer, key.Id, key.OriginalName, key.Reason, BanHelper.BanType.Id);
					History.Add(banInfo);
					Save();
					BanHistory.OnRevoked?.Invoke(banInfo);
				}
				if (value != null)
				{
					BanHelper.BanInfo banInfo2 = BanHelper.BanInfo.Get(key.Expires, value.IssuanceTime, value.Issuer, value.Id, value.OriginalName, value.Reason, BanHelper.BanType.Ip);
					History.Add(banInfo2);
					Save();
					BanHistory.OnRevoked?.Invoke(banInfo2);
				}
			}
		}
		catch
		{
		}
		id = id.Replace(";", ":").Replace(Environment.NewLine, "").Replace("\n", "");
		FileManager.WriteToFile(FileManager.ReadAllLines(BanHandler.GetPath(banType)).Where(delegate(string b)
		{
			BanDetails banDetails = BanHandler.ProcessBanItem(b, banType);
			return banDetails != null && banDetails.Id != id;
		}), BanHandler.GetPath(banType));
		return false;
	}

	[Command("importbans", new CommandType[]
	{
		CommandType.GameConsole,
		CommandType.RemoteAdmin
	})]
	[Description("Imports all saved bans.")]
	[Permission(PermissionLevel.Administrator)]
	private static string ImportHistoryCommand(ReferenceHub sender)
	{
		History.Clear();
		History.AddRange(BanHelper.ReadBans(BanHelper.BanType.Ip));
		History.AddRange(BanHelper.ReadBans(BanHelper.BanType.Id));
		Save();
		return "History imported.";
	}
	*/
}

