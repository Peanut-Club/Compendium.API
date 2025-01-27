using UnityEngine;

namespace Compendium.Positions;

public class Position
{
	private Vector3 pos;

	private bool posSet;

	public string Name { get; set; } = "Výchozí jméno.";


	public string Description { get; set; } = "Žádný popis.";


	public float X { get; set; }

	public float Y { get; set; }

	public float Z { get; set; }

	public Vector3 GetPosition()
	{
		if (!posSet)
		{
			pos = new Vector3(X, Y, Z);
			posSet = true;
		}
		return pos;
	}
}
