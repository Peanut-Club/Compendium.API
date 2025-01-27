using System.Collections.Generic;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Updating;
using PluginAPI.Events;
using UnityEngine;

namespace Compendium.Processors;

public static class RocketProcessor
{
	private static List<ReferenceHub> Active = new List<ReferenceHub>();

	private static object Lock = new object();

	public static bool IsActive(ReferenceHub hub)
	{
		lock (Lock)
		{
			return Active.Contains(hub);
		}
	}

	public static void Add(ReferenceHub hub)
	{
		lock (Lock)
		{
			if (!Active.Contains(hub))
			{
				Active.Add(hub);
			}
		}
	}

	public static void Remove(ReferenceHub hub)
	{
		lock (Lock)
		{
			Active.Remove(hub);
		}
	}

	[Event]
	private static void OnDeath(PlayerDeathEvent ev)
	{
		lock (Lock)
		{
			Active.Remove(ev.Player.ReferenceHub);
		}
	}

	[Event]
	private static void OnLeft(PlayerLeftEvent ev)
	{
		lock (Lock)
		{
			Active.Remove(ev.Player.ReferenceHub);
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	private static void OnRestart()
	{
		lock (Lock)
		{
			Active.Clear();
		}
	}

	[Update]
	private static void Update()
	{
		lock (Lock)
		{
			for (int i = 0; i < Active.Count; i++)
			{
				Vector3 value = Active[i].Position();
				value.y += 1.2f;
				Active[i].Position(value);
			}
		}
	}
}
