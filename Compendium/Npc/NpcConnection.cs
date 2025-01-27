using System;
using Mirror;

namespace Compendium.Npc;

public class NpcConnection : NetworkConnectionToClient
{
	public override string address => "localhost";

	public NpcConnection(int networkConnectionId)
		: base(networkConnectionId)
	{
	}

	public override void Send(ArraySegment<byte> segment, int channelId = 0)
	{
	}
}
