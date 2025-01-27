using System.ComponentModel;

namespace Compendium.Settings;

public class GuardSteamSettings
{
	[Description("A Steam API key.")]
	public string Key { get; set; } = "none";


	[Description("Whether or not to kick private accounts.")]
	public bool KickPrivate { get; set; } = true;


	[Description("Whether or not to kick accounts that have not been set up yet.")]
	public bool KickNotSetup { get; set; } = true;


	[Description("Sets the minimum required account age.")]
	public int AccountAge { get; set; } = -1;

}
