using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BetterCommands;
using BetterCommands.Management;
using Compendium.Attributes;
using Compendium.Constants;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Updating;
using GameCore;
using helpers;
using helpers.Attributes;
using helpers.Extensions;
using helpers.Patching;
using helpers.Pooling.Pools;
using helpers.Time;
using PluginAPI.Core;

namespace Compendium.Features;

public static class FeatureManager
{
	private static readonly List<Type> _knownFeatures = new List<Type>();

	private static readonly List<IFeature> _features = new List<IFeature>();

	public static IReadOnlyList<IFeature> LoadedFeatures => _features;

	public static IReadOnlyList<Type> RegisteredFeatures => _knownFeatures;

	[Load]
	public static void Reload()
	{
		Unload();
		string[] files = Directory.GetFiles(Directories.ThisFeatures, "*.dll");
		string[] array = files;
		foreach (string text in array)
		{
			try
			{
				byte[] rawAssembly = File.ReadAllBytes(text);
				Assembly assembly = Assembly.Load(rawAssembly);
				Type[] types = assembly.GetTypes();
				Type[] array2 = types;
				foreach (Type type in array2)
				{
					if (Reflection.HasInterface<IFeature>(type))
					{
						if (_knownFeatures.Contains(type))
						{
							Plugin.Warn("Feature '" + type.FullName + "' is already loaded.");
							continue;
						}
						if (_features.Any((IFeature f) => f.GetType() == type))
						{
							Plugin.Warn("Feature '" + type.FullName + "' is already enabled.");
							continue;
						}
						_knownFeatures.Add(type);
						_features.Add(Reflection.Instantiate<IFeature>(type));
					}
				}
			}
			catch (Exception message)
			{
				Plugin.Error("Failed to load file: " + text);
				Plugin.Error(message);
			}
		}
		Plugin.Info($"Found {_features.Count} features!");
		Load();
	}

	public static bool IsRegistered<TFeature>() where TFeature : IFeature
	{
		return _knownFeatures.Contains(typeof(TFeature));
	}

	public static bool IsRegistered(Type type)
	{
		return _knownFeatures.Contains(type);
	}

	public static bool IsInstantiated(Type type)
	{
		IFeature feature;
		return TryGetFeature(type, out feature);
	}

	public static bool IsInstantiated<TFeature>() where TFeature : IFeature
	{
		TFeature feature;
		return TryGetFeature<TFeature>(out feature);
	}

	public static void Enable(string name)
	{
		if (TryGetFeature(name, out var feature))
		{
			Enable(feature);
		}
	}

	public static void Enable<TFeature>() where TFeature : IFeature
	{
		if (TryGetFeature<TFeature>(out var feature))
		{
			Enable(feature);
		}
	}

	public static void Enable(Type type)
	{
		if (TryGetFeature(type, out var feature))
		{
			Enable(feature);
		}
	}

	public static void Enable(IFeature feature)
	{
		if (Plugin.Config.FeatureSettings.Disabled.Remove(feature.Name))
		{
			Plugin.SaveConfig();
		}
		Load(feature);
	}

	public static void Disable(string name)
	{
		if (TryGetFeature(name, out var feature))
		{
			Disable(feature);
		}
	}

	public static void Disable<TFeature>() where TFeature : IFeature
	{
		if (TryGetFeature<TFeature>(out var feature))
		{
			Disable(feature);
		}
	}

	public static void Disable(Type type)
	{
		if (TryGetFeature(type, out var feature))
		{
			Disable(feature);
		}
	}

