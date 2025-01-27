using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterCommands.Permissions;
using Compendium.HttpServer;
using Compendium.Charts;
using Compendium.PlayerData;
using Compendium.Staff;
using Grapevine;
using helpers;
using helpers.Time;
using UnityEngine;

namespace Compendium.HttpApi;

[RestResource]
public class StaffApi
{
	[RestRoute("Get", "/api/staff/activity")]
	public async Task StaffActivityAsync(IHttpContext context)
	{
		if (context.TryAccess(null, PermissionLevel.Lowest))
		{
			StringBuilder sb = Pools.PoolStringBuilder();
			StaffActivity._storage.Data.OrderByDescending((StaffActivityData x) => x.TwoWeeks).For(delegate(int _, StaffActivityData data)
			{
				sb.AppendLine(string.Format("{0}:    {1}h (GAME) | {2} (OVERWATCH)", PlayerDataRecorder.TryQuery(data.UserId, queryNick: false, out var record) ? (record.NameTracking.LastValue + " (" + record.UserId + ")") : data.UserId, Mathf.RoundToInt((float)TimeSpan.FromSeconds(data.TwoWeeks).TotalHours), TimeSpan.FromSeconds(data.TwoWeeksOverwatch).UserFriendlySpan()));
			});
			await context.Response.SendResponseAsync(sb.ReturnStringBuilderValue());
		}
	}

	[RestRoute("Get", "/api/staff/activity_chart")]
	public async Task StaffActivityChartAsync(IHttpContext context)
	{
		if (context.TryAccess(null, PermissionLevel.Lowest))
		{
			List<KeyValuePair<string, int>> set = new List<KeyValuePair<string, int>>();
			StaffActivity._storage.Data.OrderByDescending((StaffActivityData x) => x.TwoWeeks).For(delegate(int _, StaffActivityData data)
			{
				set.Add(new KeyValuePair<string, int>((PlayerDataRecorder.TryQuery(data.UserId, queryNick: false, out var record) ? (record.NameTracking.LastValue + " (" + record.UserId + ")") : data.UserId) ?? "", Mathf.RoundToInt((float)TimeSpan.FromSeconds(data.TwoWeeks).TotalHours)));
			});
			byte[] chart = ChartBuilder.GetChart("Aktivita", set);
			context.Response.ContentType = "image/png";
			context.Response.AddHeader("Content-Type", "image/png");
			await context.Response.SendResponseAsync(chart);
		}
	}

	[RestRoute("Get", "/api/staff/activity_json")]
	public async Task ActivityJsonAsync(IHttpContext context)
	{
		context.Response.ContentType = "application/json";
		StatusResponse statusResponse = (context.TryAccess() ? new StatusResponse(StaffActivity._storage.Data.Value) : new StatusResponse(success: false, "Not enough access"));
		await context.Response.SendResponseAsync(statusResponse.ToPureJson());
	}

	[RestRoute("Get", "/api/staff/single_activity_json")]
	public async Task SingleActivityJsonAsync(IHttpContext context)
	{
		context.Response.ContentType = "application/json";
		string userid;
		StaffActivityData value;
		StatusResponse statusResponse = ((!context.TryAccess()) ? new StatusResponse(success: false, "Not enough access") : (((userid = context.Request.QueryString.Get("userid")) == null) ? new StatusResponse(success: false, "Query parameter 'userid' is not specified") : (StaffActivity._storage.Data.Value.TryGetFirst((StaffActivityData record) => record.UserId.Contains(userid), out value) ? new StatusResponse(value) : new StatusResponse(success: false, "UserID '" + userid + "' not found"))));
		await context.Response.SendResponseAsync(statusResponse.ToPureJson());
	}
}
