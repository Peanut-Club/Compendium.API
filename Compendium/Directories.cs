using System.IO;
using Newtonsoft.Json;
using PluginAPI.Helpers;

namespace Compendium;

public static class Directories
{
	public static string MainPath => Paths.SecretLab + "/compendium";

	public static string DataPath => MainPath + "/data";

	public static string FeaturesPath => MainPath + "/features";

	public static string ConfigPath => MainPath + "/configs";

	public static string ThisConfigs
	{
		get
		{
			if (Plugin.Config.ApiSetttings.GlobalDirectories.Contains("config") && !Plugin.Config.ApiSetttings.InstanceDirectories.Contains("config"))
			{
				return ConfigPath;
			}
			return $"{MainPath}/configs_{ServerStatic.ServerPort}";
		}
	}

	public static string ThisData
	{
		get
		{
			if (Plugin.Config.ApiSetttings.GlobalDirectories.Contains("data") && !Plugin.Config.ApiSetttings.InstanceDirectories.Contains("data"))
			{
				return DataPath;
			}
			return $"{MainPath}/data_{ServerStatic.ServerPort}";
		}
	}

	public static string ThisFeatures
	{
		get
		{
			if (Plugin.Config.ApiSetttings.GlobalDirectories.Contains("features") && !Plugin.Config.ApiSetttings.InstanceDirectories.Contains("features"))
			{
				return FeaturesPath;
			}
			return $"{MainPath}/features_{ServerStatic.ServerPort}";
		}
	}

	public static T GetData<T>(string fileName, string dataId, bool useGlobal, T defaultValue)
	{
		if (!fileName.EndsWith(".json"))
		{
			fileName += ".json";
		}
		string dataPath = GetDataPath(fileName, dataId, useGlobal);
		if (!FileManager.FileExists(dataPath))
		{
			FileManager.WriteStringToFile(JsonConvert.SerializeObject(defaultValue, Formatting.Indented), dataPath);
			return defaultValue;
		}
		string value = FileManager.ReadAllText(dataPath);
		if (!string.IsNullOrWhiteSpace(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(value);
			}
			catch
			{
				return defaultValue;
			}
		}
		return defaultValue;
	}

	public static void SetData(string fileName, string dataId, bool useGlobal, object data)
	{
		if (!fileName.EndsWith(".json"))
		{
			fileName += ".json";
		}
		string dataPath = GetDataPath(fileName, dataId, useGlobal);
		string data2 = JsonConvert.SerializeObject(data, Formatting.Indented);
		FileManager.WriteStringToFile(data2, dataPath);
	}

	public static string GetDataPath(string fileName, string dataId = null, bool useGlobal = true)
	{
		string text = "";
		text = (string.IsNullOrWhiteSpace(dataId) ? (ThisData + "/" + fileName) : (Plugin.Config.ApiSetttings.InstanceDirectories.Contains(dataId) ? $"{MainPath}/data_{ServerStatic.ServerPort}/{fileName}" : (Plugin.Config.ApiSetttings.GlobalDirectories.Contains(dataId) ? (DataPath + "/" + fileName) : (useGlobal ? (ThisData + "/" + fileName) : $"{MainPath}/data_{ServerStatic.ServerPort}/{fileName}"))));
		string directoryName = Path.GetDirectoryName(text);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		return text;
	}

	internal static void Load()
	{
		Plugin.Info("Loading directories ..");
		Plugin.Info("Main directory: " + MainPath);
		Plugin.Info("Config directory: " + ThisConfigs);
		Plugin.Info("Data directory: " + ThisData);
		Plugin.Info("Features directory: " + ThisFeatures);
		if (!Directory.Exists(MainPath))
		{
			Directory.CreateDirectory(MainPath);
		}
		if (!Directory.Exists(ThisConfigs))
		{
			Directory.CreateDirectory(ThisConfigs);
		}
		if (!Directory.Exists(ThisData))
		{
			Directory.CreateDirectory(ThisData);
		}
		if (!Directory.Exists(ThisFeatures))
		{
			Directory.CreateDirectory(ThisFeatures);
		}
	}
}
