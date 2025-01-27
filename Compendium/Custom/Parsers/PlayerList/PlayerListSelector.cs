using System;
using System.Collections.Generic;

namespace Compendium.Custom.Parsers.PlayerList;

public class PlayerListSelector
{
	private Func<List<ReferenceHub>, string, List<ReferenceHub>> tagPredicate;

	public PlayerListSelector(Func<List<ReferenceHub>, string, List<ReferenceHub>> tagPredicate)
	{
		this.tagPredicate = tagPredicate;
	}

	public PlayerListSelector()
		: this(null)
	{
	}

	public List<ReferenceHub> Select(string arg)
	{
		PlayerListParsing playerListParsing = new PlayerListParsing(new PlayerListLexer(arg.ToLower()).LexicalAnalysis());
		return playerListParsing.Run(playerListParsing.ParseCode(), tagPredicate);
	}
}
