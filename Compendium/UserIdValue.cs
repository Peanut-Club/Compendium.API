using System;
using Compendium.Enums;
using helpers.Extensions;
using helpers.Values;

namespace Compendium;

public struct UserIdValue : IValue<string>
{
	private string _value;

	public const int DiscordIdLength = 18;

	public const int SteamIdLength = 17;

	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!value.TrySplit('@', removeEmptyOrWhitespace: true, 2, out var splits))
			{
				if (!long.TryParse(value, out var result))
				{
					throw new Exception();
				}
				if (!TryGetType(value.Length, out var userIdType))
				{
					throw new Exception();
				}
				ClearId = value;
				Id = result;
				Type = userIdType;
				TypeRepresentation = userIdType.ToString().ToLower();
				_value = value + "@" + TypeRepresentation;
				return;
			}
			string text = splits[0];
			string idType = splits[1];
			if (!long.TryParse(text, out var result2))
			{
				throw new Exception();
			}
			if (!TryGetType(idType, out var type) && !TryGetType(text.Length, out type))
			{
				throw new Exception();
			}
			ClearId = text;
			TypeRepresentation = type.ToString().ToLower();
			Id = result2;
			Type = type;
			_value = value;
		}
	}

	public string ClearId { get; private set; }

	public string TypeRepresentation { get; private set; }

	public long Id { get; private set; }

	public UserIdType Type { get; private set; }

	public UserIdValue(string id)
	{
		_value = null;
		ClearId = null;
		TypeRepresentation = null;
		Id = 0L;
		Type = UserIdType.Northwood;
		Value = id;
	}

	public static bool TryParse(string id, out UserIdValue value)
	{
		try
		{
			value = new UserIdValue(id);
			return true;
		}
		catch
		{
			value = default(UserIdValue);
			return false;
		}
	}

	private static bool TryGetType(int length, out UserIdType userIdType)
	{
		switch (length)
		{
		case 18:
			userIdType = UserIdType.Discord;
			return true;
		case 17:
			userIdType = UserIdType.Steam;
			return true;
		default:
			userIdType = UserIdType.Unknown;
			return false;
		}
	}

	private static bool TryGetType(string idType, out UserIdType type)
	{
		idType = idType.ToLower();
		switch (idType)
		{
		case "northwood":
			type = UserIdType.Northwood;
			return true;
		case "patreon":
			type = UserIdType.Patreon;
			return true;
		case "steam":
			type = UserIdType.Steam;
			return true;
		case "discord":
			type = UserIdType.Discord;
			return true;
		default:
			type = UserIdType.Unknown;
			return false;
		}
	}
}
