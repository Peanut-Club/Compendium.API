using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BetterCommands;
using Compendium.Attributes;
using Compendium.Comparison;
using Compendium.Enums;
using Compendium.Value;
using helpers;
using helpers.Attributes;
using helpers.Dynamic;
using helpers.Extensions;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace Compendium.Events;

public static class EventRegistry
{
	private static bool _everExecuted;

	private static List<EventRegistryData> _registry = new List<EventRegistryData>();

	public static bool DebugOverride;

	public static List<ServerEventType> RecordEvents => Plugin.Config.ApiSetttings.EventSettings.RecordEvents;

	public static bool RoundSummary
	{
		get
		{
			if (!Plugin.Config.ApiSetttings.EventSettings.ShowRoundSummary)
			{
				return DebugOverride;
			}
			return true;
		}
	}

	public static bool LogExecutionTime
	{
		get
		{
			if (!Plugin.Config.ApiSetttings.EventSettings.ShowTotalExecution)
			{
				return DebugOverride;
			}
			return true;
		}
	}

	public static bool LogHandlers
	{
		get
		{
			if (!Plugin.Config.ApiSetttings.EventSettings.ShowEventDuration)
			{
				return DebugOverride;
			}
			return true;
		}
	}

	public static double HighestEventDuration => ((from x in _registry
		where x.Stats.LongestTime != -1.0
		orderby x.Stats.LongestTime descending
		select x).FirstOrDefault()?.Stats?.LongestTime).GetValueOrDefault();

	public static double ShortestEventDuration => ((from x in _registry
		where x.Stats.ShortestTime != -1.0
		orderby x.Stats.ShortestTime descending
		select x).LastOrDefault()?.Stats?.ShortestTime).GetValueOrDefault();

	public static double HighestTicksPerSecond => ((from x in _registry
		where x.Stats.TicksWhenLongest != 0.0
		orderby x.Stats.TicksWhenLongest descending
		select x).FirstOrDefault()?.Stats?.TicksWhenLongest).GetValueOrDefault();

	[Load]
	private static void Initialize()
	{
		EventManager.Proxy = Proxy;
	}

	[Unload]
	private static void Unload()
	{
		EventManager.Proxy = null;
		_registry.Clear();
	}

	private static object Proxy(object arg1, Type type, Event @event, IEventArguments arguments)
	{
		try
		{
			if (!_registry.Any((EventRegistryData ev) => ev.Type == arguments.BaseType))
			{
				return true;
			}
			_everExecuted = true;
			DateTime now = DateTime.Now;
			IEnumerable<EventRegistryData> enumerable = _registry.Where((EventRegistryData x) => x.Type == arguments.BaseType);
			bool result = true;
			ValueReference valueReference = new ValueReference(arg1, type);
			foreach (EventRegistryData item in enumerable)
			{
				DateTime now2 = DateTime.Now;
				EventUtils.TryInvoke(item, arguments, valueReference, out result);
				if (RecordEvents.Contains(item.Type))
				{
					DateTime now3 = DateTime.Now;
					TimeSpan timeSpan = TimeSpan.FromTicks((now3 - now2).Ticks);
					item.Stats.Record(timeSpan.TotalMilliseconds);
					if (LogHandlers)
					{
						Plugin.Debug($"Finished executing '{item.Type}' handler '{item.Target.Method.ToLogName()}' in {timeSpan.TotalMilliseconds} ms");
					}
				}
			}
			if (valueReference.Value == null)
			{
				valueReference.Value = result;
			}
			if (LogExecutionTime)
			{
				DateTime now4 = DateTime.Now;
				TimeSpan timeSpan2 = TimeSpan.FromTicks((now4 - now).Ticks);
				Plugin.Debug($"Total Event Execution of {arguments.BaseType} took {timeSpan2.TotalMilliseconds} ms.");
			}
			if (arg1 != null && valueReference.Value != null && arg1.GetType() == valueReference.Value.GetType())
			{
				return valueReference.Value;
			}
			return result;
		}
		catch (Exception message)
		{
			Plugin.Error($"Caught an exception while executing event '{arguments.BaseType}'");
			Plugin.Error(message);
			return arg1;
		}
	}

