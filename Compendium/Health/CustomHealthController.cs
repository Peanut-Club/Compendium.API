using System;
using System.Collections.Generic;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using helpers.Patching;
using PlayerRoles;
using PluginAPI.Events;

namespace Compendium.Health;

public static class CustomHealthController
{
	private static readonly Dictionary<uint, CustomHealthData> _health = new Dictionary<uint, CustomHealthData>();

	public static void SetMaxHealth(this ReferenceHub hub, float maxHealth, bool keepOnRoleChange = true)
	{
		_health[hub.netId] = new CustomHealthData(keepOnRoleChange, maxHealth);
	}

	public static void RestoreMaxHealth(this ReferenceHub hub)
	{
		_health.Remove(hub.netId);
	}

	[Event]
	private static void OnRoleChange(PlayerChangeRoleEvent ev)
	{
		try
		{
			if (_health.ContainsKey(ev.Player.NetworkId) && !_health[ev.Player.NetworkId].KeepOnRole)
			{
				_health.Remove(ev.Player.NetworkId);
			}
		}
		catch
		{
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	private static void OnRestart()
	{
		_health.Clear();
	}

	[Patch(typeof(HumanRole), "MaxHealth", PatchType.Prefix, PatchMethodType.PropertyGetter, new Type[] { })]
	private static bool Patch(HumanRole __instance, ref float __result)
	{
		if (!__instance.TryGetOwner(out var hub))
		{
			return true;
		}
		if ((object)hub == null || !_health.ContainsKey(hub.netId))
		{
			return true;
		}
		__result = _health[hub.netId].Value;
		return false;
	}
}
