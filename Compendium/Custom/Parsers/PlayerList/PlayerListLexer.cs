using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Compendium.Custom.Parsers.PlayerList;

public class PlayerListLexer
{
	private string code;

	private int pos;

	private List<PlayerListToken> tokenList = new List<PlayerListToken>();

	public PlayerListLexer(string code)
	{
		this.code = code;
	}

	public List<PlayerListToken> LexicalAnalysis()
	{
		while (NextToken())
		{
		}
		return tokenList.Where((PlayerListToken x) => x.type.name != PlayerListTokenType.tokenTypeList["SPACE"].name).ToList();
	}

	private bool NextToken()
	{
		if (pos >= code.Length)
		{
			return false;
		}
		List<PlayerListTokenType> list = PlayerListTokenType.tokenTypeList.Values.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			PlayerListTokenType playerListTokenType = list[i];
			Regex regex = new Regex("^" + playerListTokenType.regex);
			string value = regex.Match(code.Substring(pos)).Value;
			if (!string.IsNullOrEmpty(value))
			{
				PlayerListToken item = new PlayerListToken(playerListTokenType, value, pos);
				pos += value.Length;
				tokenList.Add(item);
				return true;
			}
		}
		throw new Exception($"An error was found at {pos} position.");
	}
}
