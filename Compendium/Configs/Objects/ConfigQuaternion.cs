using UnityEngine;

namespace Compendium.Configs.Objects;

public class ConfigQuaternion
{
	public float x { get; set; }

	public float y { get; set; }

	public float z { get; set; }

	public float w { get; set; }

	public Quaternion Convert()
	{
		return new Quaternion(x, y, z, w);
	}

	public static ConfigQuaternion Get(Quaternion quaternion)
	{
		return Get(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
	}

	public static ConfigQuaternion Get(float x, float y, float z, float w)
	{
		return new ConfigQuaternion
		{
			x = x,
			y = y,
			z = z,
			w = w
		};
	}
}
