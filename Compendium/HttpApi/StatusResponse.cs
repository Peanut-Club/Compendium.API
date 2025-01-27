using Newtonsoft.Json;

namespace Compendium.HttpApi;

public struct StatusResponse
{
	public bool success { get; set; }

	public object content { get; set; }

	public StatusResponse(bool success, object content)
	{
		this.success = success;
		this.content = content;
	}

	public StatusResponse(object content)
	{
		success = true;
		this.content = content;
	}

	public string ToPureJson()
	{
		return JsonConvert.SerializeObject(this);
	}
}
