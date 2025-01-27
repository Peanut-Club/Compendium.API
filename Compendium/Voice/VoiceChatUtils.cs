using System.Collections.Generic;
using Compendium.Voice.Pools;
using Compendium.Voice.States.GlobalVoice;
using Compendium.Voice.States.StaffVoice;
using PlayerRoles.Voice;
using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice;

public static class VoiceChatUtils
{
	public static bool CheckRateLimit(this VoiceModuleBase module, bool addPacket = true)
	{
		if (addPacket)
		{
			module._sentPackets++;
		}
		if (Plugin.Config.VoiceSettings.CustomRateLimit != 0 && module._sentPackets > Plugin.Config.VoiceSettings.CustomRateLimit)
		{
			return false;
		}
		return true;
	}

	public static bool CanHearSelf(this ReferenceHub hub)
	{
		List<VoiceModifier> modifiers = VoiceChat.GetModifiers(hub);
		if (modifiers != null && modifiers.Contains(VoiceModifier.PlaybackEnabled))
		{
			return true;
		}
		return false;
	}

	public static void MakeGlobalSpeaker(this ReferenceHub hub)
	{
		VoiceChat.State = new GlobalVoiceState(hub);
	}

	public static void MakeStaffSpeaker(this ReferenceHub hub)
	{
		VoiceChat.State = new StaffVoiceState(hub);
	}

	public static ReferenceHub GetGlobalSpeaker()
	{
		if (VoiceChat.State != null && VoiceChat.State is GlobalVoiceState globalVoiceState)
		{
			return globalVoiceState.Starter;
		}
		return null;
	}

	public static ReferenceHub GetStaffSpeaker()
	{
		if (VoiceChat.State != null && VoiceChat.State is StaffVoiceState staffVoiceState)
		{
			return staffVoiceState.Starter;
		}
		return null;
	}

	public static void EndCurrentState()
	{
		VoiceChat.State = null;
	}

	public static VoicePacket GeneratePacket(VoiceMessage message, IVoiceRole speakerRole, VoiceChatChannel origChannel)
	{
		VoicePacket voicePacket = PacketPool.Pool.Get();
		voicePacket.SenderChannel = origChannel;
		voicePacket.Role = speakerRole;
		voicePacket.Module = speakerRole.VoiceModule;
		voicePacket.Speaker = message.Speaker;
		voicePacket.Message = message;
		voicePacket.Pitch = 1f;
		GenerateDestinations(message, origChannel, voicePacket.Destinations);
		return voicePacket;
	}

	public static void GenerateDestinations(VoiceMessage message, VoiceChatChannel origChannel, Dictionary<ReferenceHub, VoiceChatChannel> dict)
	{
		Hub.ForEach(delegate(ReferenceHub hub)
		{
			if (hub.netId == message.Speaker.netId)
			{
				if (hub.CanHearSelf())
				{
					dict[hub] = VoiceChatChannel.RoundSummary;
				}
				else
				{
					dict[hub] = VoiceChatChannel.None;
				}
			}
			else if (!(hub.roleManager.CurrentRole is IVoiceRole voiceRole))
			{
				dict[hub] = VoiceChatChannel.None;
			}
			else
			{
				dict[hub] = voiceRole.VoiceModule.ValidateReceive(message.Speaker, origChannel);
			}
		});
	}
}
