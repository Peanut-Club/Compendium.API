namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class UnOpNode : ExpressionNode
{
	public PlayerListToken op { get; private set; }

	public ExpressionNode operand { get; private set; }

	public UnOpNode(PlayerListToken op, ExpressionNode operand)
	{
		this.op = op;
		this.operand = operand;
	}
}
