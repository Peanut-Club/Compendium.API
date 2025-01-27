using System.Collections.Generic;
using helpers;
using helpers.Enums;
using VoiceChat;

namespace Compendium.Voice.States.StaffVoice;

public class StaffVoiceState : IVoiceChatState
{
	private ReferenceHub _startedBy;

	public ReferenceHub Starter => _startedBy;

	public StaffVoiceFlag Flag { get; set; }

	public StaffVoiceState(ReferenceHub starter)
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
			if (key.netId != Starter.netId && key.netId != packet.Speaker.netId && Flag == StaffVoiceFlag.StaffOnly)
			{
				if (!packet.Speaker.IsStaff())
				{
					if (Flag.HasFlagFast(StaffVoiceFlag.PlayersHearPlayers) && key.IsStaff())
					{
						packet.Destinations[key] = VoiceChatChannel.None;
					}
				}
				else if (Flag.HasFlagFast(StaffVoiceFlag.PlayersHearStaff) && !key.IsStaff())
				{
					packet.Destinations[key] = VoiceChatChannel.RoundSummary;
				}
				else if (key.IsStaff())
				{
					packet.Destinations[key] = VoiceChatChannel.RoundSummary;
				}
				else
				{
					packet.Destinations[key] = VoiceChatChannel.None;
				}
			}
		});
		return true;
	}
}
