using System.Collections.Generic;
using System.Linq;
using System.Text;
using BetterCommands;
using Compendium.Constants;
using Compendium.Generation;
using Compendium.IO.Saving;
using Compendium.PlayerData;
using helpers;
using helpers.Attributes;
using helpers.Events;
using helpers.Extensions;
using helpers.Pooling.Pools;
using helpers.Time;

namespace Compendium.Warns;

public static class WarnSystem
{
	private static SaveFile<CollectionSaveData<WarnData>> _warnStorage;

	public static IReadOnlyCollection<WarnData> Warns => _warnStorage.Data.Value;

	public static EventProvider OnWarnIssued { get; } = new EventProvider();


	public static EventProvider OnWarnRemoved { get; } = new EventProvider();


	[Load]
	[Reload]
	public static void Load()
	{
		if (_warnStorage != null)
		{
			_warnStorage.Load();
			return;
		}
		if (_warnStorage == null)
		{
			_warnStorage = new SaveFile<CollectionSaveData<WarnData>>(Directories.GetDataPath("SavedWarns", "warns"));
		}
		Plugin.Info("Warn System loaded.");
	}

	[Unload]
	public static void Unload()
	{
		if (_warnStorage != null)
		{
			_warnStorage.Save();
		}
		_warnStorage = null;
		Plugin.Info("Warn System unloaded.");
	}

	public static WarnData[] ListIssuedWarns(PlayerDataRecord target, string filter = null)
	{
		WarnData[] source = Query(filter);
		if (!source.Any())
		{
			return null;
		}
		return source.Where((WarnData q) => q.Issuer == target.Id).ToArray();
	}

	public static WarnData[] ListReceivedWarns(PlayerDataRecord target, string filter = null)
	{
		WarnData[] source = Query(filter);
		if (!source.Any())
		{
			return null;
		}
		return source.Where((WarnData q) => q.Target == target.Id).ToArray();
	}

	public static WarnData[] Query(string filter = null)
	{
		if (filter == null || filter == "*")
		{
			return Warns.ToArray();
		}
		return Warns.Where((WarnData w) => w.Reason.ToLower().Contains(filter.ToLower()) || w.Reason.Split(new char[1] { ' ' }).Any((string x) => x.ToLowerInvariant().GetSimilarity(filter.ToLowerInvariant()) >= 0.8)).ToArray();
	}

	public static bool Remove(string id)
	{
		List<WarnData> list = ListPool<WarnData>.Pool.Get();
		foreach (WarnData warn in Warns)
		{
			if (warn.Id == id)
			{
				list.Add(warn);
			}
		}
		if (!list.Any())
		{
			ListPool<WarnData>.Pool.Push(list);
			return false;
		}
		list.ForEach(delegate(WarnData w)
		{
			_warnStorage.Data.Remove(w);
		});
		_warnStorage.Save();
		ListPool<WarnData>.Pool.Push(list);
		return true;
	}

	public static WarnData Issue(PlayerDataRecord issuer, PlayerDataRecord target, string reason)
	{
		WarnData warnData = new WarnData
		{
			Id = UniqueIdGeneration.Generate(7),
			IssuedAt = TimeUtils.LocalTime,
			Issuer = ((issuer == null) ? "Server" : issuer.Id),
			Target = target.Id,
			Reason = reason
		};
		_warnStorage.Data.Add(warnData);
		_warnStorage.Save();
		OnWarnIssued.Invoke(warnData, issuer, target);
		if (Plugin.Config.WarnSettings.Announce)
		{
			issuer.TryInvokeHub(delegate(ReferenceHub issuerHub)
			{
				issuerHub.Hint(Colors.LightGreen("<b>Hráči <color=#FF0000>" + target.NameTracking.LastValue + "</color> bylo uděleno varování</b>\n<color=#90FF33>" + reason + "</color>"), 10f);
			});
			target.TryInvokeHub(delegate(ReferenceHub targetHub)
			{
				targetHub.Broadcast(Colors.LightGreen("<b>Obdržel jsi varování!</b>\n<b><color=#FF0000>" + reason + "</color>"), 10);
			});
		}
		return warnData;
	}

	[Command("warns", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Retrieves a list of warns for a specific player.")]
	private static string ListWarnsCommand(ReferenceHub sender, PlayerDataRecord target, string filter = "*")
	{
		WarnData[] array = ListReceivedWarns(target, filter);
		if (array == null || !array.Any())
		{
			return "There aren't any warns matching your search.";
		}
		array = array.OrderBy((WarnData w) => TimeUtils.LocalTime - w.IssuedAt).ToArray();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"Found {array.Length} warn(s):");
		array.For(delegate(int i, WarnData w)
		{
			string text = "Unknown Issuer";
			PlayerDataRecord record;
			if (w.Issuer == "Server")
			{
				text = "Server";
			}
			else if (PlayerDataRecorder.TryQuery(w.Issuer, queryNick: false, out record) && record.NameTracking.LastValue != null)
			{
				text = record.NameTracking.LastValue;
			}
			sb.AppendLine(string.Format("[{0}] {1}: {2} [{3}] ({4})", i + 1, w.Id, w.Reason, text, w.IssuedAt.ToString("F")));
		});
		return sb.ToString();
	}

	[Command("warn", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Issues a warn.")]
	public static string IssueWarnCommand(ReferenceHub sender, PlayerDataRecord target, string reason)
	{
		WarnData warnData = Issue(PlayerDataRecorder.GetData(sender), target, reason);
		if (warnData == null)
		{
			return "Failed to issue that warn.";
		}
		return "Issued warn with ID " + warnData.Id + " and reason " + warnData.Reason + " to " + target.NameTracking.LastValue;
	}

	[Command("delwarn", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Deletes a warn.")]
	public static string RemoveWarnCommand(ReferenceHub sender, string id)
	{
		if (!Remove(id))
		{
			return "Failed to find a warn with ID " + id;
		}
		return "Removed warn with ID " + id;
	}
}
