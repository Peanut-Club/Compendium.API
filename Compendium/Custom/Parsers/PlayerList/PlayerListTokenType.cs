using System.Collections.Generic;

namespace Compendium.Custom.Parsers.PlayerList;

public class PlayerListTokenType
{
	public string name;

	public string regex;

	public static readonly Dictionary<string, PlayerListTokenType> tokenTypeList = new Dictionary<string, PlayerListTokenType>
	{
		{
			"PLAYER",
			new PlayerListTokenType("PLAYER", "[0-9]{17}@steam|[0-9]{18}@discord")
		},
		{
			"NUMBER",
			new PlayerListTokenType("NUMBER", "[0-9]*")
		},
		{
			"LPAR",
			new PlayerListTokenType("LPAR", "\\(")
		},
		{
			"RPAR",
			new PlayerListTokenType("RPAR", "\\)")
		},
		{
			"COMMA",
			new PlayerListTokenType("COMMA", ",")
		},
		{
			"ALL",
			new PlayerListTokenType("ALL", "\\*")
		},
		{
			"SPACE",
			new PlayerListTokenType("SPACE", " ")
		},
		{
			"RAND",
			new PlayerListTokenType("RAND", "rand")
		},
		{
			"RANK",
			new PlayerListTokenType("RANK", "rank")
		},
		{
			"ROLE",
			new PlayerListTokenType("ROLE", "role")
		},
		{
			"TEAM",
			new PlayerListTokenType("TEAM", "team")
		},
		{
			"NAME",
			new PlayerListTokenType("NAME", "name")
		},
		{
			"TAG",
			new PlayerListTokenType("TAG", "tag")
		},
		{
			"ANY",
			new PlayerListTokenType("ANY", "[^) ]+")
		}
	};

	public PlayerListTokenType(string name, string regex)
	{
		this.name = name;
		this.regex = regex;
	}
}
