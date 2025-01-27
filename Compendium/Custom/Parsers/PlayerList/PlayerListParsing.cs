using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Compendium.Custom.Parsers.PlayerList.Nodes;
using PlayerRoles;

namespace Compendium.Custom.Parsers.PlayerList;

public class PlayerListParsing
{
	public List<PlayerListToken> tokens;

	public int pos;

	public PlayerListParsing(List<PlayerListToken> tokens)
	{
		this.tokens = tokens;
	}

	public PlayerListToken Match(PlayerListTokenType[] expected)
	{
		if (pos < tokens.Count)
		{
			PlayerListToken currentToken = tokens[pos];
			if (expected.Any((PlayerListTokenType x) => x.name == currentToken.type.name))
			{
				pos++;
				return currentToken;
			}
		}
		return null;
	}

	public PlayerListToken Require(PlayerListTokenType[] expected)
	{
		PlayerListToken playerListToken = Match(expected);
		if (playerListToken == null)
		{
			throw new Exception($"{expected[0].name} is expected at {pos} position.");
		}
		return playerListToken;
	}

	public List<ReferenceHub> Run(ExpressionNode node, Func<List<ReferenceHub>, string, List<ReferenceHub>> tagPredicate)
	{
		if (node is StatementsNode statementsNode)
		{
			node = statementsNode.codeStrings[0];
		}
		try
		{
			BinOpNode biNode = node as BinOpNode;
			if (biNode != null)
			{
				if (biNode.op.type == PlayerListTokenType.tokenTypeList["ROLE"])
				{
					return (from x in Run(biNode.leftOp, tagPredicate)
						where (int)x.RoleId() == (sbyte)int.Parse(((NumberNode)biNode.rightOp).number.text) && !x.serverRoles.IsInOverwatch
						select x).ToList();
				}
				if (biNode.op.type == PlayerListTokenType.tokenTypeList["TEAM"])
				{
					return (from x in Run(biNode.leftOp, tagPredicate)
						where (uint)x.GetTeam() == (byte)int.Parse(((NumberNode)biNode.rightOp).number.text) && !x.serverRoles.IsInOverwatch
						select x).ToList();
				}
				if (biNode.op.type == PlayerListTokenType.tokenTypeList["TAG"])
				{
					if (tagPredicate == null)
					{
						throw new Exception("Tag predicate not implemented.");
					}
					return tagPredicate((from x in Run(biNode.leftOp, tagPredicate)
						where !x.serverRoles.IsInOverwatch
						select x).ToList(), ((AnyNode)biNode.rightOp).any.text);
				}
				if (biNode.op.type == PlayerListTokenType.tokenTypeList["RAND"])
				{
					List<ReferenceHub> list = (from x in Run(biNode.leftOp, tagPredicate)
						where !x.serverRoles.IsInOverwatch
						select x).ToList();
					List<ReferenceHub> list2 = new List<ReferenceHub>();
					int num = int.Parse(((NumberNode)biNode.rightOp).number.text);
					byte[] array = new byte[num];
					new RNGCryptoServiceProvider().GetBytes(array);
					for (int i = 0; i < num; i++)
					{
						if (list.Count == 0)
						{
							break;
						}
						int index = array[i] * list.Count / 255;
						list2.Add(list[index]);
						list.RemoveAt(index);
					}
					return list2;
				}
				if (biNode.op.type == PlayerListTokenType.tokenTypeList["RANK"])
				{
					string selectorText = ((AnyNode)biNode.rightOp).any.text;
					if (selectorText == "*")
					{
						return (from x in Run(biNode.leftOp, tagPredicate)
							where !string.IsNullOrEmpty(x.serverRoles.Network_myText) && !x.serverRoles.IsInOverwatch
							select x).ToList();
					}
					if (selectorText == "!*")
					{
						return (from x in Run(biNode.leftOp, tagPredicate)
							where string.IsNullOrEmpty(x.serverRoles.Network_myText) && !x.serverRoles.IsInOverwatch
							select x).ToList();
					}
					if (selectorText.StartsWith("!"))
					{
						return (from x in Run(biNode.leftOp, tagPredicate)
							where x.serverRoles.Network_myText != selectorText.Substring(1) && !x.serverRoles.IsInOverwatch
							select x).ToList();
					}
					return (from x in Run(biNode.leftOp, tagPredicate)
						where x.serverRoles.Network_myText == selectorText && !x.serverRoles.IsInOverwatch
						select x).ToList();
				}
			}
			if (node is AllNode)
			{
				return Hub.Hubs.ToList();
			}
			UnOpNode unNode = node as UnOpNode;
			if (unNode != null && unNode.op.type == PlayerListTokenType.tokenTypeList["NAME"])
			{
				return new List<ReferenceHub> { Hub.Hubs.First((ReferenceHub x) => x.Nick().StartsWith(((AnyNode)unNode.operand).any.text)) };
			}
			PlayerNode playerNode = node as PlayerNode;
			if (playerNode != null)
			{
				return new List<ReferenceHub> { Hub.Hubs.First((ReferenceHub x) => x.UserId() == playerNode.playerID.text) };
			}
			if (node is NumberNode numberNode)
			{
				return new List<ReferenceHub> { ReferenceHub.GetHub(int.Parse(numberNode.number.text)) };
			}
			throw new Exception("Error!");
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	public ExpressionNode ParseCode()
	{
		StatementsNode statementsNode = new StatementsNode();
		while (pos < tokens.Count)
		{
			ExpressionNode node = ParseExpression();
			statementsNode.AddNode(node);
		}
		return statementsNode;
	}

	private ExpressionNode ParseExpression()
	{
		if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["NUMBER"] }) != null)
		{
			pos--;
			return (NumberNode)ParseNumber();
		}
		if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["PLAYER"] }) != null)
		{
			pos--;
			return (PlayerNode)ParsePlayer();
		}
		if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["ALL"] }) != null)
		{
			pos--;
			return (AllNode)ParseAll();
		}
		if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["ANY"] }) != null)
		{
			pos--;
			return (AnyNode)ParseAny();
		}
		return ParseFormula();
	}

	private ExpressionNode ParseFormula()
	{
		PlayerListToken playerListToken = Match(new PlayerListTokenType[6]
		{
			PlayerListTokenType.tokenTypeList["NAME"],
			PlayerListTokenType.tokenTypeList["RAND"],
			PlayerListTokenType.tokenTypeList["RANK"],
			PlayerListTokenType.tokenTypeList["ROLE"],
			PlayerListTokenType.tokenTypeList["TEAM"],
			PlayerListTokenType.tokenTypeList["TAG"]
		});
		if (playerListToken != null && Require(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["LPAR"] }) != null)
		{
			if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["NAME"] }) != null)
			{
				AnyNode operand = (AnyNode)ParseAny();
				return new UnOpNode(playerListToken, operand);
			}
			ExpressionNode leftOp = ParseExpression();
			Require(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["COMMA"] });
			if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["TAG"] }) != null)
			{
				AnyNode rightOp = (AnyNode)ParseAny();
				return new BinOpNode(playerListToken, leftOp, rightOp);
			}
			if (Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["RANK"] }) != null)
			{
				AnyNode rightOp2 = (AnyNode)ParseAny();
				return new BinOpNode(playerListToken, leftOp, rightOp2);
			}
			ExpressionNode rightOp3 = ParseExpression();
			Require(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["RPAR"] });
			return new BinOpNode(playerListToken, leftOp, rightOp3);
		}
		throw new Exception($"Operator expected at {pos} position.");
	}

	private ExpressionNode ParseNumber()
	{
		PlayerListToken playerListToken = Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["NUMBER"] });
		if (playerListToken != null)
		{
			return new NumberNode(playerListToken);
		}
		throw new Exception($"Number expected at {pos} position.");
	}

	private ExpressionNode ParsePlayer()
	{
		PlayerListToken playerListToken = Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["PLAYER"] });
		if (playerListToken != null)
		{
			return new PlayerNode(playerListToken);
		}
		throw new Exception($"UserID expected at {pos} position.");
	}

	private ExpressionNode ParseAll()
	{
		PlayerListToken playerListToken = Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["ALL"] });
		if (playerListToken != null)
		{
			return new AllNode(playerListToken);
		}
		throw new Exception($"* expected at {pos} position.");
	}

	private ExpressionNode ParseAny()
	{
		PlayerListToken playerListToken = Match(new PlayerListTokenType[1] { PlayerListTokenType.tokenTypeList["ANY"] });
		if (playerListToken != null)
		{
			return new AnyNode(playerListToken);
		}
		throw new Exception($"\"[^) ]+\" regex string match expected at {pos} position.");
	}
}
