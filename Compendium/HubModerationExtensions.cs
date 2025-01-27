using System;

namespace Compendium;

public static class HubModerationExtensions
{
	public static void Kick(this ReferenceHub hub, string reason = "No reason provided.")
	{
		ServerConsole.Disconnect(hub.connectionToClient, reason);
	}

	public static void Ban(this ReferenceHub hub, bool issueIp = true, string duration = "5m", string reason = "No reason provided.")
	{
		long num = Misc.RelativeTimeToSeconds(duration);
		if (num > 0)
		{
			BanHandler.IssueBan(new BanDetails
			{
				Expires = (DateTime.Now + TimeSpan.FromSeconds(num)).Ticks,
				Id = hub.UserId(),
				IssuanceTime = DateTime.Now.Ticks,
				Issuer = "Dedicated Server",
				OriginalName = hub.Nick(),
				Reason = reason
			}, BanHandler.BanType.UserId, forced: true);
			if (issueIp)
			{
				BanHandler.IssueBan(new BanDetails
				{
					Expires = (DateTime.Now + TimeSpan.FromSeconds(num)).Ticks,
					Id = hub.Ip(),
					IssuanceTime = DateTime.Now.Ticks,
					Issuer = "Dedicated Server",
					OriginalName = hub.Nick(),
					Reason = reason
				}, BanHandler.BanType.IP, forced: true);
			}
		}
	}
}
