using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BetterCommands;
using helpers;
using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Binary;
using helpers.Random;
using Utils.NonAllocLINQ;

namespace Compendium.Sounds;

public static class AudioStore
{
	private static BinaryImage _manifestImage;

	private static Dictionary<string, string> _manifest;

	private static Dictionary<string, byte[]> _preloaded;

	public static string DirectoryPath { get; } = Directories.ThisData + "/AudioFiles";


	public static string ManifestFilePath { get; } = DirectoryPath + "/SavedManifest";


	public static IReadOnlyDictionary<string, string> Manifest => _manifest;

	public static bool TryGet(string id, out byte[] oggBytes)
	{
		if (_preloaded.TryGetValue(id, out oggBytes))
		{
			return true;
		}
		if (Manifest.TryGetValue(id, out var value) && File.Exists(value))
		{
			oggBytes = File.ReadAllBytes(value);
			return true;
		}
		oggBytes = null;
		return false;
	}

	public static void Save(string id, byte[] oggBytes)
	{
		if (Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains(id) || Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains("*"))
		{
			_preloaded[id] = oggBytes;
			Plugin.Info("Added audio '" + id + "' to preloaded files.");
		}
		if (_manifest.TryGetValue(id, out var value))
		{
			if (_preloaded.ContainsKey(id))
			{
				_preloaded[id] = oggBytes;
			}
			File.WriteAllBytes(value, oggBytes);
			Plugin.Info($"Overwritten audio '{id}' in the manifest ({oggBytes.Length})");
		}
		else
		{
			value = DirectoryPath + "/" + RandomGeneration.Default.GetReadableString(20).RemovePathUnsafe().Replace("/", "");
			File.WriteAllBytes(value, oggBytes);
			_manifest[id] = value;
			Save();
			Plugin.Info($"Saved audio '{id}' to the manifest ({oggBytes.Length} bytes).");
		}
	}

	[Load]
	[Reload]
	public static void Load()
	{
		if (!Directory.Exists(DirectoryPath))
		{
			Directory.CreateDirectory(DirectoryPath);
		}
		if (_manifestImage != null)
		{
			_manifestImage.Load();
			if (!_manifestImage.TryGetFirst<Dictionary<string, string>>(out _manifest))
			{
				Save();
			}
			Plugin.Info("Audio storage reloaded.");
			ReloadManifestMan();
			return;
		}
		_manifest = new Dictionary<string, string>();
		_preloaded = new Dictionary<string, byte[]>();
		_manifestImage = new BinaryImage(ManifestFilePath);
		_manifestImage.Load();
		if (!_manifestImage.TryGetFirst<Dictionary<string, string>>(out _manifest))
		{
			Save();
		}
		ReloadManifestMan();
		Plugin.Info("Audio storage loaded.");
	}

	private static void ReloadManifestMan()
	{
		Plugin.Info("Reloading the manifest ..");
		if (_manifest == null)
		{
			_manifest = new Dictionary<string, string>();
		}
		if (!_manifestImage.TryGetFirst<Dictionary<string, string>>(out _manifest))
		{
			Save();
		}
		string[] files = Directory.GetFiles(DirectoryPath);
		string[] array = files;
		foreach (string path in array)
		{
			switch (Path.GetFileNameWithoutExtension(path))
			{
			case "SavedManifest":
			case "ffmpeg":
			case "ffprobe":
				continue;
			}
			if (!_manifest.TryGetKey(Path.GetFullPath(path), out var key))
			{
				_manifest.Add(key = Path.GetFileNameWithoutExtension(path), Path.GetFullPath(path));
			}
		}
		List<string> removeList = new List<string>();
		_manifest.ForEach(delegate(KeyValuePair<string, string> pair)
		{
			if (!File.Exists(pair.Value))
			{
				removeList.Add(pair.Key);
			}
		});
		removeList.ForEach(delegate(string id)
		{
			_manifest.Remove(id);
		});
		_preloaded.Clear();
		_manifest.ForEach(delegate(KeyValuePair<string, string> pair)
		{
			if (Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains(pair.Key) || Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains("*"))
			{
				if (File.Exists(pair.Value))
				{
					_preloaded[pair.Key] = File.ReadAllBytes(pair.Value);
					Plugin.Info("Preloaded audio '" + pair.Key + "' from file '" + Path.GetFileName(pair.Value) + "'");
				}
				else
				{
					Plugin.Warn("Failed to preload '" + pair.Key + "': file '" + Path.GetFileName(pair.Value) + "' does not exist!");
				}
			}
		});
		Save();
	}

	[Unload]
	public static void Unload()
	{
		Save();
		_manifestImage.Clear();
		_manifestImage = null;
	}

	[Command("armanifest", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Reloads the audio storage manifest.")]
	private static string ReloadManifest(ReferenceHub sender)
	{
		ReloadManifestMan();
		return "Reloaded the storage manifest.";
	}

	[Command("listmanifest", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Views all files in the manifest.")]
	private static string ListManifest(ReferenceHub sender)
	{
		if (!_manifest.Any())
		{
			return "There are no files in the manifest.";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"Showing {_manifest.Count} files in the manifest:");
		_manifest.For(delegate(int i, KeyValuePair<string, string> pair)
		{
			sb.AppendLine($"[{i + 1}] {pair.Key} ({Path.GetFileName(pair.Value)})");
		});
		return sb.ToString();
	}

	[Command("clearmanifest", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Clears the manifest.")]
	private static string ClearManifest(ReferenceHub sender, bool deleteFiles)
	{
		if (deleteFiles)
		{
			string[] files = Directory.GetFiles(DirectoryPath);
			string[] array = files;
			foreach (string path in array)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
				switch (fileNameWithoutExtension)
				{
				case "SavedManifest":
				case "ffmpeg":
				case "ffprobe":
					continue;
				}
				File.Delete(path);
				sender.Message("Deleted file: " + fileNameWithoutExtension, isRemoteAdmin: true);
			}
		}
		_manifest.Clear();
		_preloaded.Clear();
		Save();
		return "Cleared the audio manifest.";
	}

	[Command("deletemanifest", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Removes an audio file from the manifest.")]
	private static string DeleteManifest(ReferenceHub sender, string id, bool deleteFile)
	{
		if (_manifest.TryGetValue(id, out var value) && deleteFile)
		{
			File.Delete(value);
		}
		_manifest.Remove(id);
		_preloaded.Remove(id);
		Save();
		return "Removed '" + id + "' from the manifest.";
	}

	public static void Save()
	{
		if (_manifest == null)
		{
			_manifest = new Dictionary<string, string>();
		}
		if (_manifestImage != null)
		{
			_manifestImage.Clear();
			_manifestImage.Add(_manifest);
			_manifestImage.Save();
		}
	}
}
