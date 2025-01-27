using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BetterCommands;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.IO.Saving;
using Compendium.Npc;
using helpers;
using helpers.Attributes;
using helpers.Pooling;
using PlayerRoles;
using UnityEngine;
using Utils.NonAllocLINQ;
using VoiceChat;
using Xabe.FFmpeg;

namespace Compendium.Sounds;

public static class Audio
{
	internal static readonly HashSet<AudioPlayer> _activePlayers = new HashSet<AudioPlayer>();

	internal static readonly Dictionary<ReferenceHub, AudioPlayer> _ownedPlayers = new Dictionary<ReferenceHub, AudioPlayer>();

	internal static SaveFile<SimpleSaveData<Dictionary<string, HashSet<string>>>> _mutes;

	public static Dictionary<string, HashSet<string>> Mutes
	{
		get
		{
			if (_mutes.Data.Value == null)
			{
				_mutes.Data.Value = new Dictionary<string, HashSet<string>>();
				_mutes.Save();
			}
			return _mutes.Data.Value;
		}
	}

	[Load]
	[Reload]
	private static void Load()
	{
		if (_mutes != null)
		{
			_mutes.Load();
			return;
		}
		_mutes = new SaveFile<SimpleSaveData<Dictionary<string, HashSet<string>>>>(Directories.GetDataPath("SavedAudioMutes", "audioMutes"));
		FFmpeg.SetExecutablesPath(AudioStore.DirectoryPath);
	}

