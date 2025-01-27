namespace Compendium.Custom.Parsers.PlayerList;

public class PlayerListToken
{
	private int pos;

	public PlayerListTokenType type { get; private set; }

	public string text { get; private set; }

	public PlayerListToken(PlayerListTokenType type, string text, int pos)
	{
		this.type = type;
		this.text = text;
		this.pos = pos;
	}
}
