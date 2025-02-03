using Compendium.Input;
using UnityEngine;

namespace Compendium.Voice.Profiles.Scp;

public class ScpVoiceKeybind : IInputHandler
{
	public KeyCode Key => KeyCode.RightAlt;

	public bool IsChangeable => true;

    public string Id => "voice_proximity";
    public string Label => "SCP - Proximity voice";

    public void OnPressed(ReferenceHub player)
	{
		IVoiceProfile profile = VoiceChat.GetProfile(player);
		if (profile != null && profile is ScpVoiceProfile scpVoiceProfile)
		{
			scpVoiceProfile.OnSwitchUsed();
		}
	}
}
