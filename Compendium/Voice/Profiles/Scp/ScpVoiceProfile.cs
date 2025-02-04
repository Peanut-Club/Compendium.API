using System.Collections.Generic;
using BetterCommands;
using Compendium.API.Compendium.Voice.Proximity;
using Compendium.Constants;
using Compendium.Input;
using Compendium.IO.Saving;
using Compendium.Voice.Profiles.Pitch;
using helpers;
using helpers.Attributes;
using helpers.Pooling.Pools;
using PlayerRoles;
using PlayerRoles.Spectating;
using VoiceChat;

namespace Compendium.Voice.Profiles.Scp;

public class ScpVoiceProfile : PitchProfile
{
	private static SaveFile<CollectionSaveData<string>> _mutes;

	private ProximitySpeaker _speaker;
	public override byte ControllerId { get; internal set; } = 255;

    public ScpVoiceFlag Flag { get; set; }

	public ScpVoiceFlag NextFlag
	{
		get
		{
			if (Flag == ScpVoiceFlag.ScpChatOnly)
			{
				return ScpVoiceFlag.ProximityAndScpChat;
			}
			if (Flag == ScpVoiceFlag.ProximityAndScpChat)
			{
				return ScpVoiceFlag.ProximityChatOnly;
			}
			return ScpVoiceFlag.ScpChatOnly;
		}
	}

	public ScpVoiceProfile(ReferenceHub owner)
		: base(owner)
	{
    }

    public override void Enable() {
        base.Enable();
        _speaker = ProximityManager.CreateProximitySpeaker(Owner);
        ControllerId = _speaker.ControllerId;
        //Plugin.Info("Speaker created with id: " + ControllerId);
    }

    public override void Disable() {
        base.Disable();
        var id = ControllerId;
        ProximityManager.DeleteSpeaker(_speaker);
        _speaker = null;
        ControllerId = 255;
        //Plugin.Info("Speaker deleted with id: " + id);
    }

    public void OnSwitchUsed()
	{
		Flag = NextFlag;
		base.Owner.Broadcast(Colors.LightGreen("<b>Voice přepnut na " + TypeAndColor() + " chat</b>"), 3);
	}

	public override bool Process(VoicePacket packet)
	{
		if (packet.Speaker.netId != base.Owner.netId)
		{
			return false;
		}
		base.Process(packet);
		if (base.Owner.IsSCP())
		{
			if (packet.SenderChannel != VoiceChatChannel.Mimicry)
			{
				packet.SenderChannel = VoiceChatChannel.ScpChat;
			}
			Dictionary<ReferenceHub, VoiceChatChannel> dictionary = DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Get(packet.Destinations);
			foreach (KeyValuePair<ReferenceHub, VoiceChatChannel> destination in packet.Destinations)
			{
				if (destination.Key.netId == packet.Speaker.netId || !dictionary.ContainsKey(destination.Key) || dictionary[destination.Key] == VoiceChatChannel.Mimicry)
				{
					continue;
				}
				if (destination.Key.RoleId() == RoleTypeId.Overwatch && base.Owner.IsSpectatedBy(destination.Key) && !_mutes.Data.Contains(destination.Key.UserId()))
				{
					dictionary[destination.Key] = VoiceChatChannel.RoundSummary;
				}
				else if (Flag == ScpVoiceFlag.ScpChatOnly)
				{
					if (!destination.Key.IsSCP())
					{
						dictionary[destination.Key] = VoiceChatChannel.None;
					}
					else
					{
						dictionary[destination.Key] = VoiceChatChannel.ScpChat;
					}
				}
				else if (Flag == ScpVoiceFlag.ProximityAndScpChat)
				{
					if (destination.Key.IsSCP())
					{
						dictionary[destination.Key] = VoiceChatChannel.ScpChat;
					}
					else
					{
						if (_mutes.Data.Contains(destination.Key.UserId()))
						{
							continue;
						}
						if (Plugin.Config.VoiceSettings.AllowedScpChat.Contains(base.Owner.RoleId()))
						{
							/*
							if (destination.Key.Position().IsWithinDistance(base.Owner.Position(), 25f))
							{
								dictionary[destination.Key] = VoiceChatChannel.RoundSummary;
							}
							else
							{
								dictionary[destination.Key] = VoiceChatChannel.None;
                            }*/
                            dictionary[destination.Key] = VoiceChatChannel.Proximity;
                        }
						else
						{
							dictionary[destination.Key] = VoiceChatChannel.None;
						}
					}
				}
				else
				{
					if (Flag != ScpVoiceFlag.ProximityChatOnly || _mutes.Data.Contains(destination.Key.UserId()))
					{
						continue;
					}
					if (Plugin.Config.VoiceSettings.AllowedScpChat.Contains(base.Owner.RoleId()))
					{
                        /*
						if (destination.Key.Position().IsWithinDistance(base.Owner.Position(), 25f))
						{
							dictionary[destination.Key] = VoiceChatChannel.RoundSummary;
						}
						else
						{
							dictionary[destination.Key] = VoiceChatChannel.None;
						}*/
                        dictionary[destination.Key] = VoiceChatChannel.Proximity;
                    }
					else
					{
						dictionary[destination.Key] = VoiceChatChannel.None;
					}
				}
			}
			packet.Destinations.Clear();
			packet.Destinations.AddRange(dictionary);
			DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Push(dictionary);
		}
		return true;
	}

	public string TypeAndColor()
	{
		ScpVoiceFlag flag = Flag;
		if (1 == 0)
		{
		}
		string result = flag switch
		{
			ScpVoiceFlag.ScpChatOnly => Colors.Red("SCP"), 
			ScpVoiceFlag.ProximityChatOnly => Colors.Green("Proximity"), 
			ScpVoiceFlag.ProximityAndScpChat => Colors.Red("SCP") + " a " + Colors.Green("Proximity"), 
			_ => "", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	[Command("muteproximity", new CommandType[] { CommandType.PlayerConsole })]
	[CommandAliases(new object[] { "mprox", "mutep" })]
	[Description("Mutes SCP proximity chat.")]
	private static string MuteProximityCommand(ReferenceHub sender)
	{
		if (_mutes == null)
		{
			_mutes = new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("SavedProximityMutes", "proxMutes"));
		}
		if (_mutes.Data.Contains(sender.UserId()))
		{
			_mutes.Data.Remove(sender.UserId());
			_mutes.Save();
			return "SCP proximity chat unmuted.";
		}
		_mutes.Data.Add(sender.UserId());
		_mutes.Save();
		return "SCP proximity chat muted.";
	}

	[Load]
	private static void Load()
	{
		if (!InputManager.TryGetHandler<ScpVoiceKeybind>(out var _))
		{
			InputManager.Register<ScpVoiceKeybind>();
		}
		if (_mutes == null)
		{
			_mutes = new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("SavedProximityMutes", "proxMutes"));
		}
	}
}
