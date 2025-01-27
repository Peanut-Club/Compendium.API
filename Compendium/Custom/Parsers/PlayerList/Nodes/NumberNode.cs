namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class NumberNode : ExpressionNode
{
	public PlayerListToken number { get; private set; }

	public NumberNode(PlayerListToken number)
	{
		this.number = number;
	}
}
