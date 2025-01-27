namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class AnyNode : ExpressionNode
{
	public PlayerListToken any { get; private set; }

	public AnyNode(PlayerListToken any)
	{
		this.any = any;
	}
}
