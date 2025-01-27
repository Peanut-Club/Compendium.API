using System;
using Hints;

namespace Compendium.Messages;

public class HintMessage : MessageBase
{
	public static event Action<HintMessage, ReferenceHub> HintProxies;

	public override void Send(ReferenceHub hub)
	{
		if (HintMessage.HintProxies != null)
		{
			HintMessage.HintProxies(this, hub);
			return;
		}
		StringHintParameter stringHintParameter = new StringHintParameter(base.Value);
		HintParameter[] parameters = new HintParameter[1] { stringHintParameter };
		TextHint hint = new TextHint(base.Value, parameters, null, (float)base.Duration);
		hub.hints.Show(hint);
	}

	public static HintMessage Create(string content, double duration)
	{
		return new HintMessage
		{
			Duration = duration,
			Value = content
		};
	}
}
