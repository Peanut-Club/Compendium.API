using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings;

public class GuardVpnSettings
{
	[Description("VPN client key.")]
	public string Key { get; set; } = "none";


	[Description("Whether or not to use the strict blocking level.")]
	public bool IsStrict { get; set; } = true;


	[Description("A list of blocked ASNs.")]
	public List<string> BlockedAsn { get; set; } = new List<string>();


	[Description("A list of whitelisted ASNs.")]
	public List<string> WhitelistedAsn { get; set; } = new List<string>();


	[Description("A list of blocked CIDR masks.")]
	public List<string> BlockedCIDR { get; set; } = new List<string>();


	[Description("A list of whitelisted CIDR masks.")]
	public List<string> WhitelistedCIDR { get; set; } = new List<string>();

}
