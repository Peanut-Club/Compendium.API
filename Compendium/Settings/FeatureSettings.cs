using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Compendium.Settings;

public class FeatureSettings
{
	public class ConfigVector
	{
		public float X { get; set; }

		public float Y { get; set; }

		public float Z { get; set; }

		public Vector3 ToUnityVector()
		{
			return new Vector3(X, Y, Z);
		}

		public static ConfigVector FromUnityVector(Vector3 vector)
		{
			return new ConfigVector
			{
				X = vector.x,
				Y = vector.y,
				Z = vector.z
			};
		}
	}

	[Description("A list of disabled features.")]
	public List<string> Disabled { get; set; } = new List<string>();


	[Description("A list of features with enabled debug messages.")]
	public List<string> Debug { get; set; } = new List<string>();


	[Description("A list of light positions to be spawned when the round starts.")]
	public List<ConfigVector> LightPositions { get; set; } = new List<ConfigVector>
	{
		new ConfigVector
		{
			X = 0f,
			Y = 0f,
			Z = 0f
		}
	};


	[Description("How many NPCs to spawn at the start of a round.")]
	public int NpcPreload { get; set; } = 5;

}
