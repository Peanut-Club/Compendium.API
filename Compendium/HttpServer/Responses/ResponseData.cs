using System.Text.Json;
using System.Text.Json.Serialization;
using Grapevine;

namespace Compendium.HttpServer.Responses;

public class ResponseData
{
	[JsonPropertyName("res_code")]
	public int Code { get; set; }

	[JsonPropertyName("res_success")]
	public bool IsSuccess { get; set; }

	[JsonPropertyName("res_data")]
	public string Data { get; set; }

	public static ResponseData MissingKey()
	{
		return new ResponseData
		{
			Code = 401,
			IsSuccess = false,
			Data = "Missing authorization header!"
		};
	}

	public static ResponseData InvalidKey()
	{
		return new ResponseData
		{
			Code = 401,
			IsSuccess = false,
			Data = "Invalid authorization key!"
		};
	}

	public static ResponseData Ok(string response = null)
	{
		return new ResponseData
		{
			Code = 200,
			IsSuccess = true,
			Data = response
		};
	}

	public static ResponseData Ok(object response = null)
	{
		string response2 = null;
		if (response != null)
		{
			response2 = JsonSerializer.Serialize(response);
		}
		return Ok(response2);
	}

	public static ResponseData Fail(string response = null)
	{
		return new ResponseData
		{
			Code = 403,
			IsSuccess = false,
			Data = response
		};
	}

	public static void Respond(IHttpContext context, ResponseData data)
	{
		context.Response.SendResponseAsync(JsonSerializer.Serialize(data));
	}
}
