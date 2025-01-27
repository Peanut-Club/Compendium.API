using System;
using helpers;
using helpers.Logging;
using helpers.Verify;
using UnityEngine;

namespace Compendium.Logging;

public class LoggingProxy : LoggerBase
{
	public override void Log(LogBuilder log)
	{
		string text = log.Build();
		if (VerifyUtils.VerifyString(text))
		{
			ServerConsole.AddLog(text, GetColor(text));
			UnityEngine.Debug.Log(text);
		}
	}

	private static ConsoleColor GetColor(string log)
	{
		if (log.Contains("INFO"))
		{
			return ConsoleColor.Green;
		}
		if (log.Contains("ERROR"))
		{
			return ConsoleColor.Red;
		}
		if (log.Contains("WARN"))
		{
			return ConsoleColor.Yellow;
		}
		if (log.Contains("DEBUG"))
		{
			return ConsoleColor.Cyan;
		}
		return ConsoleColor.Magenta;
	}
}
