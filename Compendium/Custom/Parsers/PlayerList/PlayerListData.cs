using System;
using System.Collections.Generic;
using System.Linq;
using Compendium.Extensions;
using PlayerRoles;
using UnityEngine;

namespace Compendium.Custom.Parsers.PlayerList;

public struct PlayerListData
{
	public readonly ReferenceHub[] Matched;

	public readonly ReferenceHub[] Staff;

	public readonly ReferenceHub[] Dead;

	public readonly ReferenceHub[] Alive;

	public readonly int Count;

	public readonly int StaffCount;

	public readonly int DeadCount;

	public readonly int AliveCount;

	public readonly bool Any;

	public readonly bool AnyStaff;

	public readonly bool AnyDead;

	public readonly bool AnyAlive;

	public PlayerListData(IEnumerable<ReferenceHub> matched)
	{
		Matched = matched.ToArray();
		Staff = matched.Where((ReferenceHub m) => m.IsStaff()).ToArray();
		Dead = matched.Where((ReferenceHub m) => !m.IsAlive()).ToArray();
		Alive = matched.Where((ReferenceHub m) => m.IsAlive()).ToArray();
		Count = Matched.Length;
		StaffCount = Staff.Length;
		DeadCount = Dead.Length;
		AliveCount = Alive.Length;
		Any = Matched.Length != 0;
		AnyStaff = Staff.Length != 0;
		AnyDead = Dead.Length != 0;
		AnyAlive = Alive.Length != 0;
	}

	public void ForEach(Action<ReferenceHub> action)
	{
		for (int i = 0; i < Matched.Length; i++)
		{
			action(Matched[i]);
		}
	}

	public void ForEach(Predicate<ReferenceHub> predicate, Action<ReferenceHub> action)
	{
		for (int i = 0; i < Matched.Length; i++)
		{
			if (!predicate(Matched[i]))
			{
				action(Matched[i]);
			}
		}
	}

	public void ForEachInRadius(Vector3 position, float distance, Action<ReferenceHub> action)
	{
		ForEach((ReferenceHub hub) => hub.Position().IsWithinDistance(position, distance), action);
	}
}
