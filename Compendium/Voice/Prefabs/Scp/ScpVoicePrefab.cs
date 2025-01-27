using System;
using Compendium.Voice.Profiles.Scp;

namespace Compendium.Voice.Prefabs.Scp;

public class ScpVoicePrefab : BasePrefab
{
	public override Type Type { get; } = typeof(ScpVoiceProfile);


	public ScpVoicePrefab()
		: base(Plugin.Config.VoiceSettings.AllowedScpChat)
	{
	}

	public override IVoiceProfile Instantiate(ReferenceHub owner)
	{
		return new ScpVoiceProfile(owner);
	}
}
