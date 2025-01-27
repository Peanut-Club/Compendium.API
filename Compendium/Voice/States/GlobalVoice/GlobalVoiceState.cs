using System.Collections.Generic;
using helpers;
using helpers.Enums;
using VoiceChat;

namespace Compendium.Voice.States.GlobalVoice;

public class GlobalVoiceState : IVoiceChatState
{
	private ReferenceHub _startedBy;

	public ReferenceHub Starter => _startedBy;

	public GlobalVoiceFlag GlobalVoiceFlag { get; set; }

	public GlobalVoiceState(ReferenceHub starter)
	{
		_startedBy = starter;
	}

	public bool Process(VoicePacket packet)
	{
		if ((object)Starter == null)
		{
			return false;
		}
		packet.Destinations.ForEach(delegate(KeyValuePair<ReferenceHub, VoiceChatChannel> p)
		{
			ReferenceHub key = p.Key;
			if (key.netId != Starter.netId && key.netId != packet.Speaker.netId)
			{
				if (packet.Speaker.netId != Starter.netId)
				{
					if (GlobalVoiceFlag == GlobalVoiceFlag.SpeakerOnly)
					{
						packet.Destinations[key] = VoiceChatChannel.None;
					}
					else if (GlobalVoiceFlag == GlobalVoiceFlag.StaffOnly && packet.Speaker.IsStaff())
					{
						packet.Destinations[key] = VoiceChatChannel.RoundSummary;
					}
					else if (GlobalVoiceFlag.HasFlagFast(GlobalVoiceFlag.PlayerVoice) && !packet.Speaker.IsStaff() && !key.IsStaff())
					{
						packet.Destinations[key] = VoiceChatChannel.RoundSummary;
					}
					else
					{
						packet.Destinations[key] = VoiceChatChannel.None;
					}
				}
				else
				{
					packet.Destinations[key] = VoiceChatChannel.RoundSummary;
				}
			}
		});
		return true;
	}
}
