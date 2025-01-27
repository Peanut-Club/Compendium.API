namespace Compendium.Voice;

public interface IVoiceProfile
{
	ReferenceHub Owner { get; }

	bool IsEnabled { get; }

	bool IsPersistent { get; }

	bool Process(VoicePacket packet);

	void OnRoleChanged();

	void Enable();

	void Disable();
}
