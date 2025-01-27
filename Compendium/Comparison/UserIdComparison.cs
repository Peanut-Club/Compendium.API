namespace Compendium.Comparison;

public static class UserIdComparison
{
	public static bool Compare(string uid, string uid2)
	{
		if (!UserIdValue.TryParse(uid, out var value))
		{
			return false;
		}
		if (!UserIdValue.TryParse(uid2, out var value2))
		{
			return false;
		}
		return value.Value == value2.Value;
	}
}
