using System;
using System.ComponentModel;
using System.Linq;
using PlayerRoles;

namespace Compendium.Settings;

public class VoiceSettings
{
	[Description("Maximum number of packets sent per 100 milliseconds.")]
	public int CustomRateLimit { get; set; } = 128;


	[Description("All SCP roles allowed to use the proximity chat.")]
	public RoleTypeId[] AllowedScpChat { get; set; } = Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().Where(delegate(RoleTypeId r)
	{
		RoleTypeId roleTypeId = r;
		return roleTypeId.ToString().ToLower().Contains("scp");
	})
		.ToArray();

}
