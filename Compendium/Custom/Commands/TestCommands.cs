using BetterCommands;
using Compendium.Custom.Parsers.PlayerList;

namespace Compendium.Custom.Commands;

public static class TestCommands
{
	[Command("selector", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Tests the player list selector.")]
	public static string SelectorCommand(ReferenceHub sender, PlayerListData list)
	{
		string text = $"Found players: {list.Count}\n";
		for (int i = 0; i < list.Count; i++)
		{
			text += $"[{i}] {list.Matched[i].Nick()}\n";
		}
		return text;
	}
}
