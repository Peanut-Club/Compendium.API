using System;
using helpers;
using PluginAPI.Enums;

namespace Compendium.Events;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class EventAttribute : Attribute
{
	public ServerEventType? Type { get; internal set; }

	public Priority Priority { get; set; } = Priority.Normal;


	public EventAttribute(ServerEventType type)
	{
		Type = type;
	}

	public EventAttribute(Priority priority)
	{
		Priority = priority;
	}

	public EventAttribute(ServerEventType type, Priority priority)
	{
		Type = type;
		Priority = priority;
	}

	public EventAttribute()
	{
	}
}
