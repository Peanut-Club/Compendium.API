using System;
using System.Collections.Generic;
using System.Linq;
using Compendium.Events;
using Compendium.Generation;
using Compendium.IO.Saving;
using Compendium.PlayerData;
using Compendium.Updating;
using helpers;
using helpers.Attributes;
using helpers.Pooling.Pools;
using PluginAPI.Events;
using VoiceChat;

namespace Compendium.Mutes;

public static class MuteManager
{
	private static SaveFile<CollectionSaveData<Mute>> Mutes;

	private static SaveFile<CollectionSaveData<Mute>> History;

	private static object LockObject = new object();

	public static event Action<Mute> OnExpired;

	public static event Action<Mute> OnIssued;

	[Load]
	public static void Load()
	{
		if (Mutes == null)
		{
			Mutes = new SaveFile<CollectionSaveData<Mute>>(Directories.GetDataPath("SavedMutes", "mutes"));
		}
		if (History == null)
		{
			History = new SaveFile<CollectionSaveData<Mute>>(Directories.GetDataPath("SavedMuteHistory", "muteHistory"));
		}
		OnExpired += delegate(Mute m)
		{
			Plugin.Info("Mute '" + m.Id + "' (" + m.IssuerId + " -> " + m.TargetId + ") for '" + m.Reason + "' (at " + new DateTime(m.IssuedAt).ToString("G") + ") has expired.");
		};
		OnIssued += delegate(Mute m)
		{
			Plugin.Info("Mute '" + m.Id + "' (" + m.IssuerId + " -> " + m.TargetId + ") for '" + m.Reason + "' (at " + new DateTime(m.IssuedAt).ToString("G") + ") has been issued.");
		};
	}

	public static bool Remove(Mute mute)
	{
		lock (LockObject)
		{
			if (!Mutes.Data.Contains(mute))
			{
				return false;
			}
			Mutes.Data.Remove(mute);
			Mutes.Save();
			History.Data.Add(mute);
			History.Save();
			MuteManager.OnExpired?.Invoke(mute);
			return true;
		}
	}

	public static bool RemoveAll(ReferenceHub target)
	{
		lock (LockObject)
		{
			if (!Mutes.Data.Any((Mute m) => m.TargetId == target.UserId()))
			{
				return false;
			}
			IEnumerable<Mute> enumerable = Mutes.Data.Where((Mute m) => m.TargetId == target.UserId());
			foreach (Mute item in enumerable)
			{
				Mutes.Data.Remove(item);
				Mutes.Save();
				History.Data.Add(item);
				History.Save();
				MuteManager.OnExpired?.Invoke(item);
			}
			return true;
		}
	}

	public static Mute Query(string id)
	{
		return Mutes.Data.FirstOrDefault((Mute m) => m.Id == id);
	}

	public static Mute[] Query(ReferenceHub target)
	{
		return Mutes.Data.Where((Mute m) => m.TargetId == target.UserId()).ToArray();
	}

	public static Mute[] Query(PlayerDataRecord record)
	{
		return Mutes.Data.Where((Mute m) => m.TargetId == record.UserId).ToArray();
	}

	public static Mute[] QueryHistory(ReferenceHub target)
	{
		return History.Data.Where((Mute m) => m.TargetId == target.UserId()).ToArray();
	}

	public static Mute[] QueryHistory(PlayerDataRecord record)
	{
		return History.Data.Where((Mute m) => m.TargetId == record.UserId).ToArray();
	}

	public static Mute[] QueryIssued(ReferenceHub issuer)
	{
		return Mutes.Data.Where((Mute m) => m.IssuerId == issuer.UserId()).Concat(History.Data.Where((Mute m) => m.IssuerId == issuer.UserId())).ToArray();
	}

	public static Mute[] QueryIssued(PlayerDataRecord record)
	{
		return Mutes.Data.Where((Mute m) => m.IssuerId == record.UserId).Concat(History.Data.Where((Mute m) => m.IssuerId == record.UserId)).ToArray();
	}

