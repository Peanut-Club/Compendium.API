using System.Text.Json.Serialization;
using Compendium.Enums;
using GameCore;

namespace Compendium.HttpApi;

public class ServerStatusObject
{
	[JsonPropertyName("server_name")]
	public string Name { get; set; }

	[JsonPropertyName("server_players")]
	public int Players { get; set; }

	[JsonPropertyName("server_max_players")]
	public int MaxPlayers { get; set; }

	[JsonPropertyName("server_status_id")]
	public int StatusId { get; set; }

	public static ServerStatusObject GetCurrent()
	{
		ServerStatusObject serverStatusObject = new ServerStatusObject();
		serverStatusObject.Name = World.CurrentClearOrAlternativeServerName;
		serverStatusObject.Players = Hub.Count;
		serverStatusObject.MaxPlayers = ConfigFile.ServerConfig.GetInt("max_players", 20);
		serverStatusObject.StatusId = GetCurrentStatusId();
		return serverStatusObject;
	}

	public static int GetCurrentStatusId()
	{
		if (IdleMode.IdleModeActive)
		{
			return 0;
		}
		if (RoundHelper.State == RoundState.Ending)
		{
			return 3;
		}
		if (RoundHelper.State == RoundState.InProgress)
		{
			return 2;
		}
		if (RoundHelper.State == RoundState.Restarting)
		{
			return 4;
		}
		return 1;
	}
}
