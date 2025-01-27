using System;
using helpers;
using helpers.Values;

namespace Compendium.Messages;

public class MessageBase : IValue<string>
{
	public string Value { get; set; }

	public double Duration { get; set; }

	public bool IsValid
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Value))
			{
				return Duration > 0.0;
			}
			return false;
		}
	}

	public virtual void Send(ReferenceHub hub)
	{
	}

	public void SendToTargets(params ReferenceHub[] targets)
	{
		targets.ForEach(Send);
	}

	public void SendToAll()
	{
		Hub.Hubs.ForEach(Send);
	}

	public void SendConditionally(Predicate<ReferenceHub> predicate)
	{
		Hub.ForEach(Send, predicate);
	}
}
