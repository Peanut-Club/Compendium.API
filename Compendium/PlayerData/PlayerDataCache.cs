using System;
using System.Collections.Generic;
using helpers.Time;

namespace Compendium.PlayerData;

public class PlayerDataCache
{
	public DateTime LastChangeTime { get; set; } = DateTime.MinValue;


	public string LastValue { get; set; }

	public Dictionary<DateTime, string> AllValues { get; set; } = new Dictionary<DateTime, string>();


	public bool Compare(string newValue)
	{
		if (newValue == null)
		{
			return false;
		}
		if (LastValue == null || LastValue != newValue)
		{
			LastValue = newValue;
			LastChangeTime = TimeUtils.LocalTime;
			AllValues[LastChangeTime] = LastValue;
			return true;
		}
		return false;
	}
}
