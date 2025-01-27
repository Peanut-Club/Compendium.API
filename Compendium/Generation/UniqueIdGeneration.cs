using System.Collections.Generic;
using Compendium.IO.Saving;
using helpers.Attributes;
using helpers.Random;

namespace Compendium.Generation;

public static class UniqueIdGeneration
{
	private static readonly List<string> _generated = new List<string>();

	private static SaveFile<UniqueIdSaveFile> _generationStorage;

	public static IReadOnlyList<string> Generated => _generated;

	public static bool IsPreviouslyGenerated(string id)
	{
		return _generationStorage.Data.IDs.Contains(id);
	}

	public static string Generate(int length = 10)
	{
		string text = RandomGeneration.Default.GetReadableString(length).TrimEnd(new char[1] { '=' });
		while (IsPreviouslyGenerated(text))
		{
			text = RandomGeneration.Default.GetReadableString(length).TrimEnd(new char[1] { '=' });
		}
		_generationStorage.Data.IDs.Add(text);
		_generationStorage.Save();
		return text;
	}

	[Load]
	private static void Initialize()
	{
		_generationStorage = new SaveFile<UniqueIdSaveFile>(Directories.ThisData + "/SavedGenerations");
	}

	[Unload]
	private static void Unload()
	{
		_generationStorage.Save();
	}
}
