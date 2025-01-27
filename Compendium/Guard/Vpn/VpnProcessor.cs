using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Compendium.Http;

namespace Compendium.Guard.Vpn;

public class VpnProcessor : IServerGuardProcessor
{
	public const string BaseUrl = "http://v2.api.iphub.info/ip";

	public void Process(ReferenceHub hub, Action<ServerGuardReason> callback)
	{
		if (Plugin.Config == null || Plugin.Config.GuardSettings == null || string.IsNullOrWhiteSpace(Plugin.Config.GuardSettings.VpnSettings.Key) || Plugin.Config.GuardSettings.VpnSettings.Key == "none")
		{
			callback(ServerGuardReason.Ignore);
			return;
		}
		string address = "http://v2.api.iphub.info/ip/" + hub.Ip();
		HttpDispatch.Get(address, delegate(HttpDispatchData data)
		{
			try
			{
				VpnResponse response = JsonSerializer.Deserialize<VpnResponse>(data.Response);
				string item = $"ASN{response.Asn}";
				if (Plugin.Config.GuardSettings.VpnSettings.WhitelistedAsn.Contains(item) || Plugin.Config.GuardSettings.VpnSettings.WhitelistedCIDR.Any((string cidr) => ServerGuardUtils.IsInRange(response.Ip, cidr)))
				{
					callback(ServerGuardReason.None);
				}
				else if (response.BlockLevel == 1 || (response.BlockLevel == 2 && Plugin.Config.GuardSettings.VpnSettings.IsStrict))
				{
					callback(ServerGuardReason.ProxyNetwork);
				}
				else if (Plugin.Config.GuardSettings.VpnSettings.BlockedAsn.Contains(item))
				{
					callback(ServerGuardReason.BlockedAsn);
				}
				else if (Plugin.Config.GuardSettings.VpnSettings.BlockedCIDR.Any((string cidr) => ServerGuardUtils.IsInRange(response.Ip, cidr)))
				{
					callback(ServerGuardReason.BlockedCidr);
				}
				else
				{
					callback(ServerGuardReason.None);
				}
			}
			catch (Exception message)
			{
				Plugin.Error("Failed to retrieve IP hub info");
				Plugin.Error(message);
				callback(ServerGuardReason.Ignore);
			}
		}, new KeyValuePair<string, string>("X-Key", Plugin.Config.GuardSettings.VpnSettings.Key));
	}
}
