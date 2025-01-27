namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class PlayerNode : ExpressionNode
{
	public PlayerListToken playerID { get; private set; }

	public PlayerNode(PlayerListToken playerID)
	{
		this.playerID = playerID;
	}
}
