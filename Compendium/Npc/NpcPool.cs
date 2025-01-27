using System;
using helpers.Pooling;

namespace Compendium.Npc;

public class NpcPool : Pool<NpcHub>
{
	public static NpcPool Pool { get; } = new NpcPool();


	public bool HasAny => base.Queue.Count > 0;

	public NpcPool()
		: base((Action<NpcHub>)delegate(NpcHub npc)
		{
			npc.UnPool();
		}, (Action<NpcHub>)delegate(NpcHub npc)
		{
			npc.Pool();
		}, (Func<NpcHub>)null)
	{
	}
}
