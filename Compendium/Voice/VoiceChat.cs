using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using BetterCommands;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Features;
using Compendium.Updating;
using Compendium.Voice.Pools;
using Compendium.Voice.Prefabs.Scp;
using Compendium.Voice.Profiles.Pitch;
using helpers.Attributes;
using helpers.Patching;
using Mirror;
using PlayerRoles;
using PlayerRoles.Voice;
using PluginAPI.Events;
using Utils.NonAllocLINQ;
using VoiceChat;
using VoiceChat.Codec.Enums;
using VoiceChat.Codec;
using VoiceChat.Networking;

namespace Compendium.Voice;

public static class VoiceChat
{
	private static readonly Dictionary<uint, IVoiceProfile> _activeProfiles = new Dictionary<uint, IVoiceProfile>();

	private static readonly Dictionary<uint, List<VoiceModifier>> _activeModifiers = new Dictionary<uint, List<VoiceModifier>>();

	private static readonly HashSet<uint> _speakCache = new HashSet<uint>();

	private static readonly HashSet<IVoicePrefab> _activePrefabs = new HashSet<IVoicePrefab>();


    private static OpusEncoder Encoder = new OpusEncoder(OpusApplicationType.Voip);

    private static OpusDecoder Decoder = new OpusDecoder();

    private static float[] ampArray = new float[48000];

    public static IReadOnlyCollection<IVoicePrefab> Prefabs => _activePrefabs;

	public static IReadOnlyCollection<IVoiceProfile> Profiles => _activeProfiles.Values;

	public static IVoiceChatState State { get; set; }

	public static event Action<ReferenceHub> OnStartedSpeaking;

	public static event Action<ReferenceHub> OnStoppedSpeaking;

	public static void RegisterPrefab<TPrefab>() where TPrefab : IVoicePrefab, new()
	{
		if (TryGetPrefab<TPrefab>(out var _))
		{
			Plugin.Warn("Tried registering an already existing prefab.");
		}
		else
		{
			_activePrefabs.Add(new TPrefab());
		}
	}

	public static void SetState(ReferenceHub hub, VoiceModifier voiceModifier)
	{
		if (!_activeModifiers.ContainsKey(hub.netId))
		{
			_activeModifiers[hub.netId] = new List<VoiceModifier> { voiceModifier };
		}
		else
		{
			_activeModifiers[hub.netId].Add(voiceModifier);
		}
	}

	public static void RemoveState(ReferenceHub hub, VoiceModifier voiceModifier)
	{
		if (_activeModifiers.ContainsKey(hub.netId))
		{
			_activeModifiers[hub.netId].Remove(voiceModifier);
		}
	}

	public static List<VoiceModifier> GetModifiers(ReferenceHub hub)
	{
		if (!_activeModifiers.TryGetValue(hub.netId, out var value))
		{
			return null;
		}
		return value;
	}

	public static void SetProfile(ReferenceHub hub, IVoiceProfile profile)
	{
		if (profile == null)
		{
			if (_activeProfiles.TryGetValue(hub.netId, out var value))
			{
				value.Disable();
			}
			_activeProfiles.Remove(hub.netId);
		}
		else
		{
			_activeProfiles[hub.netId] = profile;
			profile.Enable();
		}
	}

	public static void SetProfile(ReferenceHub hub, IVoicePrefab prefab)
	{
		SetProfile(hub, prefab.Instantiate(hub));
	}

	public static IVoiceProfile GetProfile(ReferenceHub hub)
	{
		if (!_activeProfiles.TryGetValue(hub.netId, out var value))
		{
			return null;
		}
		return value;
	}

	public static void UnregisterPrefab<TPrefab>() where TPrefab : IVoicePrefab, new()
	{
		_activePrefabs.RemoveWhere((IVoicePrefab p) => p is TPrefab);
	}

	public static bool TryGetAvailableProfile(RoleTypeId role, out IVoicePrefab prefab)
	{
		return _activePrefabs.TryGetFirst((IVoicePrefab p) => p.Roles.Contains(role) || p.Roles.IsEmpty(), out prefab);
	}

	public static bool TryGetPrefab<TPrefab>(out TPrefab prefab) where TPrefab : IVoicePrefab, new()
	{
		if (_activePrefabs.TryGetFirst((IVoicePrefab p) => p is TPrefab, out var first) && first is TPrefab val)
		{
			prefab = val;
			return true;
		}
		prefab = default(TPrefab);
		return false;
	}

	private static bool IsSpeaking(ReferenceHub hub)
	{
		return _speakCache.Contains(hub.netId);
	}

	private static void SetSpeaking(ReferenceHub hub, bool state)
	{
		if (!state)
		{
			_speakCache.Remove(hub.netId);
		}
		else
		{
			_speakCache.Add(hub.netId);
		}
	}

	[Load]
	private static void Load()
	{
		RegisterPrefab<ScpVoicePrefab>();
	}

