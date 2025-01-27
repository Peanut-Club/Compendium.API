using System;
using UnityEngine;

namespace Compendium.Extensions;

public static class UnityExtensions
{
	public static float DistanceSquared(this Vector3 a, Vector3 b)
	{
		return (a - b).sqrMagnitude;
	}

	public static float DistanceSquared(this GameObject a, GameObject b)
	{
		return (a.transform.position - b.transform.position).sqrMagnitude;
	}

	public static float DistanceSquared(this GameObject a, Vector3 b)
	{
		return (a.transform.position - b).sqrMagnitude;
	}

	public static float DistanceSquared(this Vector3 a, GameObject b)
	{
		return (a - b.transform.position).sqrMagnitude;
	}

	public static float DistanceSquared(this Component a, Component b)
	{
		return (a.transform.position - b.transform.position).sqrMagnitude;
	}

	public static float DistanceSquared(this Component a, Vector3 b)
	{
		return (a.transform.position - b).sqrMagnitude;
	}

	public static float DistanceSquared(this Vector3 a, Component b)
	{
		return (a - b.transform.position).sqrMagnitude;
	}

	public static float DistanceSquared(this Transform a, Transform b)
	{
		return (a.position - b.position).sqrMagnitude;
	}

	public static float DistanceSquared(this Transform a, Vector3 b)
	{
		return (a.position - b).sqrMagnitude;
	}

	public static float DistanceSquared(this Vector3 a, Transform b)
	{
		return (a - b.transform.position).sqrMagnitude;
	}

	public static bool IsWithinDistance(this Vector3 a, Vector3 b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this GameObject a, GameObject b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this GameObject a, Vector3 b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Vector3 a, GameObject b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Component a, Component b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Component a, Vector3 b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Vector3 a, Component b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Transform a, Transform b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Transform a, Vector3 b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static bool IsWithinDistance(this Vector3 a, Transform b, float maxDistance)
	{
		return a.DistanceSquared(b) <= maxDistance * maxDistance;
	}

	public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
	{
		if (!(gameObject != null))
		{
			throw new ArgumentNullException("gameObject");
		}
		if (!gameObject.TryGetComponent<T>(out var component))
		{
			return gameObject.AddComponent<T>();
		}
		return component;
	}

	public static T GetOrAddComponent<T>(this Component component) where T : Component
	{
		return component.gameObject.GetOrAddComponent<T>();
	}

	public static bool TryGet<TComponent>(this GameObject component, out TComponent result)
	{
		if (component.TryGetComponent<TComponent>(out result))
		{
			return true;
		}
		result = component.GetComponentInParent<TComponent>();
		if (result != null)
		{
			return true;
		}
		result = component.GetComponentInChildren<TComponent>();
		return result != null;
	}

	public static bool DestroyComponent<T>(this GameObject gameObject) where T : Component
	{
		if ((object)gameObject == null || !gameObject.TryGet<T>(out var result))
		{
			return false;
		}
		UnityEngine.Object.Destroy(result);
		return true;
	}

	public static bool DestroyComponent<T>(this Component component) where T : Component
	{
		if ((object)component == null || !component.gameObject.TryGet<T>(out var result))
		{
			return false;
		}
		UnityEngine.Object.Destroy(result);
		return true;
	}

	public static bool DestroyImmediate<T>(this GameObject gameObject) where T : Component
	{
		if (!gameObject.TryGet<T>(out var result))
		{
			return false;
		}
		UnityEngine.Object.DestroyImmediate(result);
		return true;
	}

	public static bool DestroyImmediate<T>(this Component component) where T : Component
	{
		if (!component.gameObject.TryGet<T>(out var result))
		{
			return false;
		}
		UnityEngine.Object.DestroyImmediate(result);
		return true;
	}

	public static (ushort horizontal, ushort vertical) ToClientUShorts(this Quaternion rotation)
	{
		if (rotation.eulerAngles.z != 0f)
		{
			rotation = Quaternion.LookRotation(rotation * Vector3.forward, Vector3.up);
		}
		float y = rotation.eulerAngles.y;
		float num = 0f - rotation.eulerAngles.x;
		if (num < -90f)
		{
			num += 360f;
		}
		else if (num > 270f)
		{
			num -= 360f;
		}
		return (ToHorizontal(y), ToVertical(num));
		static ushort ToHorizontal(float horizontal)
		{
			horizontal = Mathf.Clamp(horizontal, 0f, 360f);
			return (ushort)Mathf.RoundToInt(horizontal * 182.04167f);
		}
		static ushort ToVertical(float vertical)
		{
			vertical = Mathf.Clamp(vertical, -88f, 88f) + 88f;
			return (ushort)Mathf.RoundToInt(vertical * 372.35794f);
		}
	}
}
