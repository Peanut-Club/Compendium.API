using UnityEngine;

namespace Compendium.Configs.Objects;

public class ConfigVector
{
	public float x { get; set; }

	public float y { get; set; }

	public float z { get; set; }

	public Vector3 Convert()
	{
		return new Vector3(x, y, z);
	}

	public static ConfigVector Get(Vector3 vector)
	{
		return Get(vector.x, vector.y, vector.z);
	}

	public static ConfigVector Get(float x, float y, float z)
	{
		return new ConfigVector
		{
			x = x,
			y = y,
			z = z
		};
	}
}
