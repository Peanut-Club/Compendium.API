using System;
using helpers.Patching;

namespace Compendium.Custom.Patches.Features;

public static class NextRoundActionPatch
{
	[Patch(typeof(ServerStatic), "StopNextRound", PatchType.Postfix, PatchMethodType.PropertySetter, "Server Action Announcement Patch", new Type[] { })]
	public static void Postfix(ServerStatic.NextRoundAction value)
	{
		if (Plugin.Config.ApiSetttings.ServerActionAnnouncements.TryGetValue(value, out var value2))
		{
			World.Broadcast(value2, 5);
		}
	}
}
