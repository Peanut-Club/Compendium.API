using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Compendium.Http;

namespace Compendium.Guard.Steam;

public class SteamProcessor : IServerGuardProcessor
{
	public const string BaseUrl = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=$key$&steamids=$steamid$&format=json";

	public void Process(ReferenceHub player, Action<ServerGuardReason> callback)
	{
		if (Plugin.Config == null || Plugin.Config.GuardSettings == null || string.IsNullOrWhiteSpace(Plugin.Config.GuardSettings.SteamSettings.Key) || Plugin.Config.GuardSettings.SteamSettings.Key == "none")
		{
			callback(ServerGuardReason.Ignore);
			return;
		}
		string text = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=$key$&steamids=$steamid$&format=json".Replace("$key$", Plugin.Config.GuardSettings.SteamSettings.Key).Replace("$steamid$", player.ParsedUserId().ClearId);
		Plugin.Debug("Targeting URL: " + text);
		HttpDispatch.Get(text, delegate(HttpDispatchData data)
		{
			try
			{
				Plugin.Debug("\n=== STEAM RESPONSE (" + player.UserId() + ") ===\n" + data.Response);
				if (!JsonSerializer.Deserialize<JsonObject>(data.Response).TryGetPropertyValue("response", out JsonNode jsonNode))
				{
					Plugin.Error("Failed to fetch responseNode");
					callback(ServerGuardReason.Ignore);
				}
				else
				{
					SteamResponse steamResponse = jsonNode["players"].Deserialize<JsonArray>().First().Deserialize<SteamResponse>();
					Plugin.Debug($"State: {steamResponse.StateType}, Visibility: {steamResponse.VisibilityType}, Name: {steamResponse.Name}, Age: {steamResponse.CreationTimestamp} ({UnixTimeStampToDateTime(steamResponse.CreationTimestamp)})");
					if (Plugin.Config.GuardSettings.SteamSettings.KickPrivate && steamResponse.VisibilityType != 3)
					{
						callback(ServerGuardReason.PrivateAccount);
					}
					else if (Plugin.Config.GuardSettings.SteamSettings.KickNotSetup && steamResponse.StateType != 1)
					{
						callback(ServerGuardReason.NotSetupAccount);
					}
					else if (steamResponse.VisibilityType == 3 && Plugin.Config.GuardSettings.SteamSettings.AccountAge > 0 && (DateTime.Now.ToLocalTime() - UnixTimeStampToDateTime(steamResponse.CreationTimestamp)).TotalSeconds < (double)Plugin.Config.GuardSettings.SteamSettings.AccountAge)
					{
						callback(ServerGuardReason.AccountAge);
					}
					else
					{
						callback(ServerGuardReason.None);
					}
				}
			}
			catch (Exception arg)
			{
				Plugin.Error($"Error during STEAM processing: {arg}");
				callback(ServerGuardReason.Ignore);
			}
		});
	}

	public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
	}
}
