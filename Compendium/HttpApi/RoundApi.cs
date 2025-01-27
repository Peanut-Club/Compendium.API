using System.Threading.Tasks;
using Compendium.HttpServer;
using Grapevine;
using PluginAPI.Core;

namespace Compendium.HttpApi;

[RestResource]
public class RoundApi
{
	[RestRoute("Any", "api/round/restart")]
	public async Task RoundRestartAsync(IHttpContext context)
	{
		if (context.TryAccess("round.restart"))
		{
			Round.Restart(fastRestart: false, overrideRestartAction: false, ServerStatic.NextRoundAction.DoNothing);
			context.Respond("The round is restarting ..");
		}
	}
}
