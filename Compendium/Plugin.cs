using System;
using System.Reflection;
using BetterCommands;
using BetterCommands.Permissions;
using Compendium.Attributes;
using Compendium.Custom.Parsers;
using Compendium.Custom.Parsers.PlayerList;
using Compendium.Events;
using Compendium.Features;
using Compendium.Logging;
using Compendium.Updating;
using helpers;
using helpers.Attributes;
using helpers.Logging.Loggers;
using helpers.Patching;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Loader;
using Utils.NonAllocLINQ;

namespace Compendium;

[LogSource("Compendium Core")]
public class Plugin
{
	[PluginConfig]
	public Config ConfigInstance;

	public PluginHandler HandlerInstance;

	public static Plugin Instance { get; private set; }

	public static Config Config => Instance?.ConfigInstance ?? null;

	public static PluginHandler Handler => Instance?.HandlerInstance ?? null;

	[PluginEntryPoint("Compendium API", "3.9.0", "A huge API for each Compendium component.", "marchellc_")]
	[PluginPriority(LoadPriority.Lowest)]
	public void Load()
	{
		if (Instance != null)
		{
			throw new InvalidOperationException("This plugin has already been loaded!");
		}
		Instance = this;
		HandlerInstance = PluginHandler.Get(this);
		if (Config.LogSettings.UseLoggingProxy)
		{
			helpers.Log.AddLogger<LoggingProxy>();
		}
		PlayerDataRecordParser.Load();
		PlayerListParser.Load();
		StaffGroupParser.Load();
		VectorParser.Init();
		QuaternionParser.Init();
		Directories.Load();
		Info("Loading ..");
		Calls.OnFalse(delegate
		{
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				PatchManager.PatchAssemblies(executingAssembly);
				EventRegistry.RegisterEvents(executingAssembly);
				AttributeLoader.ExecuteLoadAttributes(executingAssembly);
				AttributeRegistry<RoundStateChangedAttribute>.Register();
				UpdateHandler.Register();
				helpers.Log.AddLogger(new FileLogger(FileLoggerMode.AppendToFile, 0, $"Server {ServerStatic.ServerPort}.txt"));
				LoadConfig();
				Info("Loaded!");
			}
			catch (Exception message)
			{
				Error("Startup failed!");
				Error(message);
			}
		}, () => ServerStatic.PermissionsHandler == null);
	}

	[PluginUnload]
	public void Unload()
	{
		Assembly exec = Assembly.GetExecutingAssembly();
		PatchManager.UnpatchAssemblies(exec);
		EventRegistry.UnregisterEvents(exec);
		AttributeLoader.ExecuteUnloadAttributes(exec);
		UpdateHandler.Unregister();
		AssemblyLoader.Plugins.ForEachKey(delegate(Assembly pl)
		{
			if (!(pl == exec))
			{
				PatchManager.UnpatchAssemblies(pl);
				EventRegistry.UnregisterEvents(pl);
				AttributeLoader.ExecuteUnloadAttributes(pl);
				UpdateHandler.Unregister(pl);
			}
		});
		SaveConfig();
		Instance = null;
		HandlerInstance = null;
		ConfigInstance = null;
	}

	[PluginReload]
	public void Reload()
	{
		LoadConfig();
		AttributeLoader.ExecuteReloadAttributes(Assembly.GetExecutingAssembly());
		FeatureManager.LoadedFeatures.ForEach(delegate(IFeature f)
		{
			AttributeLoader.ExecuteReloadAttributes(f.GetType().Assembly);
		});
	}

	public static void SaveConfig()
	{
		Handler?.SaveConfig(Instance, "ConfigInstance");
	}

	public static void LoadConfig()
	{
		Handler?.LoadConfig(Instance, "ConfigInstance");
	}

	public static void ModifyConfig(Action<Config> action)
	{
		action?.Invoke(Config);
		SaveConfig();
	}

	public static void Debug(object message)
	{
		helpers.Log.Debug(message);
		if (Config.LogSettings.ShowDebug)
		{
			PluginAPI.Core.Log.Debug(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
		}
	}

	public static void Error(object message)
	{
		if (!Config.LogSettings.UseLoggingProxy)
		{
			helpers.Log.Error(message);
		}
		PluginAPI.Core.Log.Error(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
	}

	public static void Warn(object message)
	{
		if (!Config.LogSettings.UseLoggingProxy)
		{
			helpers.Log.Warn(message);
		}
		PluginAPI.Core.Log.Warning(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
	}

	public static void Info(object message)
	{
		if (!Config.LogSettings.UseLoggingProxy)
		{
			helpers.Log.Info(message);
		}
		PluginAPI.Core.Log.Info(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
	}

	[Command("announcerestart", new BetterCommands.CommandType[]
	{
		BetterCommands.CommandType.GameConsole,
		BetterCommands.CommandType.RemoteAdmin,
		BetterCommands.CommandType.PlayerConsole
	})]
	[Permission(PermissionLevel.Administrator)]
	[CommandAliases(new object[] { "ar" })]
	[Description("Announces a server restart and then restarts in 10 seconds.")]
	private static string AnnounceRestartCommand(ReferenceHub sender)
	{
		World.Broadcast("<color=red><b>Server se restartuje za 10 sekund!</b></color>", 10);
		Calls.Delay(10f, delegate
		{
			Server.Restart();
		});
		return "Restarting in 10 seconds ..";
	}

	[Command("creload", new BetterCommands.CommandType[]
	{
		BetterCommands.CommandType.GameConsole,
		BetterCommands.CommandType.RemoteAdmin
	})]
	[Description("Reloads Compendium's core API.")]
	[Permission(PermissionLevel.Administrator)]
	private static string ReloadCommand(ReferenceHub sender)
	{
		if (Instance == null)
		{
			return "Instance is inactive.";
		}
		Instance.Reload();
		return "Reloaded!";
	}

	[Command("creloadcfg", new BetterCommands.CommandType[]
	{
		BetterCommands.CommandType.RemoteAdmin,
		BetterCommands.CommandType.GameConsole
	})]
	[Description("Reloads the API's config file.")]
	[Permission(PermissionLevel.Administrator)]
	private static string ReloadConfigCommand(ReferenceHub sender)
	{
		LoadConfig();
		return "Config file reloaded.";
	}
}
