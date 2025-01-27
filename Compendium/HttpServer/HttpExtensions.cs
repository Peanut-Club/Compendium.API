using System.Linq;
using System.Net;
using BetterCommands.Permissions;
using Compendium.HttpServer.Authentification;
using Compendium.HttpServer.Responses;
using Compendium.PlayerData;
using Compendium.Staff;
using Grapevine;

namespace Compendium.HttpServer;

public static class HttpExtensions
{
	public static string GetRealIp(this IHttpContext ctx)
	{
		string text = ctx.Request.Headers.Get("X-Real-IP");
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return ctx.Request.RemoteEndPoint.Address.ToString();
	}

	public static void RespondFail(this IHttpContext context, System.Net.HttpStatusCode code, string message = null)
	{
		ResponseData.Respond(context, new ResponseData
		{
			Code = (int)code,
			IsSuccess = false,
			Data = message
		});
	}

	public static void Respond(this IHttpContext context, string response = null)
	{
		ResponseData.Respond(context, ResponseData.Ok(response));
	}

	public static void RespondJson(this IHttpContext context, object response)
	{
		ResponseData.Respond(context, ResponseData.Ok(response));
	}

	public static bool TryAccess(this IHttpContext context, string perm = null, PermissionLevel staffLevel = PermissionLevel.None)
	{
		if (!string.IsNullOrWhiteSpace(perm))
		{
			return context.TryAuth(perm, staffLevel);
		}
		return true;
	}

	public static bool TryAuth(this IHttpContext context, string perm, PermissionLevel staffLevel)
	{
		if (staffLevel != 0)
		{
			string realIp = context.GetRealIp();
			if (PlayerDataRecorder.TryQuery(realIp, queryNick: false, out var record) && StaffHandler.Members.TryGetValue(record.UserId, out var value) && value.Any((string group) => StaffHandler.Groups.TryGetValue(group, out var value3) && value3.GroupFlags.Contains(StaffGroupFlags.IsStaff)) && value.Any((string group) => HasPerm(group, staffLevel)))
			{
				return true;
			}
		}
		string value2 = context.Request.Headers.GetValue<string>("X-Key");
		if (string.IsNullOrWhiteSpace(value2))
		{
			ResponseData.Respond(context, ResponseData.MissingKey());
			Plugin.Warn($"{context.Request.RemoteEndPoint} attempted to access '{context.Request.Endpoint}' without an auth key!");
			return false;
		}
		HttpAuthentificationResult httpAuthentificationResult = HttpAuthentificator.TryAuthentificate(value2, perm);
		if (httpAuthentificationResult != HttpAuthentificationResult.Authorized)
		{
			ResponseData.Respond(context, ResponseData.InvalidKey());
			Plugin.Warn($"{context.Request.RemoteEndPoint} attempted to access '{context.Request.Endpoint}' with an '{httpAuthentificationResult}' auth!");
			return false;
		}
		return true;
	}

	private static bool HasPerm(string group, PermissionLevel level)
	{
		if (PermissionManager.Config != null && PermissionManager.Config.LevelsByPlayer != null && PermissionManager.Config.LevelsByPlayer.TryGetValue(group, out var value))
		{
			return value >= level;
		}
		return false;
	}
}
