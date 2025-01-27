using System.Threading.Tasks;
using Compendium.HttpServer;
using Grapevine;
using PluginAPI.Core;

namespace Compendium.HttpApi;

[RestResource]
public class ServerApi
{
	[RestRoute("Get", "/api/server/packet_threshold")]
	public async Task PacketThresholdAsync(IHttpContext context)
	{
		int count = ReferenceHub.AllHubs.Count;
		int num = 450;
		await context.Response.SendResponseAsync((count * num).ToString());
	}

	[RestRoute("Get", "/api/server/bu_status")]
	public async Task UptimeRoute(IHttpContext context)
	{
		if (context.TryAccess())
		{
			context.Respond("OK");
		}
	}

	[RestRoute("Get", "/api/server/status")]
	public async Task ServerStatusAsync(IHttpContext context)
	{
		if (context.TryAccess("server.status"))
		{
			context.RespondJson(ServerStatusObject.GetCurrent());
		}
	}

	[RestRoute("Any", "/api/server/restart")]
	public async Task ServerRestartAsync(IHttpContext context)
	{
		if (context.TryAccess("server.restart"))
		{
			World.Broadcast("<color=red><b>Server se restartuje za 10 sekund!</b></color>", 10);
			Calls.Delay(10f, delegate
			{
				Server.Restart();
			});
			context.Respond("The server is going to restart in 10 seconds ..");
		}
	}
}