	public static void RegisterEvents(object instance)
	{
		if (instance != null)
		{
			RegisterEvents(instance.GetType(), instance);
		}
	}

	public static void RegisterEvents(Assembly assembly)
	{
		assembly.ForEachType(delegate(Type t)
		{
			RegisterEvents(t);
		});
	}

	public static void RegisterEvents(Type type, object instance = null)
	{
		type.ForEachMethod(delegate(MethodInfo m)
		{
			RegisterEvents(m, skipAttributeCheck: false, instance);
		});
	}

	public static void RegisterEvents(MethodInfo method, bool skipAttributeCheck, object instance = null)
	{
		try
		{
			if (!method.DeclaringType.Namespace.StartsWith("System") && method.IsDefined(typeof(EventAttribute), inherit: false) && !IsRegistered(method, instance) && EventUtils.TryCreateEventData(method, skipAttributeCheck, instance, out var registryData))
			{
				_registry.Add(registryData);
			}
		}
		catch (Exception message)
		{
			Plugin.Error("An error occured while registering event '" + method.ToLogName() + "'");
			Plugin.Error(message);
		}
	}

	public static bool IsRegistered(MethodInfo method, object instance = null)
	{
		if (!EventUtils.TryValidateInstance(method, ref instance))
		{
			return false;
		}
		method = DynamicMethodCache.GetOriginalMethod(method);
		EventRegistryData value;
		return _registry.TryGetFirst((EventRegistryData ev) => ev.Target.Method == method && NullableObjectComparison.Compare(ev.Target.Target, instance), out value);
	}

	public static void UnregisterEvents(object instance)
	{
		if (instance != null)
		{
			UnregisterEvents(instance.GetType(), instance);
		}
	}

	public static void UnregisterEvents(Assembly assembly)
	{
		assembly.ForEachType(delegate(Type t)
		{
			UnregisterEvents(t);
		});
	}

	public static void UnregisterEvents(Type type, object instance = null)
	{
		type.ForEachMethod(delegate(MethodInfo m)
		{
			UnregisterEvents(m, instance);
		});
	}

	public static bool UnregisterEvents(MethodInfo method, object instance = null)
	{
		if (!EventUtils.TryValidateInstance(method, ref instance))
		{
			return false;
		}
		method = DynamicMethodCache.GetOriginalMethod(method);
		return _registry.RemoveAll((EventRegistryData ev) => ev.Target.Method == method && NullableObjectComparison.Compare(ev.Target.Target, instance)) > 0;
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		if (!_everExecuted)
		{
			return;
		}
		StringBuilder sb = Pools.PoolStringBuilder();
		Dictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>> dict = Pools.PoolDictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>>();
		_registry.ForEach(delegate(EventRegistryData ev)
		{
			if (ev.Stats != null && ev.Stats.LongestTime != -1.0 && ev.Stats.ShortestTime != -1.0 && ev.Stats.AverageTime != -1.0 && ev.Stats.LastTime != -1.0 && !(ev.Stats.TicksWhenLongest <= 0.0) && ev.Stats.Executions > 0)
			{
				if (!dict.ContainsKey(ev.Type))
				{
					dict[ev.Type] = Pools.PoolList<Tuple<string, double, double, double, double, double, int>>();
				}
				dict[ev.Type].Add(new Tuple<string, double, double, double, double, double, int>(ev.Target.Method.ToLogName(), ev.Stats.LongestTime, ev.Stats.ShortestTime, ev.Stats.AverageTime, ev.Stats.LastTime, ev.Stats.TicksWhenLongest, ev.Stats.Executions));
			}
		});
		sb.AppendLine();
		dict.ForEach(delegate(KeyValuePair<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>> p)
		{
			if (p.Value.Any())
			{
				sb.AppendLine($"== EVENT: {p.Key} ({p.Value.Count} handler(s)) ==");
				p.Value.ForEach(delegate(Tuple<string, double, double, double, double, double, int> stats)
				{
					sb.AppendLine($"    > {stats.Item1} = L: {stats.Item2} ms;S: {stats.Item3} ms;A: {stats.Item4} ms;LS: {stats.Item5} ms;TPS: {stats.Item6};NUM: {stats.Item7}");
				});
				p.Value.ReturnList();
			}
		});
		dict.ReturnDictionary();
		string text = sb.ReturnStringBuilderValue();
		if (!string.IsNullOrWhiteSpace(text))
		{
			Plugin.Info(text);
		}
		_registry.For(delegate(int _, EventRegistryData ev)
		{
			ev.Stats?.Reset();
		});
	}