	public static Mute[] QueryAll()
	{
		return Mutes.Data.ToArray();
	}

	public static Mute[] QueryHistory()
	{
		return History.Data.ToArray();
	}

	public static Mute[] QueryAllWithHistory()
	{
		return Mutes.Data.Concat(History.Data).ToArray();
	}

	public static bool Issue(ReferenceHub issuer, ReferenceHub target, string reason, TimeSpan duration)
	{
		if (duration.TotalMilliseconds <= 0.0)
		{
			return false;
		}
		if ((object)target == null)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(reason))
		{
			reason = "Not specified";
		}
		if (issuer == null)
		{
			issuer = ReferenceHub.HostHub;
		}
		Mute mute = new Mute
		{
			Id = UniqueIdGeneration.Generate(3),
			ExpiresAt = (DateTime.Now + duration).Ticks,
			IssuedAt = DateTime.Now.Ticks,
			IssuerId = issuer.UserId(),
			TargetId = target.UserId(),
			Reason = reason
		};
		lock (LockObject)
		{
			Mutes.Data.Add(mute);
			Mutes.Save();
		}
		VoiceChatMutes.SetFlags(target, VcMuteFlags.LocalRegular | VcMuteFlags.LocalIntercom);
		MuteManager.OnIssued?.Invoke(mute);
		return true;
	}

	public static bool Issue(ReferenceHub issuer, PlayerDataRecord target, string reason, TimeSpan duration)
	{
		if (duration.TotalMilliseconds <= 0.0)
		{
			return false;
		}
		if (target == null)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(reason))
		{
			reason = "Not specified";
		}
		if (issuer == null)
		{
			issuer = ReferenceHub.HostHub;
		}
		Mute mute = new Mute
		{
			Id = UniqueIdGeneration.Generate(3),
			ExpiresAt = (DateTime.Now + duration).Ticks,
			IssuedAt = DateTime.Now.Ticks,
			IssuerId = issuer.UserId(),
			TargetId = target.UserId,
			Reason = reason
		};
		lock (LockObject)
		{
			Mutes.Data.Add(mute);
			Mutes.Save();
		}
		MuteManager.OnIssued?.Invoke(mute);
		if (target.TryGetHub(out var hub))
		{
			VoiceChatMutes.SetFlags(hub, VcMuteFlags.LocalRegular | VcMuteFlags.LocalIntercom);
		}
		return true;
	}

	[Update(Delay = 1000)]
	private static void Update()
	{
		if (Mutes == null || History == null)
		{
			return;
		}
		lock (LockObject)
		{
			List<Mute> list = ListPool<Mute>.Pool.Get();
			for (int i = 0; i < Mutes.Data.Count; i++)
			{
				if (Mutes.Data[i].IsExpired())
				{
					list.Add(Mutes.Data[i]);
				}
			}
			if (list.Count > 0)
			{
				for (int j = 0; j < list.Count; j++)
				{
					Mutes.Data.Remove(list[j]);
					History.Data.Add(list[j]);
					MuteManager.OnExpired?.Invoke(list[j]);
					Mutes.Save();
					History.Save();
				}
				for (int k = 0; k < list.Count; k++)
				{
					if (Hub.TryGetHub(list[k].TargetId, out var hub) && Query(hub).Length == 0)
					{
						VoiceChatMutes.SetFlags(hub, VcMuteFlags.None);
					}
				}
			}
			list.ReturnList();
		}
	}

	[Event]
	private static void OnPlayerJoined(PlayerJoinedEvent ev)
	{
		if (Query(ev.Player.ReferenceHub).Length != 0)
		{
			VoiceChatMutes.SetFlags(ev.Player.ReferenceHub, VcMuteFlags.LocalRegular | VcMuteFlags.LocalIntercom);
		}
	}
}
