using System;
using Compendium.Npc;
using helpers.Patching;
using PlayerRoles.RoleAssign;
using PlayerRoles.Spectating;

namespace Compendium.Custom.Patches.Features;

public static class NpcSpawnPatches
{
	[Patch(typeof(SpectatorRole), "ReadyToRespawn", PatchType.Prefix, PatchMethodType.PropertyGetter, new Type[] { })]
	public static bool CanSpawnPatch(SpectatorRole __instance, ref bool __result)
	{
		if (__instance._lastOwner != null && NpcHub.IsNpc(__instance._lastOwner))
		{
			return __result = false;
		}
		return true;
	}

	[Patch(typeof(RoleAssigner), "CheckPlayer", PatchType.Prefix, new Type[] { })]
	public static bool AssignPatch(ReferenceHub hub, ref bool __result)
	{
		if (hub != null && NpcHub.IsNpc(hub))
		{
			return __result = false;
		}
		return true;
	}
}
