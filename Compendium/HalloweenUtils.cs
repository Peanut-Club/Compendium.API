using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace Compendium;

public static class HalloweenUtils
{
	public static void SpawnBones(Vector3 pos)
	{
		ReferenceHub.HostHub.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
		if (!(ReferenceHub.HostHub.roleManager.CurrentRole is HumanRole humanRole))
		{
			return;
		}
		humanRole.FpcModule.ServerOverridePosition(pos, Vector3.zero);
		BasicRagdoll basicRagdoll = RagdollManager.ServerSpawnRagdoll(ReferenceHub.HostHub, new UniversalDamageHandler(-1f, DeathTranslations.Warhead));
		BasicRagdoll basicRagdoll2 = RagdollManager.ServerSpawnRagdoll(ReferenceHub.HostHub, new Scp3114DamageHandler(basicRagdoll, isStarting: false));
		NetworkServer.Destroy(basicRagdoll.gameObject);
		if ((object)basicRagdoll2 != null && basicRagdoll2 is DynamicRagdoll ragdoll)
		{
			ReferenceHub.HostHub.roleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RemoteAdmin);
			if (ReferenceHub.HostHub.roleManager.CurrentRole is Scp3114Role scp)
			{
				Scp3114RagdollToBonesConverter.ServerConvertNew(scp, ragdoll);
				ReferenceHub.HostHub.roleManager.ServerSetRole(RoleTypeId.None, RoleChangeReason.RemoteAdmin);
			}
		}
	}
}