	[Command("event", new BetterCommands.CommandType[]
	{
		BetterCommands.CommandType.RemoteAdmin,
		BetterCommands.CommandType.GameConsole
	})]
	private static string EventCommand(ReferenceHub sender, string id)
	{
		switch (id)
		{
		case "handlers":
		{
			IOrderedEnumerable<EventRegistryData> values = _registry.OrderByDescending((EventRegistryData x) => x.Type);
			StringBuilder sb2 = Pools.PoolStringBuilder();
			values.ForEach(delegate(EventRegistryData h)
			{
				StringBuilder stringBuilder = sb2;
				object[] array = new object[4]
				{
					h.Type,
					h.Target.Method.ToLogName(),
					null,
					null
				};
				object[] buffer = h.Buffer;
				array[2] = ((buffer != null) ? buffer.Length : (-1));
				array[3] = h.Buffer?.Count((object x) => x != null) ?? (-1);
				stringBuilder.AppendLine(string.Format("{0} {1} {2} {3}", array));
			});
			return sb2.ReturnStringBuilderValue();
		}
		case "stats":
		{
			StringBuilder sb = Pools.PoolStringBuilder();
			Dictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>> dict = Pools.PoolDictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>>();
			_registry.ForEach(delegate(EventRegistryData ev)
			{
				if (ev.Stats != null && ev.Stats.LongestTime != -1.0 && ev.Stats.ShortestTime != -1.0 && ev.Stats.AverageTime != -1.0 && ev.Stats.LastTime != -1.0 && !(ev.Stats.TicksWhenLongest <= 0.0) && ev.Stats.Executions > 0)
				{
					if (!dict.ContainsKey(ev.Type))
					{
						dict[ev.Type] = Pools.PoolList<Tuple<string, double, double, double, double, double, int>>();
					}
					dict[ev.Type].Add(new Tuple<string, double, double, double, double, double, int>(ev.Target.Method.ToLogName(), ev.Stats.LongestTime, ev.Stats.ShortestTime, ev.Stats.AverageTime, ev.Stats.LastTime, ev.Stats.TicksWhenLongest, ev.Stats.Executions));
				}
			});
			sb.AppendLine();
			dict.ForEach(delegate(KeyValuePair<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>> p)
			{
				if (p.Value.Any())
				{
					sb.AppendLine($"== EVENT: {p.Key} ({p.Value.Count} handler(s)) ==");
					p.Value.ForEach(delegate(Tuple<string, double, double, double, double, double, int> stats)
					{
						sb.AppendLine($"    > {stats.Item1} = L: {stats.Item2} ms;S: {stats.Item3} ms;A: {stats.Item4} ms;LS: {stats.Item5} ms;TPS: {stats.Item6};NUM: {stats.Item7}");
					});
					p.Value.ReturnList();
				}
			});
			dict.ReturnDictionary();
			return sb.ReturnStringBuilderValue();
		}
		case "log":
			DebugOverride = !DebugOverride;
			if (!DebugOverride)
			{
				return "Debug disabled";
			}
			return "Debug enabled";
		case "reset":
			_registry.ForEach(delegate(EventRegistryData r)
			{
				r.Stats.Reset();
			});
			return "Stats reset.";
		default:
			return "Unknown ID";
		}
	}
}
