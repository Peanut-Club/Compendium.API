using System;
using System.Globalization;
using BetterCommands.Parsing;
using helpers.Results;
using UnityEngine;

namespace Compendium.Custom.Parsers;

public class QuaternionParser : ICommandArgumentParser
{
	public static void Init()
	{
		CommandArgumentParser.AddParser<QuaternionParser>(typeof(Quaternion));
		ArgumentUtils.SetFriendlyName(typeof(Quaternion), "A combination of 4 coordinates (X, Y, Z and W)");
	}

	public IResult Parse(string value, Type type)
	{
		string[] array = value.Split(new char[1] { ' ' });
		if (array.Length != 4)
		{
			return Result.Error("Incorrect Qauternion formatting, correct: \"X Y Z W\"");
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
		if (!float.TryParse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var result4))
		{
			return Result.Error("Failed to parse the Z axis value (" + array[3] + ")");
		}
		return Result.Success(new Quaternion(result, result2, result3, result4));
	}
}
