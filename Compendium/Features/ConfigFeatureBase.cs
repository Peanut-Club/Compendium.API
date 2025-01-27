using System.IO;
using helpers.Configuration;
using helpers.Events;

namespace Compendium.Features;

public class ConfigFeatureBase : IFeature
{
	private bool _isEnabled;

	public readonly EventProvider OnLoad = new EventProvider();

	public readonly EventProvider OnUnload = new EventProvider();

	public readonly EventProvider OnReload = new EventProvider();

	public readonly EventProvider OnUpdate = new EventProvider();

	public readonly EventProvider OnRestart = new EventProvider();

	public readonly EventProvider OnWaitingForPlayers = new EventProvider();

	public virtual string Name => "Config Feature Base";

	public virtual bool IsPatch => true;

	public bool IsEnabled => _isEnabled;

	public string Path
	{
		get
		{
			if (!CanBeShared || !Plugin.Config.ApiSetttings.GlobalDirectories.Contains(Name))
			{
				return $"{Directories.MainPath}/configs_{ServerStatic.ServerPort}/{Name}.ini";
			}
			return Directories.ThisConfigs + "/" + Name + ".ini";
		}
	}

	public virtual bool CanBeShared { get; } = true;


	public ConfigHandler Config { get; private set; }

	public virtual void CallUpdate()
	{
		OnUpdate.Invoke();
	}

	public virtual void Load()
	{
		_isEnabled = true;
		LoadConfig();
		OnLoad.Invoke();
	}

	public virtual void Reload()
	{
		OnReload.Invoke();
	}

	public virtual void Restart()
	{
		OnRestart.Invoke();
	}

	public virtual void OnWaiting()
	{
		Config?.Load();
		OnWaitingForPlayers.Invoke();
		if (Plugin.Config.ApiSetttings.ReloadOnRestart)
		{
			Reload();
		}
	}

	public virtual void Unload()
	{
		_isEnabled = false;
		OnUnload.Invoke();
		Config?.Save();
		Config = null;
	}

	public void SaveConfig()
	{
		Config?.Save();
	}

	public void LoadConfig()
	{
		if (Config == null)
		{
			string directoryName = System.IO.Path.GetDirectoryName(Path);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			Config = new ConfigHandler(Path);
			Config.BindAll(GetType().Assembly);
		}
		Config.Load();
	}
}
