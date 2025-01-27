namespace Compendium.Messages;

public class BroadcastMessage : MessageBase
{
	public bool IsAdminChat { get; set; }

	public bool IsTruncated { get; set; }

	public bool ClearDisplay { get; set; } = true;


	public override void Send(ReferenceHub hub)
	{
		if (ClearDisplay)
		{
			Broadcast.Singleton?.TargetClearElements(hub.connectionToClient);
		}
		if (IsAdminChat)
		{
			Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, base.Value, (ushort)base.Duration, Broadcast.BroadcastFlags.AdminChat);
		}
		else if (IsTruncated)
		{
			Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, base.Value, (ushort)base.Duration, Broadcast.BroadcastFlags.Truncated);
		}
		else
		{
			Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, base.Value, (ushort)base.Duration, Broadcast.BroadcastFlags.Normal);
		}
	}

	public static BroadcastMessage Create(string content, ushort duration, bool shouldClear = true, bool isTruncated = false, bool isAdminChat = false)
	{
		return new BroadcastMessage
		{
			Value = content,
			Duration = (int)duration,
			ClearDisplay = shouldClear,
			IsAdminChat = isAdminChat,
			IsTruncated = isTruncated
		};
	}
}
