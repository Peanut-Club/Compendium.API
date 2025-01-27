using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Compendium.Enums;
using Compendium.Events;
using helpers;
using helpers.CustomReflect;
using helpers.Extensions;
using MEC;
using UnityEngine;

namespace Compendium.Updating;

public static class UpdateHandler
{
	private static List<UpdateData> _updates;

	private static CoroutineHandle _cor;

	private static volatile ConcurrentQueue<UpdateData> _updatesQueue;

	public static Thread Thread;

	static UpdateHandler()
	{
		_updates = new List<UpdateData>();
		_updatesQueue = new ConcurrentQueue<UpdateData>();
		_cor = Timing.RunCoroutine(Handler());
		Thread = new Thread((ThreadStart)async delegate
		{
			while (true)
			{
				await Task.Delay(100);
				UpdateData result;
				while (_updatesQueue.TryDequeue(out result))
				{
					result.DoCall();
				}
				result = null;
			}
		});
		Thread.Start();
	}

	public static bool Unregister()
	{
		return Unregister(Assembly.GetCallingAssembly());
	}

	public static bool Unregister(Assembly assembly)
	{
		return assembly.GetTypes().Any((Type t) => Unregister(t, null));
	}

	public static bool Unregister(Type type, object target)
	{
		return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any((MethodInfo m) => Unregister(m, target));
	}

	public static bool Unregister(Action handler)
	{
		return Unregister(handler.Method, handler.Target);
	}

	public static bool Unregister(Action<UpdateData> handler)
	{
		return Unregister(handler.Method, handler.Target);
	}

	public static bool Unregister(MethodInfo method, object target)
	{
		return _updates.RemoveAll((UpdateData u) => u.Is(method, target)) > 0;
	}

	public static void Register()
	{
		Register(Assembly.GetCallingAssembly());
	}

	public static void Register(Assembly assembly)
	{
		assembly.ForEachType(delegate(Type t)
		{
			Register(t, null);
		});
	}

	public static void Register(Type type, object target)
	{
		type.ForEachMethod(delegate(MethodInfo m)
		{
			if (m.IsDefined(typeof(UpdateAttribute)))
			{
				Register(m, target);
			}
		});
	}

	public static void Register(Action handler, bool isUnity = true, bool isWaiting = true, bool isRestarting = true, int delay = -1)
	{
		if (_updates.Any((UpdateData u) => u.Is(handler.Method, handler.Target)))
		{
			Plugin.Error("Cannot register update method " + handler.Method.ToLogName() + ": already registered");
		}
		else
		{
			_updates.Add(new UpdateData(isUnity, isWaiting, isRestarting, delay, handler));
		}
	}

	public static void Register(Action<UpdateData> handler, bool isUnity = true, bool isWaiting = true, bool isRestarting = true, int delay = -1)
	{
		if (_updates.Any((UpdateData u) => u.Is(handler.Method, handler.Target)))
		{
			Plugin.Error("Cannot register update method " + handler.Method.ToLogName() + ": already registered");
		}
		else
		{
			_updates.Add(new UpdateData(isUnity, isWaiting, isRestarting, delay, handler));
		}
	}

	public static void Register(MethodInfo method, object target)
	{
		if (_updates.Any((UpdateData u) => u.Is(method, target)))
		{
			Plugin.Error("Cannot register update method " + method.ToLogName() + ": already registered");
			return;
		}
		if (!method.TryGetAttribute<UpdateAttribute>(out var attributeValue))
		{
			Plugin.Error("Cannot register update method " + method.ToLogName() + ": missing attribute");
			return;
		}
		if (!EventUtils.TryValidateInstance(method, ref target))
		{
			Plugin.Error("Cannot register update method " + method.ToLogName() + ": invalid target instance");
			return;
		}
		if (!attributeValue.IsUnity)
		{
			List<Instruction> instructions = MethodBodyReader.GetInstructions(method);
			List<MethodBase> list = new List<MethodBase>();
			for (int i = 0; i < instructions.Count; i++)
			{
				if (instructions[i].Operand is MethodBase methodBase && (Reflection.HasType<UnityEngine.Object>(methodBase.DeclaringType) || methodBase.DeclaringType.Assembly.FullName.Contains("Assembly-CSharp")))
				{
					list.Add(methodBase);
				}
			}
			if (list.Count > 0)
			{
				Plugin.Error("Cannot register method " + method.ToLogName() + ": method running on separate thread contains Unity instructions");
				for (int j = 0; j < list.Count; j++)
				{
					Plugin.Error(list[j].ToLogName());
				}
				return;
			}
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length == 0)
		{
			Action action = BuildDelegate<Action>(method, target);
			if (action == null)
			{
				Plugin.Error("Cannot register update method " + method.ToLogName() + ": invalid delegate");
			}
			else
			{
				_updates.Add(new UpdateData(attributeValue.IsUnity, attributeValue.PauseWaiting, attributeValue.PauseRestarting, attributeValue.Delay, action));
			}
		}
		else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(UpdateData))
		{
			Action<UpdateData> action2 = BuildDelegate<Action<UpdateData>>(method, target);
			if (action2 == null)
			{
				Plugin.Error("Cannot register update method " + method.ToLogName() + ": invalid delegate");
			}
			else
			{
				_updates.Add(new UpdateData(attributeValue.IsUnity, attributeValue.PauseWaiting, attributeValue.PauseRestarting, attributeValue.Delay, action2));
			}
		}
		else
		{
			Plugin.Error("Cannot register update method " + method.ToLogName() + ": invalid overload parameters");
		}
	}

	private static IEnumerator<float> Handler()
	{
		while (true)
		{
			yield return float.NegativeInfinity;
			yield return float.NegativeInfinity;
			foreach (UpdateData update in _updates)
			{
				if (!update.IsUnity && update.CanRun())
				{
					_updatesQueue.Enqueue(update);
				}
				else if (update.IsUnity && (!update.PauseWaiting || RoundHelper.State != RoundState.WaitingForPlayers) && (!update.PauseRestarting || RoundHelper.State != RoundState.Restarting) && update.CanRun())
				{
					update.DoCall();
				}
			}
		}
	}

	private static TDelegate BuildDelegate<TDelegate>(MethodInfo method, object target) where TDelegate : Delegate
	{
		try
		{
			return method.CreateDelegate(typeof(TDelegate), target) as TDelegate;
		}
		catch (Exception message)
		{
			Plugin.Error("Failed to build delegate invoker for " + method.ToLogName());
			Plugin.Error(message);
		}
		return null;
	}
}
