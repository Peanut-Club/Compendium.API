using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Compendium.Logging;
using helpers;
using helpers.Json;
using PluginAPI.Core;

namespace Compendium.Features;

public static class FLog
{
	public static void Warn(object message, params LogParameter[] parameters)
	{
		StackTrace stackTrace = new StackTrace();
		StackFrame stackFrame = stackTrace.GetFrames().Skip(1).First();
		MethodBase method = stackFrame.GetMethod();
		Type declaringType = method.DeclaringType;
		Assembly assembly = declaringType.Assembly;
		string text = message.ToString();
		if (!TryGetLogName(assembly, out var name))
		{
			throw new InvalidOperationException("Failed to find log name for type: " + declaringType.FullName);
		}
		if (parameters != null && parameters.Any())
		{
			text += $"\nParameters ({parameters.Length}):\n{parameters.ToJson()}";
		}
		PluginAPI.Core.Log.Warning(text, name);
	}

	public static void Debug(object message, params LogParameter[] parameters)
	{
		StackTrace stackTrace = new StackTrace();
		StackFrame stackFrame = stackTrace.GetFrames().Skip(1).First();
		MethodBase method = stackFrame.GetMethod();
		Type declaringType = method.DeclaringType;
		Assembly assembly = declaringType.Assembly;
		string text = message.ToString();
		if (assembly.CanDebug(out var feature))
		{
			if (parameters != null && parameters.Any())
			{
				text += $"\nParameters ({parameters.Length}):\n{parameters.ToJson()}";
			}
			PluginAPI.Core.Log.Debug(text, debugEnabled: true, feature.Name);
		}
	}

	public static void Error(object message, params LogParameter[] parameters)
	{
		StackTrace stackTrace = new StackTrace();
		StackFrame stackFrame = stackTrace.GetFrames().Skip(1).First();
		MethodBase method = stackFrame.GetMethod();
		Type declaringType = method.DeclaringType;
		Assembly assembly = declaringType.Assembly;
		string text = message.ToString();
		if (!TryGetLogName(assembly, out var name))
		{
			throw new InvalidOperationException("Failed to find log name for type: " + declaringType.FullName);
		}
		if (parameters != null && parameters.Any())
		{
			text += $"\nParameters ({parameters.Length}):\n{parameters.ToJson()}";
		}
		PluginAPI.Core.Log.Error(text, name);
	}

	public static void Info(object message, params LogParameter[] parameters)
	{
		StackTrace stackTrace = new StackTrace();
		StackFrame stackFrame = stackTrace.GetFrames().Skip(1).First();
		MethodBase method = stackFrame.GetMethod();
		Type declaringType = method.DeclaringType;
		Assembly assembly = declaringType.Assembly;
		string text = message.ToString();
		if (!TryGetLogName(assembly, out var name))
		{
			throw new InvalidOperationException("Failed to find log name for type: " + declaringType.FullName);
		}
		if (parameters != null && parameters.Any())
		{
			text += $"\nParameters ({parameters.Length}):\n{parameters.ToJson()}";
		}
		PluginAPI.Core.Log.Info(text, name);
	}

	public static bool CanDebug(this Assembly assembly, out IFeature feature)
	{
		if (FeatureManager.LoadedFeatures.TryGetFirst((IFeature f) => f.GetType().Assembly == assembly, out feature))
		{
			if (!Plugin.Config.FeatureSettings.Debug.Contains(feature.Name))
			{
				return Plugin.Config.FeatureSettings.Debug.Contains("*");
			}
			return true;
		}
		return false;
	}

	private static bool TryGetLogName(Assembly assembly, out string name)
	{
		if (FeatureManager.LoadedFeatures.TryGetFirst((IFeature f) => f.GetType().Assembly == assembly, out var value))
		{
			name = value.Name ?? value.GetType().Name;
			return true;
		}
		name = null;
		return false;
	}
}
