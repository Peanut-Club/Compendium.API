using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Compendium.Updating;
using helpers;
using helpers.Attributes;
using helpers.Extensions;

namespace Compendium.Http;

[LogSource("HTTP")]
public static class HttpDispatch
{
	private static volatile HttpClient _client;

	private static volatile HttpClientHandler _handler;

	private static volatile ConcurrentQueue<HttpDispatchData> _dispatchQueue = new ConcurrentQueue<HttpDispatchData>();

	public static void PostJson(string address, Action<HttpDispatchData> callback, string jsonContent, params KeyValuePair<string, string>[] headers)
	{
		Post(address, callback, new StringContent(jsonContent), headers);
	}

	public static void Post(string address, Action<HttpDispatchData> callback, HttpContent content, params KeyValuePair<string, string>[] headers)
	{
		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, address);
		if (headers.Any())
		{
			headers.ForEach(delegate(KeyValuePair<string, string> pair)
			{
				request.Headers.Add(pair.Key, pair.Value);
			});
		}
		if (content != null)
		{
			request.Content = content;
		}
		request.Headers.Add("User-Agent", "Other");
		_dispatchQueue.Enqueue(new HttpDispatchData(address, request, callback));
	}

	public static void Get(string address, Action<HttpDispatchData> callback, params KeyValuePair<string, string>[] headers)
	{
		Get(address, callback, null, headers);
	}

	public static void GetJson(string address, Action<HttpDispatchData> callback, string jsonContent, params KeyValuePair<string, string>[] headers)
	{
		Get(address, callback, new StringContent(jsonContent), headers);
	}

	public static void Get(string address, Action<HttpDispatchData> callback, HttpContent content, params KeyValuePair<string, string>[] headers)
	{
		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, address);
		if (headers.Any())
		{
			headers.ForEach(delegate(KeyValuePair<string, string> pair)
			{
				request.Headers.Add(pair.Key, pair.Value);
			});
		}
		request.Headers.Add("User-Agent", "Other");
		if (content != null)
		{
			request.Content = content;
		}
		_dispatchQueue.Enqueue(new HttpDispatchData(address, request, callback));
	}

	[Load]
	private static void Initialize()
	{
		_handler = new HttpClientHandler();
		_handler.UseDefaultCredentials = true;
		_handler.UseProxy = false;
		_client = new HttpClient(_handler);
	}

	private static void OnRequestFailed(HttpDispatchData httpDispatchData, Exception exception)
	{
		Plugin.Warn("Request to \"" + httpDispatchData.Target + "\" failed!");
		Plugin.Warn(exception.Message ?? "");
		if (Plugin.Config.ApiSetttings.HttpSettings.MaxRequeueCount != 0 && httpDispatchData.RequeueCount < Plugin.Config.ApiSetttings.HttpSettings.MaxRequeueCount)
		{
			_dispatchQueue.Enqueue(httpDispatchData);
			httpDispatchData.OnRequeued();
		}
	}

	private static void OnRequestFailed(HttpDispatchData httpDispatchData, HttpStatusCode code)
	{
		if (Plugin.Config.ApiSetttings.HttpSettings.Debug)
		{
			Plugin.Warn("Request to \"" + httpDispatchData.Target + "\" failed! (" + code.ToString().SpaceByPascalCase() + ")");
		}
		if (Plugin.Config.ApiSetttings.HttpSettings.MaxRequeueCount != 0 && httpDispatchData.RequeueCount < Plugin.Config.ApiSetttings.HttpSettings.MaxRequeueCount)
		{
			_dispatchQueue.Enqueue(httpDispatchData);
			httpDispatchData.OnRequeued();
		}
	}

	[Update(IsUnity = false, PauseRestarting = false, PauseWaiting = false)]
	private static async void Update()
	{
		if (!_dispatchQueue.TryDequeue(out var data))
		{
			return;
		}
		try
		{
			data.RefreshRequest();
			using HttpResponseMessage response = await _client.SendAsync(data.Request);
			if (!response.IsSuccessStatusCode)
			{
				OnRequestFailed(data, response.StatusCode);
				return;
			}
			HttpDispatchData httpDispatchData = data;
			HttpDispatchData httpDispatchData2 = httpDispatchData;
			httpDispatchData2.OnReceived(await response.Content.ReadAsStringAsync());
		}
		catch (Exception exception)
		{
			OnRequestFailed(data, exception);
		}
	}
}
