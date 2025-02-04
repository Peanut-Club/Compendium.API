using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;
using VoiceChat.Networking;

namespace Compendium.Voice.Profiles.Pitch;

public class PitchProfile : IVoiceProfile
{
	private bool isEnabled;

	private float pitchFactor;

	private ReferenceHub owner;

	public OpusEncoder Encoder = new OpusEncoder(OpusApplicationType.Voip);

	public OpusDecoder Decoder = new OpusDecoder();

	public PitchHelper PitchHelper = new PitchHelper();

	public ReferenceHub Owner => owner;
	public virtual byte ControllerId { get; internal set; } = 255;

    public IVoiceRole Role => owner.Role() as IVoiceRole;

	public VoiceChatChannel Channel
	{
		get
		{
			return Module?.CurrentChannel ?? VoiceChatChannel.None;
		}
		set
		{
			Module.CurrentChannel = value;
		}
	}

	public VoiceModuleBase Module => Role?.VoiceModule ?? null;

	public bool IsEnabled => isEnabled;

	public bool IsPersistent => false;

	public bool IsActive
	{
		get
		{
			if (IsEnabled && Pitch != 1f && Pitch >= 0.1f)
			{
				return Pitch <= 2f;
			}
			return false;
		}
	}

	public float Pitch
	{
		get
		{
			return pitchFactor;
		}
		set
		{
			pitchFactor = Mathf.Clamp(value, 0.1f, 2f);
		}
	}

	public PitchProfile(ReferenceHub owner)
	{
		pitchFactor = 1f;
		this.owner = owner;
	}

	public virtual void Disable()
	{
		isEnabled = false;
		pitchFactor = 1f;
		owner = null;
	}

	public virtual void Enable()
	{
		isEnabled = true;
		pitchFactor = 1f;
	}

	public virtual void OnRoleChanged()
	{
	}

	public virtual bool Process(VoicePacket packet)
	{
		if (IsActive)
		{
			float[] array = new float[48000];
			VoiceMessage message = packet.Message;
			Decoder.Decode(message.Data, message.DataLength, array);
			PitchHelper.PitchShift(Pitch, 480L, 48000f, array);
			message.DataLength = Encoder.Encode(array, message.Data);
			packet.Message = message;
			return true;
		}
		return false;
	}
}
