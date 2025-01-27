using System;
using System.Collections.Generic;
using System.Threading;
using helpers;
using helpers.Attributes;

namespace Compendium.Messages;

public static class MessageScheduler
{
	private static readonly List<MessageSchedulerData> _messageList = new List<MessageSchedulerData>();

	private static readonly object _lock = new object();

	[Load]
	private static void Load()
	{
		new Timer(OnTick, null, 0, 200);
	}

	public static void Schedule(ReferenceHub target, MessageBase message, int? msDelay = null)
	{
		lock (_lock)
		{
			if (msDelay.HasValue)
			{
				_messageList.Add(new MessageSchedulerData(message, target, DateTime.Now + TimeSpan.FromMilliseconds(msDelay.Value)));
			}
			else
			{
				_messageList.Add(new MessageSchedulerData(message, target, null));
			}
		}
	}

	private static void OnTick(object _)
	{
		lock (_lock)
		{
			if (_messageList.Count <= 0)
			{
				return;
			}
			List<MessageSchedulerData> list = Pools.PoolList<MessageSchedulerData>();
			for (int i = 0; i < _messageList.Count; i++)
			{
				MessageSchedulerData item = _messageList[i];
				if (!item.At.HasValue || !(DateTime.Now < item.At.Value))
				{
					item.Message.Send(item.Target);
					list.Add(item);
				}
			}
			list.For(delegate(int _, MessageSchedulerData data)
			{
				_messageList.Remove(data);
			});
			list.ReturnList();
		}
	}
}
