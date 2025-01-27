using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Compendium.Extensions;

public static class ColorHelper
{
	public static IReadOnlyDictionary<string, string> NorthwoodApprovedColorCodes { get; } = new Dictionary<string, string>
	{
		{ "pink", "#FF96DE" },
		{ "red", "#C50000" },
		{ "white", "#FFFFFF" },
		{ "brown", "#944710" },
		{ "silver", "#A0A0A0" },
		{ "light_green", "#32CD32" },
		{ "crimson", "#DC143C" },
		{ "cyan", "#00B7EB" },
		{ "aqua", "#00FFFF" },
		{ "deep_pink", "#FF1493" },
		{ "tomato", "#FF6448" },
		{ "yellow", "#FAFF86" },
		{ "magenta", "#FF0090" },
		{ "blue_green", "#4DFFB8" },
		{ "orange", "#FF9966" },
		{ "lime", "#BFFF00" },
		{ "green", "#228B22" },
		{ "emerald", "#50C878" },
		{ "carmine", "#960018" },
		{ "nickel", "#727472" },
		{ "mint", "#98FB98" },
		{ "army_green", "#4B5320" },
		{ "pumpkin", "#EE7600" },
		{ "black", "#000000" }
	};


	public static Color ParseColor(string html)
	{
		if (!ColorUtility.TryParseHtmlString(html, out var color))
		{
			return Color.black;
		}
		return color;
	}

	public static Color GetNorthwoodApprovedColor(string colorName)
	{
		if (!NorthwoodApprovedColorCodes.TryGetValue(colorName, out var value))
		{
			return Color.black;
		}
		return ParseColor(value);
	}

	public static string GetClosestNorthwoodColor(string color)
	{
		return ParseColor(color).GetClosestNorthwoodColor();
	}

	public static string GetClosestNorthwoodColor(this Color color)
	{
		if (!NorthwoodApprovedColorCodes.TryGetValue(color.GetClosestNorthwoodColorName(), out var value))
		{
			return "#FFFFFF";
		}
		return value;
	}

	public static string GetClosestNorthwoodColorName(string color)
	{
		return ParseColor(color).GetClosestNorthwoodColorName();
	}

	public static string GetClosestNorthwoodColorName(this Color color)
	{
		float[] array = color.Hsv();
		float value = array[0] * 36000f + array[1] * 100f + array[2];
		return NorthwoodApprovedColorCodes.ToDictionary((KeyValuePair<string, string> k) => k.Key, (KeyValuePair<string, string> v) => ParseColor(v.Value).Hsv()).OrderBy(delegate(KeyValuePair<string, float[]> e)
		{
			float[] value2 = e.Value;
			return Mathf.Abs(value2[0] * 36000f + value2[1] * 100f + value2[2] - value);
		}).FirstOrDefault()
			.Key;
	}

	public static float[] Hsv(this Color color)
	{
		Color.RGBToHSV(color, out var H, out var S, out var V);
		return new float[3] { H, S, V };
	}

	public static string ToHex(this Color color, bool includeHash = true, bool includeAlpha = true)
	{
		return (includeHash ? "#" : "") + (includeAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color));
	}

	public static string ToHex(this Color32 color, bool includeHash = true, bool includeAlpha = true)
	{
		return ((Color)color).ToHex(includeHash, includeAlpha);
	}

	public static Color ToLightColor(this Color color)
	{
		return new Color(color.r / 255f, color.g / 255f, color.b / 255f);
	}
}
