using System;
using System.Collections.Generic;
using BetterCommands.Parsing;
using Compendium.Staff;
using helpers;
using helpers.Results;

namespace Compendium.Custom.Parsers;

public class StaffGroupParser : ICommandArgumentParser
{
	internal static void Load()
	{
		CommandArgumentParser.AddParser<StaffGroupParser>(typeof(StaffGroup));
		ArgumentUtils.SetFriendlyName(typeof(StaffGroup), "a staff group's key");
	}

	public IResult Parse(string value, Type type)
	{
		if (StaffHandler.Groups.TryGetFirst((KeyValuePair<string, StaffGroup> g) => string.Equals(value, g.Value.Key, StringComparison.OrdinalIgnoreCase), out var value2) && value2.Value != null)
		{
			return Result.Success(value2.Value);
		}
		return Result.Error("No matching groups were found.");
	}
}
