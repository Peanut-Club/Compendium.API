using System;
using System.Globalization;
using BetterCommands.Parsing;
using helpers.Results;
using UnityEngine;

namespace Compendium.Custom.Parsers;

public class VectorParser : ICommandArgumentParser
{
	public static void Init()
	{
		CommandArgumentParser.AddParser<VectorParser>(typeof(Vector3));
		ArgumentUtils.SetFriendlyName(typeof(Vector3), "A combination of 3 coordinates (X, Y and Z)");
	}

	public IResult Parse(string value, Type type)
	{
		string[] array = value.Split(new char[1] { ' ' });
		if (array.Length != 3)
		{
			return Result.Error("Incorrect Vector formatting, correct: \"X Y Z\"");
		}
		if (!float.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return Result.Error("Failed to parse the X axis value (" + array[0] + ")");
		}
		if (!float.TryParse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var result2))
		{
			return Result.Error("Failed to parse the Y axis value (" + array[1] + ")");
		}
		if (!float.TryParse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var result3))
		{
			return Result.Error("Failed to parse the Z axis value (" + array[2] + ")");
		}
		return Result.Success(new Vector3(result, result2, result3));
	}
}
