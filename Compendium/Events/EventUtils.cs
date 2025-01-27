using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compendium.Value;
using helpers;
using helpers.Dynamic;
using helpers.Extensions;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace Compendium.Events;

public static class EventUtils
{
	public static void TryInvoke(EventRegistryData data, IEventArguments args, ValueReference isAllowed, out bool result)
	{
		Delegate target = data.Target;
		try
		{
			if (target is Action action)
			{
				action();
				result = true;
				return;
			}
			if (target is Func<bool> func)
			{
				result = func();
				return;
			}
			if (target is DynamicMethodDelegate dynamicMethodDelegate)
			{
				data.PrepareBuffer(args, isAllowed);
				if (Plugin.Config.ApiSetttings.EventSettings.UseStable)
				{
					object obj = data.Target.Method.Invoke(data.Handle, data.Buffer);
					if (obj != null && obj is bool flag)
					{
						result = flag;
					}
					else
					{
						result = true;
					}
				}
				else
				{
					object obj2 = dynamicMethodDelegate(data.Handle, data.Buffer);
					if (obj2 != null && obj2 is bool flag2)
					{
						result = flag2;
					}
					else
					{
						result = true;
					}
				}
				return;
			}
			Plugin.Warn("Failed to invoke delegate '" + target.GetType().FullName + "' (" + data.Target.Method.ToLogName() + ") - unknown delegate type");
		}
		catch (Exception message)
		{
			Plugin.Error($"Failed to invoke delegate '{data.Target.Method.ToLogName()}' while executing event '{args.BaseType}'");
			Plugin.Error(message);
		}
		result = true;
	}

	public static bool TryCreateEventData(MethodInfo target, bool skipAttributeCheck, object handle, out EventRegistryData registryData)
	{
		if ((object)target == null)
		{
			registryData = null;
			return false;
		}
		EventAttribute attributeValue = null;
		if (!skipAttributeCheck && !target.TryGetAttribute<EventAttribute>(out attributeValue))
		{
			registryData = null;
			return false;
		}
		if (!TryValidateInstance(target, ref handle))
		{
			registryData = null;
			return false;
		}
		ParameterInfo[] parameters = target.GetParameters();
		ServerEventType serverEventType = ((attributeValue != null && attributeValue.Type.HasValue) ? attributeValue.Type.Value : ServerEventType.None);
		if (serverEventType == ServerEventType.None && !TryRecognizeEventType(target, parameters, out serverEventType))
		{
			registryData = null;
			return false;
		}
		if (!TryGenerateDelegate(target, handle, out var del))
		{
			Plugin.Error("Failed to generate calling delegate for method '" + target.ToLogName() + "'");
			registryData = null;
			return false;
		}
		object[] buffer = null;
		if (parameters.Length != 0)
		{
			buffer = new object[parameters.Length];
		}
		Type[] args = parameters.Select((ParameterInfo x) => x.ParameterType).ToArray();
		registryData = new EventRegistryData(del, attributeValue?.Priority ?? Priority.Normal, serverEventType, handle, buffer, args);
		return true;
	}

	public static bool TryGenerateDelegate(MethodInfo method, object handle, out Delegate del)
	{
		try
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 0)
			{
				if (method.ReturnType == typeof(void))
				{
					del = method.CreateDelegate(typeof(Action), handle);
					return true;
				}
				if (method.ReturnType == typeof(bool))
				{
					del = method.CreateDelegate(typeof(Func<bool>), handle);
					return true;
				}
				Plugin.Warn("Cannot create invocation delegate for event handler '" + method.ToLogName() + "': unsupported return type (" + method.ReturnType.FullName + ")");
				del = null;
				return false;
			}
			if (!Reflection.HasInterface<IEventArguments>(parameters[0].ParameterType))
			{
				Plugin.Warn("Event handler '" + method.ToLogName() + "' has invalid event argument at index '0' (expected a class deriving from IEventArguments, actual class is '" + parameters[0].ParameterType.FullName + "')");
				del = null;
				return false;
			}
			if (parameters.Length == 2 && parameters[1].ParameterType != typeof(ValueReference))
			{
				Plugin.Warn("Event handler '" + method.ToLogName() + "' has invalid event argument at index '1' (expected a ValueReference, actual class is '" + parameters[1].ParameterType.FullName + "')");
				del = null;
				return false;
			}
			if (parameters.Length > 2)
			{
				Plugin.Warn("Event handler '" + method.ToLogName() + "' has too many arguments!");
				del = null;
				return false;
			}
			del = method.GetOrCreateInvoker();
			return true;
		}
		catch (Exception message)
		{
			Plugin.Error("Failed to generate delegate for " + method.ToLogName());
			Plugin.Error(message);
			del = null;
			return false;
		}
	}

	public static bool TryRecognizeEventType(MethodInfo method, ParameterInfo[] parameters, out ServerEventType serverEventType)
	{
		if (!parameters.Any())
		{
			Plugin.Warn("Failed to recognize event type of event handler '" + method.ToLogName() + "': no recognizable event parameters");
			serverEventType = ServerEventType.PlayerJoined;
			return false;
		}
		Type evParameterType = null;
		foreach (ParameterInfo parameterInfo in parameters)
		{
			if (Reflection.HasInterface<IEventArguments>(parameterInfo.ParameterType))
			{
				evParameterType = parameterInfo.ParameterType;
				break;
			}
		}
		if ((object)evParameterType == null)
		{
			Plugin.Warn("Failed to recognize event type of event handler '" + method.ToLogName() + "': no recognizable event parameters");
			serverEventType = ServerEventType.PlayerJoined;
			return false;
		}
		if (!EventManager.Events.TryGetFirst((KeyValuePair<ServerEventType, Event> ev) => ev.Value.EventArgType == evParameterType, out var value) || value.Value == null)
		{
			Plugin.Warn("Failed to recognize event type of event handler '" + method.ToLogName() + "': unknown event type");
			serverEventType = ServerEventType.PlayerJoined;
			return false;
		}
		serverEventType = value.Key;
		return true;
	}

	public static bool TryValidateInstance(MethodInfo method, ref object instance)
	{
		if (instance == null && !method.IsStatic)
		{
			if (Singleton.HasInstance(method.DeclaringType))
			{
				instance = Singleton.Instance(method.DeclaringType);
				return true;
			}
			Plugin.Warn("Failed to register event handler '" + method.ToLogName() + "': missing class instance");
			return false;
		}
		if (instance != null && instance.GetType() != method.DeclaringType)
		{
			Plugin.Warn("Failed to register event handler '" + method.ToLogName() + "': invalid class handle (method's class: " + method.DeclaringType.FullName + ", class handle type: " + instance.GetType().FullName + ")");
			return false;
		}
		return true;
	}
}
