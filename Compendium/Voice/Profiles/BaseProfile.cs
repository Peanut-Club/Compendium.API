using PlayerRoles.Voice;
using VoiceChat;

namespace Compendium.Voice.Profiles;

public class BaseProfile : IVoiceProfile
{
	private ReferenceHub _owner;

	private bool _isEnabled;

	public ReferenceHub Owner => _owner;

	public bool IsEnabled => _isEnabled;

	public bool IsPersistent { get; }

	public IVoiceRole Role => _owner.Role() as IVoiceRole;

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

	public BaseProfile(ReferenceHub owner)
	{
		_owner = owner;
	}

	public virtual bool Process(VoicePacket packet)
	{
		return false;
	}

	public virtual void OnRoleChanged()
	{
	}

	public virtual void Disable()
	{
		_isEnabled = false;
	}

	public virtual void Enable()
	{
		_isEnabled = true;
	}
}
