using System;
using System.Collections.Generic;
using helpers.Pooling.Pools;

namespace Compendium.Extensions;

public static class CollectionExtensions
{
	public static int FindIndex<T>(this HashSet<T> set, Func<T, bool> validator)
	{
		int num = 0;
		foreach (T item in set)
		{
			if (validator(item))
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public static void SetElementAtIndex<T>(this HashSet<T> set, int index, T value)
	{
		List<T> list = ListPool<T>.Pool.Get(set);
		list[index] = value;
		for (int i = 0; i < list.Count; i++)
		{
			set.Add(list[i]);
		}
		ListPool<T>.Pool.Push(list);
	}
}
