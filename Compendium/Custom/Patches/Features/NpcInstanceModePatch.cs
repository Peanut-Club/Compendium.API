using System;
using CentralAuth;
using Compendium.Npc;
using helpers.Patching;

namespace Compendium.Custom.Patches.Features;

public static class NpcInstanceModePatch
{
	[Patch(typeof(PlayerAuthenticationManager), "InstanceMode", PatchType.Prefix, PatchMethodType.PropertyGetter, new Type[] { })]
	public static bool GetPrefix(PlayerAuthenticationManager __instance, ref ClientInstanceMode __result)
	{
		if (NpcHub.IsNpc(__instance._hub))
		{
			__result = ClientInstanceMode.Host;
			return false;
		}
		return true;
	}

	[Patch(typeof(PlayerAuthenticationManager), "InstanceMode", PatchType.Prefix, PatchMethodType.PropertySetter, new Type[] { })]
	public static bool SetPrefix(PlayerAuthenticationManager __instance)
	{
		if (NpcHub.IsNpc(__instance._hub))
		{
			__instance._targetInstanceMode = ClientInstanceMode.Host;
			return false;
		}
		return true;
	}
}
