using System.Net;
using System.Threading.Tasks;
using BetterCommands.Permissions;
using Compendium.HttpServer;
using Compendium.PlayerData;
using Grapevine;
using Newtonsoft.Json;

namespace Compendium.Http.Api;

[RestResource]
public class CommonApi
{
	[RestRoute("Get", "/api/ip")]
	public async Task IpAsync(IHttpContext context)
	{
		if (context.TryAccess())
		{
			await context.Response.SendResponseAsync(context.GetRealIp());
		}
	}

	[RestRoute("Get", "/api/query/{key}")]
	public async Task QueryAsync(IHttpContext context)
	{
		if (context.TryAccess("query", PermissionLevel.Lowest))
		{
			PlayerDataRecord record;
			if (!context.Request.PathParameters.TryGetValue("key", out var value))
			{
				context.RespondFail(System.Net.HttpStatusCode.NotFound, "Failed to retrieve key parameter");
			}
			else if (PlayerDataRecorder.TryQuery(value, queryNick: true, out record))
			{
				await context.Response.SendResponseAsync(JsonConvert.SerializeObject(record, Formatting.Indented));
			}
			else
			{
				context.RespondFail(System.Net.HttpStatusCode.NotFound, "The specified record has not been found");
			}
		}
	}
}