	public static void Disable(IFeature feature)
	{
		if (!Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name))
		{
			Plugin.Config.FeatureSettings.Disabled.Add(feature.Name);
			Plugin.SaveConfig();
		}
		Unload(feature);
	}

	public static void Load<TFeature>() where TFeature : IFeature
	{
		if (TryGetFeature<TFeature>(out var feature))
		{
			Load(feature);
		}
	}

	public static void Load(Type type)
	{
		if (TryGetFeature(type, out var feature))
		{
			Load(feature);
		}
	}

	public static void Load(string name)
	{
		if (TryGetFeature(name, out var feature))
		{
			Load(feature);
		}
	}

	public static void Load(IFeature feature)
	{
		try
		{
			if (!Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name))
			{
				Singleton.Set(feature);
				Assembly assembly = feature.GetType().Assembly;
				feature.Load();
				AttributeLoader.ExecuteLoadAttributes(assembly);
				if (feature.IsPatch)
				{
					PatchManager.PatchAssemblies(assembly);
				}
				EventRegistry.RegisterEvents(assembly);
				CommandManager.Register(assembly);
				UpdateHandler.Register(assembly);
				AttributeRegistry<RoundStateChangedAttribute>.Register(assembly);
				Plugin.Info("Loaded feature '" + feature.Name + "'");
			}
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed to load feature {feature.Name}:\n{arg}");
		}
	}

	public static void Unload<TFeature>() where TFeature : IFeature
	{
		if (TryGetFeature<TFeature>(out var feature))
		{
			Unload(feature);
		}
	}

	public static void Unload(Type type)
	{
		if (TryGetFeature(type, out var feature))
		{
			Unload(feature);
		}
	}

	public static void Unload(string name)
	{
		if (TryGetFeature(name, out var feature))
		{
			Unload(feature);
		}
	}

	public static void Unload(IFeature feature)
	{
		try
		{
			Assembly assembly = feature.GetType().Assembly;
			AttributeRegistry<RoundStateChangedAttribute>.Unregister(assembly);
			UpdateHandler.Unregister(assembly);
			AttributeLoader.ExecuteUnloadAttributes(assembly);
			if (feature.IsPatch)
			{
				PatchManager.UnpatchAssemblies(assembly);
			}
			EventRegistry.UnregisterEvents(assembly);
			List<CommandData> rList = ListPool<CommandData>.Pool.Get();
			CommandManager.Commands.ForEach(delegate(KeyValuePair<CommandType, HashSet<CommandData>> cmd)
			{
				cmd.Value.ForEach(delegate(CommandData c)
				{
					if (c.DeclaringType.Assembly == assembly)
					{
						rList.Add(c);
					}
				});
			});
			rList.ForEach(delegate(CommandData cmd)
			{
				CommandManager.TryUnregister(cmd.Name, CommandType.RemoteAdmin);
			});
			rList.ForEach(delegate(CommandData cmd)
			{
				CommandManager.TryUnregister(cmd.Name, CommandType.GameConsole);
			});
			rList.ForEach(delegate(CommandData cmd)
			{
				CommandManager.TryUnregister(cmd.Name, CommandType.PlayerConsole);
			});
			rList.ReturnList();
			feature.Unload();
			_features.Remove(feature);
			_knownFeatures.Remove(feature.GetType());
			Singleton.Dispose(feature.GetType());
			Plugin.Info("Unloaded feature '" + feature.Name + "'");
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed to unload feature {feature.Name}:\n{arg}");
		}
	}

	public static void Register<TFeature>() where TFeature : IFeature
	{
		Register(typeof(TFeature));
	}

	public static void Register(Type type)
	{
		if (!_knownFeatures.Contains(type))
		{
			_knownFeatures.Add(type);
			_features.Add(Reflection.Instantiate<IFeature>(type));
		}
	}

	public static void Unregister<TFeature>() where TFeature : IFeature
	{
		Unregister(typeof(TFeature));
	}

	public static void Unregister(IFeature feature)
	{
		if (_knownFeatures.Remove(feature.GetType()))
		{
			_features.Remove(feature);
		}
	}

	public static void Unregister(string name)
	{
		if (TryGetFeature(name, out var feature))
		{
			Unregister(feature);
		}
	}

	public static void Unregister(Type type)
	{
		if (_knownFeatures.Contains(type))
		{
			_knownFeatures.Remove(type);
			_features.Remove(_features.First((IFeature x) => x.GetType() == type));
		}
	}

	public static IFeature GetFeature(string name)
	{
		if (!TryGetFeature(name, out var feature))
		{
			return null;
		}
		return feature;
	}

	public static TFeature GetFeature<TFeature>() where TFeature : IFeature
	{
		if (!TryGetFeature<TFeature>(out var feature))
		{
			return default(TFeature);
		}
		return feature;
	}

	public static IFeature GetFeature(Type type)
	{
		if (!TryGetFeature(type, out var feature))
		{
			return null;
		}
		return feature;
	}

	public static bool TryGetFeature(string name, out IFeature feature)
	{
		feature = _features.FirstOrDefault((IFeature x) => x.Name == name);
		return feature != null;
	}

	public static bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : IFeature
	{
		feature = _features.FirstOrDefault((IFeature x) => x is TFeature).As<TFeature>();
		return feature != null;
	}

	public static bool TryGetFeature(Type type, out IFeature feature)
	{
		feature = _features.FirstOrDefault((IFeature x) => x.GetType() == type);
		return feature != null;
	}

	public static void Load()
	{
		_features.ForEach(Load);
	}

	public static void Unload()
	{
		_features.ForEach(Unload);
		_features.Clear();
		_knownFeatures.Clear();
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		if (Plugin.Config.ApiSetttings.ReloadOnRestart)
		{
			Plugin.LoadConfig();
			ConfigFile.ReloadGameConfigs();
		}
		_features.ForEach(delegate(IFeature feature)
		{
			try
			{
				if (feature.IsEnabled)
				{
					feature.OnWaiting();
				}
			}
			catch (Exception arg)
			{
				Plugin.Error($"Failed to invoke the OnWaiting function of feature {feature.Name}:\n{arg}");
			}
		});
	}

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	private static void OnRestart()
	{
		_features.ForEach(delegate(IFeature feature)
		{
			try
			{
				if (feature.IsEnabled)
				{
					feature.Restart();
				}
			}
			catch (Exception arg)
			{
				Plugin.Error($"Failed to invoke the Restart function of feature {feature.Name}:\n{arg}");
			}
		});
	}

	[Update]
	private static void OnUpdate()
	{
		_features.ForEach(delegate(IFeature feature)
		{
			try
			{
				if (feature.IsEnabled)
				{
					feature.CallUpdate();
				}
			}
			catch (Exception arg)
			{
				Plugin.Error($"Failed to invoke the Update function of feature {feature.Name}:\n{arg}");
			}
		});
	}

	[Command("dfeature", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "df" })]
	[Description("Disables the specified feature.")]
	private static string DisableFeature(Player sender, string featureName)
	{
		if (TryGetFeature(featureName, out var feature))
		{
			if (!feature.IsEnabled)
			{
				return "Feature " + feature.Name + " is already disabled!";
			}
			Disable(feature);
			return "Feature " + feature.Name + " has been disabled!";
		}
		return "Feature " + featureName + " does not exist!";
	}

	[Command("efeature", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "ef" })]
	[Description("Enables the specified feature.")]
	private static string EnableFeature(Player sender, string featureName)
	{
		if (TryGetFeature(featureName, out var feature))
		{
			if (feature.IsEnabled)
			{
				return "Feature " + feature.Name + " is already enabled!";
			}
			Enable(feature);
			return "Feature " + feature.Name + " has been enabled!";
		}
		return "Feature " + featureName + " does not exist!";
	}

	[Command("rfeature", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "rf" })]
	[Description("Reloads the specified feature.")]
	private static string ReloadFeature(Player sender, string featureName)
	{
		if (TryGetFeature(featureName, out var feature))
		{
			if (!feature.IsEnabled)
			{
				return "Feature " + feature.Name + " is disabled!";
			}
			AttributeLoader.ExecuteReloadAttributes(feature.GetType().Assembly);
			feature.Reload();
			return "Feature " + feature.Name + " has been reloaded!";
		}
		return "Feature " + featureName + " does not exist!";
	}

	[Command("lfeatures", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "lf" })]
	[Description("Lists all available features.")]
	private static string ListFeatures(Player sender)
	{
		if (!_features.Any())
		{
			return $"There aren't any loaded features ({_features.Count}/{_knownFeatures.Count})";
		}
		StringBuilder sb = StringBuilderPool.Pool.Get();
		sb.AppendLine($"Showing a list of {_features.Count} features:");
		_features.For(delegate(int i, IFeature feature)
		{
			Assembly assembly = feature.GetType().Assembly;
			sb.AppendLine(string.Format("<b>[{0}] </b> {1} v{2} [{3}]{4}", i + 1, Colors.LightGreen(feature?.Name ?? "UNKNOWN NAME"), assembly.GetName().Version, feature.IsEnabled ? Colors.Green("ENABLED") : Colors.Red("DISABLED"), feature.IsPatch ? " <i>(contains patches)</i>" : ""));
		});
		return StringBuilderPool.Pool.PushReturn(sb);
	}

	[Command("dtfeature", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "dtf" })]
	[Description("Shows all details about a feature.")]
	private static string DetailFeature(Player sender, string featureName)
	{
		if (!_features.TryGetFirst((IFeature f) => f.Name.GetSimilarity(featureName) >= 0.8, out var value))
		{
			return "There are no features matching your query.";
		}
		if (value == null)
		{
			return "The requested feature's instance is null.";
		}
		Type type = value.GetType();
		Assembly assembly = type.Assembly;
		AssemblyName name = assembly.GetName();
		string text = Directories.ThisFeatures + "/" + name.Name + ".dll";
		DateTime dateTime = (File.Exists(text) ? File.GetLastWriteTime(text).ToLocalTime() : DateTime.MinValue);
		string text2 = dateTime.ToString("F") + " (" + (TimeUtils.LocalTime - dateTime).UserFriendlySpan() + " ago)";
		StringBuilder sb = StringBuilderPool.Pool.Get();
		sb.AppendLine("== Feature Detail ==");
		sb.AppendLine("- Name: " + value.Name);
		sb.AppendLine("- Main class: " + type.FullName);
		sb.AppendLine("- Assembly: " + name.FullName);
		sb.AppendLine($"- Version: {name.Version}");
		sb.AppendLine("- File Location: " + text);
		sb.AppendLine("- File Time: " + text2);
		if (value is ConfigFeatureBase configFeatureBase)
		{
			sb.AppendLine("- Config File Location: " + (configFeatureBase.Config?.Path ?? "UNKNOWN"));
		}
		if (value.IsPatch)
		{
			sb.AppendLine("- Contains Patches");
		}
		sb.AppendLine();
		sb.AppendLine("Listing all types ..");
		IOrderedEnumerable<Type> values = from t in assembly.GetTypes()
			orderby t.FullName
			select t;
		values.For(delegate(int i, Type t)
		{
			sb.AppendLine(string.Format("[ {0} ]: {1} ({2})", i, t.FullName, (t.IsSealed && t.IsAbstract) ? "static" : "instance"));
		});
		return StringBuilderPool.Pool.PushReturn(sb);
	}
}
