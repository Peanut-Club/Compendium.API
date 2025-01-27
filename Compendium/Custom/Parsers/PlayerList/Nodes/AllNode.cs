namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class AllNode : ExpressionNode
{
	public PlayerListToken all;

	public AllNode(PlayerListToken all)
	{
		this.all = all;
	}
}
