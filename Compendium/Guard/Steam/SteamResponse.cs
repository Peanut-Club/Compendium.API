using System.Text.Json.Serialization;

namespace Compendium.Guard.Steam;

public class SteamResponse
{
	[JsonPropertyName("steamid")]
	public string Id { get; set; }

	[JsonPropertyName("personaname")]
	public string Name { get; set; }

	[JsonPropertyName("communityvisibilitystate")]
	public int VisibilityType { get; set; }

	[JsonPropertyName("profilestate")]
	public int StateType { get; set; }

	public bool IsSetup => StateType != 1;

	public bool IsPublic => VisibilityType == 3;

	[JsonPropertyName("timecreated")]
	public ulong CreationTimestamp { get; set; }
}
