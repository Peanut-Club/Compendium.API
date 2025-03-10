using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PluginAPI.Enums;

namespace Compendium.Settings;

public class EventSettings
{
	[Description("A list of events to record.")]
	public List<ServerEventType> RecordEvents { get; set; } = Enum.GetValues(typeof(ServerEventType)).Cast<ServerEventType>().ToList();


	[Description("Whether or not to log duration of every event execution listed in RecordEvents.")]
	public bool ShowEventDuration { get; set; } = true;


	[Description("Whether or not to show the total event duration.")]
	public bool ShowTotalExecution { get; set; } = true;


	[Description("Whether or not to log the round summary of event times.")]
	public bool ShowRoundSummary { get; set; } = true;


	[Description("Whether or not to use the old event invocation system via Reflection (much slower, but also much more stable).")]
	public bool UseStable { get; set; }
}
