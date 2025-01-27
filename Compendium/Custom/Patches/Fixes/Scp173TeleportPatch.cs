using System;
using helpers.Patching;
using PlayerRoles.PlayableScps.Scp173;
using UnityEngine;

namespace Compendium.Custom.Patches.Fixes;

public static class Scp173TeleportPatch
{
	[Patch(typeof(Scp173MovementModule), "CheckTeleportPosition", PatchType.Prefix, PatchMethodType.Method, new Type[] { })]
	public static bool Prefix(Scp173MovementModule __instance, RaycastHit hit, ref Vector3 groundPoint, ref bool __result)
	{
		__result = false;
		groundPoint = Vector3.zero;
		float radius = __instance.CharacterControllerSettings.Radius;
		float num = radius * 1.2f;
		Vector3 vector = hit.point + hit.normal * num;
		if (Physics.CheckSphere(vector, radius, Scp173MovementModule.TpMask))
		{
			return false;
		}
		if (Physics.Raycast(hit.point, hit.normal, num, Scp173MovementModule.TpMask))
		{
			return false;
		}
		if (!Physics.SphereCast(vector, radius, Vector3.down, out var hitInfo, 3.6f, Scp173MovementModule.TpMask))
		{
			return false;
		}
		if (!Physics.SphereCast(new Ray(vector, Vector3.down), radius * 0.5f, hitInfo.distance + 0.6f, Scp173MovementModule.TpMask))
		{
			return false;
		}
		if (Vector3.Dot(Vector3.up, hitInfo.normal) < 0.15f)
		{
			return false;
		}
		if (!Physics.SphereCast(vector, radius, Vector3.up, out var hitInfo2, 7.2f, Scp173MovementModule.TpMask))
		{
			hitInfo2.point = vector + Vector3.up * 7.2f;
		}
		if (Mathf.Abs(hitInfo.point.y - hitInfo2.point.y) < __instance.CharacterControllerSettings.Height)
		{
			return false;
        }
        if (hitInfo.collider.TryGetComponent<PitKiller>(out var _)) {
            return false;
        }
        groundPoint = hitInfo.point + (hitInfo.normal + Vector3.down) * radius;
		if (groundPoint.y >= 1008.5f)
		{
			return false;
		}
		__result = true;
		return false;
	}
}
