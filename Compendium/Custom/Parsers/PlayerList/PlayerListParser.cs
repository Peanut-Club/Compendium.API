using System;
using System.Collections.Generic;
using BetterCommands.Parsing;
using helpers.Results;

namespace Compendium.Custom.Parsers.PlayerList;

public class PlayerListParser : ICommandArgumentParser
{
	internal static void Load()
	{
		CommandArgumentParser.AddParser<PlayerListParser>(typeof(PlayerListData));
	}

	public IResult Parse(string value, Type type)
	{
		PlayerListSelector playerListSelector = new PlayerListSelector();
		List<ReferenceHub> list = playerListSelector.Select(value);
		if (list.Count < 1)
		{
			return Result.Error("Failed to find any matching players.");
		}
		return Result.Success(new PlayerListData(list));
	}
}