	[Unload]
	private static void Unload()
	{
		UnregisterPrefab<ScpVoicePrefab>();
		_activeModifiers.Clear();
		_activePrefabs.Clear();
		_activeProfiles.Clear();
		_speakCache.Clear();
		State = null;
		Plugin.Info("Voice Chat system unloaded.");
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		_activeProfiles.Clear();
		_activeModifiers.Clear();
		_speakCache.Clear();
		State = null;
	}

	[Event]
	private static void OnRoleChanged(PlayerChangeRoleEvent ev)
	{
		Calls.Delay(0.5f, delegate
		{
			if (TryGetAvailableProfile(ev.NewRole, out var prefab))
			{
				SetProfile(ev.Player.ReferenceHub, prefab);
			}
			else
			{
				if (_activeProfiles.TryGetValue(ev.Player.NetworkId, out var value))
				{
					if (value.IsPersistent)
					{
						value.OnRoleChanged();
						return;
					}
					value.Disable();
				}
				_activeProfiles.Remove(ev.Player.NetworkId);
			}
		});
	}

	[Update]
	private static void OnUpdate()
	{
		foreach (ReferenceHub hub in Hub.Hubs)
		{
			if (!(hub.Role() is IVoiceRole voiceRole) || (object)voiceRole.VoiceModule == null)
			{
				continue;
			}
			if (voiceRole.VoiceModule.ServerIsSending)
			{
				if (!IsSpeaking(hub))
				{
					SetSpeaking(hub, state: true);
					VoiceChat.OnStartedSpeaking?.Invoke(hub);
				}
			}
			else if (IsSpeaking(hub))
			{
				SetSpeaking(hub, state: false);
				VoiceChat.OnStoppedSpeaking?.Invoke(hub);
			}
		}
	}

	[BetterCommands.Command("playback", new CommandType[] { CommandType.PlayerConsole })]
	[Description("Enables or disables microphone playback.")]
	private static string PlaybackCommand(ReferenceHub sender)
	{
		List<VoiceModifier> modifiers = GetModifiers(sender);
		if (modifiers != null && modifiers.Contains(VoiceModifier.PlaybackEnabled))
		{
			RemoveState(sender, VoiceModifier.PlaybackEnabled);
			return "Disabled playback.";
		}
		SetState(sender, VoiceModifier.PlaybackEnabled);
		return "Enabled playback.";
	}

	[Patch(typeof(VoiceTransceiver), "ServerReceiveMessage")]
	private static bool Patch(NetworkConnection conn, VoiceMessage msg)
	{
		try
		{
			if (msg.SpeakerNull || msg.Speaker.netId != conn.identity.netId)
			{
				return false;
			}
			if (!(msg.Speaker.Role() is IVoiceRole voiceRole))
			{
				return false;
			}
			if (!VoiceChatUtils.CheckRateLimit(voiceRole.VoiceModule))
			{
				return false;
			}
			if (VoiceChatMutes.IsMuted(msg.Speaker))
			{
				return false;
            }
            VoiceChatChannel origChannel = voiceRole.VoiceModule.ValidateSend(msg.Channel);
			IVoiceProfile profile = GetProfile(msg.Speaker);
			VoicePacket packet = VoiceChatUtils.GeneratePacket(msg, voiceRole, origChannel);
			if (State == null || !State.Process(packet)) {
                profile?.Process(packet);
			}
			msg = packet.Message;

            bool speakerIsScp = msg.Speaker.IsSCP() && msg.Speaker.roleManager.CurrentRole.RoleTypeId != RoleTypeId.Scp079;
            AudioMessage? audioMsg = null;
            if (speakerIsScp && profile != null) {
                var newMsg = new AudioMessage {
                    ControllerId = profile.ControllerId,
                    Data = (byte[])msg.Data.Clone(),
                    DataLength = msg.DataLength
                };

                Decoder.Decode(newMsg.Data, newMsg.DataLength, ampArray);
                for (int i = 0; i < ampArray.Length; i++) ampArray[i] = ampArray[i] * 20f;
                newMsg.DataLength = Encoder.Encode(ampArray, newMsg.Data);

                audioMsg = newMsg;
            }

            if (packet.SenderChannel != 0)
			{
				voiceRole.VoiceModule.CurrentChannel = packet.SenderChannel;
				packet.Destinations.ForEach(delegate(KeyValuePair<ReferenceHub, VoiceChatChannel> p)
				{
					if (speakerIsScp && p.Value == VoiceChatChannel.Proximity && audioMsg.HasValue && audioMsg.Value.ControllerId != 255) {
						p.Key.connectionToClient.Send(audioMsg.Value);
						return;
					}

					if (packet.AlternativeSenders.TryGetValue(p.Key, out var value))
					{
						msg.Speaker = value;
					}
					else if (msg.Speaker.netId != packet.Speaker.netId)
					{
						msg.Speaker = packet.Speaker;
					}
					if (p.Value != 0)
					{
						msg.Channel = p.Value;
						p.Key.connectionToClient.Send(msg);
					}
				});
			}
			PacketPool.Pool.Push(packet);
			return false;
		}
		catch (Exception message)
		{
			Plugin.Error("The voice chat patch caught an exception!");
			Plugin.Error(message);
			return true;
		}
	}
}