	[Unload]
	private static void Unload()
	{
		if (_mutes != null)
		{
			_mutes.Save();
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.Ending })]
	private static void OnRoundEnd()
	{
		_ownedPlayers.ForEachValue(delegate(AudioPlayer pl)
		{
			PoolablePool.Push(pl);
		});
		_ownedPlayers.Clear();
		_activePlayers.Clear();
	}

	public static AudioPlayer Play(string id, Vector3 position, string name = "default")
	{
		AudioPlayer pooledPlayer = PoolablePool.Get<AudioPlayer>();
		Action target = delegate
		{
			PoolablePool.Push(pooledPlayer);
		};
		pooledPlayer._speaker = NpcHub.Spawn("Audio", RoleTypeId.Tutorial, position).Hub;
		pooledPlayer.Name = name;
		pooledPlayer.Channel = VoiceChatChannel.Proximity;
		pooledPlayer.ChannelMode = VoiceChatChannel.None;
		pooledPlayer.Position.Value = position;
		pooledPlayer.OnFinishedTrack.Register(target);
		Calls.Delay(0.2f, delegate
		{
			pooledPlayer._speaker.SetSize(Vector3.zero);
			pooledPlayer.Queue(id, Plugin.Info);
		});
		return pooledPlayer;
	}

	public static AudioPlayer PlayTo(ReferenceHub hub, string id, Vector3 position, string name = "default")
	{
		AudioPlayer pooledPlayer = PoolablePool.Get<AudioPlayer>();
		Action target = delegate
		{
			PoolablePool.Push(pooledPlayer);
		};
		pooledPlayer._speaker = NpcHub.Spawn("Audio", RoleTypeId.Tutorial, position).Hub;
		pooledPlayer.Name = name;
		pooledPlayer.Channel = VoiceChatChannel.Proximity;
		pooledPlayer.ChannelMode = VoiceChatChannel.None;
		pooledPlayer.Position.Value = position;
		pooledPlayer.OnFinishedTrack.Register(target);
		pooledPlayer.AddWhitelist(hub);
		Calls.Delay(0.2f, delegate
		{
			pooledPlayer._speaker.SetSize(Vector3.zero);
			pooledPlayer.Queue(id, Plugin.Info);
		});
		return pooledPlayer;
	}

	public static AudioPlayer PlayVia(ReferenceHub hub, bool sendSelf, string id, Vector3 position, string name = "default")
	{
		AudioPlayer pooledPlayer = PoolablePool.Get<AudioPlayer>();
		Action target = delegate
		{
			PoolablePool.Push(pooledPlayer);
		};
		pooledPlayer._speaker = hub;
		pooledPlayer.SendToSelf = sendSelf;
		pooledPlayer.Name = name;
		pooledPlayer.Channel = VoiceChatChannel.Proximity;
		pooledPlayer.ChannelMode = VoiceChatChannel.None;
		pooledPlayer.Position.Value = position;
		pooledPlayer.OnFinishedTrack.Register(target);
		Calls.Delay(0.2f, delegate
		{
			pooledPlayer.Queue(id, Plugin.Info);
		});
		return pooledPlayer;
	}

	public static AudioPlayer PlayToVia(ReferenceHub hub, ReferenceHub target, bool sendSelf, string id, Vector3 position, string name = "default")
	{
		AudioPlayer pooledPlayer = PoolablePool.Get<AudioPlayer>();
		Action target2 = delegate
		{
			PoolablePool.Push(pooledPlayer);
		};
		pooledPlayer._speaker = hub;
		pooledPlayer.SendToSelf = sendSelf;
		pooledPlayer.Name = name;
		pooledPlayer.Channel = VoiceChatChannel.Proximity;
		pooledPlayer.ChannelMode = VoiceChatChannel.None;
		pooledPlayer.Position.Value = position;
		pooledPlayer.OnFinishedTrack.Register(target2);
		pooledPlayer.AddWhitelist(target);
		Calls.Delay(0.2f, delegate
		{
			pooledPlayer.Queue(id, Plugin.Info);
		});
		return pooledPlayer;
	}

	[Command("instantplay", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "iplay" })]
	[Description("Uses a pooled player to instantly play your request.")]
	private static string InstantPlay(ReferenceHub sender, string query)
	{
		Play(query, sender.Position());
		return "Done.";
	}

	[Command("instanttargetplay", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "itplay" })]
	[Description("Uses a pooled player to instantly play your request at a targeted player.")]
	private static string InstantTargetPlay(ReferenceHub sender, ReferenceHub target, string query)
	{
		Play(query, target.Position());
		return "Done.";
	}

	[Command("play", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Starts playing a song!")]
	private static string PlayCommand(ReferenceHub sender, string query)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value.Queue(query, Plugin.Info);
		return "Request queued.";
	}

	[Command("volume", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Allows you to manage volume of your audio player.")]
	private static string VolumeCommand(ReferenceHub sender, float volume)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value.Volume = volume;
		return $"Volume set to {volume}";
	}

	[Command("pause", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Pauses playback of your audio player.")]
	private static string PauseCommand(ReferenceHub sender)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			return "You don't have any active audio players.";
		}
		if (value.IsPaused)
		{
			return "Your audio player is already paused!";
		}
		value.IsPaused = true;
		return "Paused your audio player.";
	}

	[Command("resume", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Resumes playback of your audio player.")]
	private static string ResumeCommand(ReferenceHub sender)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			return "You don't have any active audio players.";
		}
		if (!value.IsPaused)
		{
			return "Your audio player is not paused!";
		}
		value.IsPaused = false;
		return "Resumed your audio player.";
	}

	[Command("discard", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Discards your audio player.")]
	private static string DiscardCommand(ReferenceHub sender)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			return "You don't have any active audio players.";
		}
		_ownedPlayers.Remove(sender);
		PoolablePool.Push(value);
		return "Discarded your audio player.";
	}

	[Command("stop", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Stops your audio player.")]
	private static string StopCommand(ReferenceHub sender)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			return "You don't have any active audio players.";
		}
		value.Stop();
		return "Stopped your audio player.";
	}

	[Command("channel", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Changes the playback channel of your audio player.")]
	private static string ChannelCommand(ReferenceHub sender, VoiceChatChannel channel)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value.Channel = channel;
		return $"Set channel of your audio player to {value.Channel}";
	}

	[Command("channelmode", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Changes the playback channel's mode of your audio player.")]
	private static string ChannelModeCommand(ReferenceHub sender, VoiceChatChannel channel)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value.ChannelMode = channel;
		return $"Set channel mode of your audio player to {value.Channel}";
	}

	[Command("loop", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Changes the loop mode of your audio player.")]
	private static string LoopCommand(ReferenceHub sender)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value.IsLooping = !value.IsLooping;
		if (!value.IsLooping)
		{
			return "Looping disabled.";
		}
		return "Looping enabled.";
	}

	[Command("audiowhitelist", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "audiow", "awh" })]
	[Description("Adds/removes a player from your audio player's whitelist.")]
	private static string WhitelistCommand(ReferenceHub sender, ReferenceHub target)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		if (value.IsWhitelisted(target))
		{
			value.RemoveWhitelist(target);
			return "Removed whitelist for " + target.Nick();
		}
		value.AddWhitelist(target);
		return "Added whitelist for " + target.Nick();
	}

	[Command("audioblacklist", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "audiobl", "abl" })]
	[Description("Adds/removes a player from your audio player's blacklist.")]
	private static string BlacklistCommand(ReferenceHub sender, ReferenceHub target)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		if (value.IsBlacklisted(target))
		{
			value.RemoveBlacklist(target);
			return "Removed blacklist for " + target.Nick();
		}
		value.AddBlacklist(target);
		return "Added blacklist for " + target.Nick();
	}

	[Command("audiosource", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "asource" })]
	[Description("Sets the audio source of your audio player.")]
	private static string SourceCommand(ReferenceHub sender, ReferenceHub target)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value._speaker = target;
		return "Set audio source to " + target.Nick();
	}

	[Command("audiohostsource", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "ahsource" })]
	[Description("Sets the audio source of your audio player to the host player.")]
	private static string HostSourceCommand(ReferenceHub sender)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		value._speaker = ReferenceHub.HostHub;
		return "Set audio source to the host player.";
	}

	[Command("audionpcsource", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "anpcsource" })]
	[Description("Sets the audio source of your audio player to an NPC.")]
	private static string NpcSourceCommand(ReferenceHub sender, int npcId)
	{
		if (!_ownedPlayers.TryGetValue(sender, out var value))
		{
			sender.Message("You don't have any active audio players .. hold on.", isRemoteAdmin: true);
			AudioPlayer audioPlayer2 = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
			AudioPlayer audioPlayer3 = audioPlayer2;
			value = audioPlayer3;
			value.Name = sender.Nick() + "'s audio player";
			value._speaker = sender;
			sender.Message("Created a new audio player.", isRemoteAdmin: true);
		}
		if (!NpcHub.TryGetNpc(npcId, out var npc))
		{
			return $"Failed to find an NPC with ID '{npcId}'";
		}
		value._speaker = npc.Hub;
		return "Set audio source to the NPC (" + npc.Name + ") player.";
	}

	[Command("download", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "adown", "audiod" })]
	[Description("Downloads an audio file.")]
	private static string DownloadCommand(ReferenceHub sender, string query, string id)
	{
		if (query.StartsWith("http"))
		{
			if (query.Contains("yt") || query.Contains("youtu"))
			{
				AudioUtils.Download(query, id, isDirect: false);
				return "Searching .. (YouTube - direct)";
			}
			AudioUtils.Download(query, id, isDirect: true);
			return "Downloading .. (other)";
		}
		AudioUtils.Download(query, id, isDirect: false);
		return "Searching .. (YouTube - search)";
	}

	[Command("audiomute", new CommandType[] { CommandType.PlayerConsole })]
	[CommandAliases(new object[] { "amute" })]
	[Description("Mutes audio from the selected source.")]
	private static string AudioMuteCommand(ReferenceHub hub, string source)
	{
		if (Mutes.TryGetValue(hub.UserId(), out var value))
		{
			if (value.Contains(source))
			{
				value.Remove(source);
				_mutes.Save();
				return "Unmuted source '" + source + "'";
			}
			value.Add(source);
			_mutes.Save();
			return "Muted source '" + source + "'";
		}
		Mutes[hub.UserId()] = new HashSet<string> { source };
		_mutes.Save();
		return "Muted source '" + source + "'";
	}

	[Command("audiosources", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole,
		CommandType.PlayerConsole
	})]
	private static string ListAudioSourcesCommand(ReferenceHub sender)
	{
		StringBuilder sb = new StringBuilder();
		if (!_activePlayers.Any())
		{
			sb.AppendLine("There aren't any active sources.");
		}
		else
		{
			_activePlayers.For(delegate(int i, AudioPlayer source)
			{
				sb.AppendLine($"[{i + 1}] {source.Name} ({source.Status})");
			});
		}
		return sb.ToString();
	}
}
