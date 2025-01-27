using System.Collections.Generic;

namespace Compendium.Custom.Parsers.PlayerList.Nodes;

public class StatementsNode : ExpressionNode
{
	public List<ExpressionNode> codeStrings = new List<ExpressionNode>();

	public void AddNode(ExpressionNode node)
	{
		codeStrings.Add(node);
	}
}
