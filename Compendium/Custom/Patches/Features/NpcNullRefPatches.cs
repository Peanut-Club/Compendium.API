using System;
using CentralAuth;
using Compendium.Npc;
using helpers.Patching;
using Mirror;

namespace Compendium.Custom.Patches.Features;

public static class NpcNullRefPatches
{
	[Patch(typeof(CustomNetworkManager), "OnServerDisconnect", PatchType.Prefix, new Type[] { })]
	public static bool CNMPrefix(CustomNetworkManager __instance, NetworkConnectionToClient conn)
	{
		if (conn != null && conn is NpcConnection)
		{
			conn.Disconnect();
			return false;
		}
		return true;
	}

	[Patch(typeof(ServerConsole), "HandlePlayerJoin", PatchType.Prefix, new Type[] { })]
	public static bool HandlePlayerJoinPrefix(ServerConsole __instance, ReferenceHub rh, ClientInstanceMode mode)
	{
		if (rh != null && NpcHub.IsNpc(rh))
		{
			return false;
		}
		return true;
	}
}
