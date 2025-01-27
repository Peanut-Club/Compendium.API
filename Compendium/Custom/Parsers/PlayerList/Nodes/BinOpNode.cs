namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class BinOpNode : ExpressionNode
{
	public PlayerListToken op { get; private set; }

	public ExpressionNode leftOp { get; private set; }

	public ExpressionNode rightOp { get; private set; }

	public BinOpNode(PlayerListToken op, ExpressionNode leftOp, ExpressionNode rightOp)
	{
		this.op = op;
		this.leftOp = leftOp;
		this.rightOp = rightOp;
	}
}
