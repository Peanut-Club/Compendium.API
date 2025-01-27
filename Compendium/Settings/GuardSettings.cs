using System.Collections.Generic;
using System.ComponentModel;
using Compendium.Guard;

namespace Compendium.Settings;

public class GuardSettings
{
	[Description("A dictionary of kick reasons and their messages.")]
	public Dictionary<ServerGuardReason, string> KickReasons { get; set; } = new Dictionary<ServerGuardReason, string>
	{
		[ServerGuardReason.ProxyNetwork] = "Tvoje síť byla detekována jako VPN!",
		[ServerGuardReason.BlockedAsn] = "Tvoje IP adresa spadá pod zablokované ASN!",
		[ServerGuardReason.BlockedCidr] = "Tvoje IP adresa spadá do zablokovaného CIDR rekordu!",
		[ServerGuardReason.PrivateAccount] = "Tvůj STEAM profil nesmí být soukromý!",
		[ServerGuardReason.NotSetupAccount] = "Tvůj STEAM profil není nastavený!"
	};


	[Description("VPN settings.")]
	public GuardVpnSettings VpnSettings { get; set; } = new GuardVpnSettings();


	[Description("Steam settings.")]
	public GuardSteamSettings SteamSettings { get; set; } = new GuardSteamSettings();

}
