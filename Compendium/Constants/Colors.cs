namespace Compendium.Constants;

public static class Colors
{
	public const string LightGreenValue = "#33FFA5";

	public const string RedValue = "#FF0000";

	public const string GreenValue = "#90FF33";

	public static string LightGreen(string str)
	{
		return "<color=#33FFA5>" + str + "</color>";
	}

	public static string Red(string str)
	{
		return "<color=#FF0000>" + str + "</color>";
	}

	public static string Green(string str)
	{
		return "<color=#90FF33>" + str + "</color>";
	}
}
