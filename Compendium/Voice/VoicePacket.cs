using System.Collections.Generic;
using PlayerRoles.Voice;
using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice;

public class VoicePacket
{
	public ReferenceHub Speaker { get; set; }

	public VoiceModuleBase Module { get; set; }

	public IVoiceRole Role { get; set; }

	public VoiceChatChannel SenderChannel { get; set; }

	public VoiceMessage Message { get; set; }

	public Dictionary<ReferenceHub, VoiceChatChannel> Destinations { get; set; }

	public Dictionary<ReferenceHub, ReferenceHub> AlternativeSenders { get; set; }

	public float Pitch { get; set; } = 1f;


	public bool IsPitched => Pitch != 1f;
}
