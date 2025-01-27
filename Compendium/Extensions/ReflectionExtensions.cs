using System;
using PluginAPI.Core.Interfaces;

namespace Compendium.Extensions;

public static class ReflectionExtensions
{
	public static bool IsPlayerType(this Type type)
	{
		return typeof(IPlayer).IsAssignableFrom(type);
	}
}
