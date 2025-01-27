using System;
using System.Net.Sockets;
using CentralAuth;
using Compendium.Extensions;
using helpers.Patching;
using LiteNetLib;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;

namespace Compendium;

public static class NetworkHelper
{
	public static event Action<NetPeer, ReferenceHub, DisconnectReason, SocketError> OnDisconnecting;

	public static bool SpawnForSelected(this GameObject networkObject, NetworkConnection ownerConnection, params ReferenceHub[] hubs)
	{
		try
		{
			if (!networkObject.TryGet<NetworkIdentity>(out var result))
			{
				Plugin.Warn("Attempted to spawn an object without a network identity.");
				return false;
			}
			if (NetworkServer.spawned.ContainsKey(result.netId))
			{
				Plugin.Warn($"Attempted to spawn a duplicate network ID: {result.netId}");
				return false;
			}
			result.connectionToClient = (NetworkConnectionToClient)ownerConnection;
			if (ownerConnection is LocalConnectionToClient)
			{
				result.isOwned = true;
			}
			if (!result.isServer && result.netId == 0)
			{
				result.isLocalPlayer = NetworkClient.localPlayer == result;
				result.isClient = NetworkClient.active;
				result.isServer = true;
				result.netId = NetworkIdentity.GetNextNetworkId();
				NetworkServer.spawned[result.netId] = result;
				result.OnStartServer();
			}
			for (int i = 0; i < hubs.Length; i++)
			{
				if (hubs[i].connectionToClient != null && hubs[i].connectionToClient.isReady && hubs[i].Mode == ClientInstanceMode.ReadyClient)
				{
					result.AddObserver(hubs[i].connectionToClient);
				}
			}
			return true;
		}
		catch (Exception message)
		{
			Plugin.Error(message);
			return false;
		}
	}

	public static void SendDestroyForAll(this uint netId)
	{
		ObjectDestroyMessage msg = default(ObjectDestroyMessage);
		msg.netId = netId;
		msg.SendToAll();
	}

	public static void SendSpawnForAll(this NetworkIdentity identity)
	{
		SpawnMessage msg = default(SpawnMessage);
		msg.assetId = identity.assetId;
		msg.isLocalPlayer = identity.isLocalPlayer;
		msg.isOwner = identity.isOwned;
		msg.netId = identity.netId;
		msg.position = identity.transform.position;
		msg.rotation = identity.transform.rotation;
		msg.scale = identity.transform.localScale;
		msg.sceneId = identity.sceneId;
		msg.SendToAll();
	}

	public static void SendSpawnForSelected(this NetworkIdentity identity, params ReferenceHub[] hubs)
	{
		for (int i = 0; i < hubs.Length; i++)
		{
			if (hubs[i].connectionToClient != null && hubs[i].connectionToClient.isReady && hubs[i].Mode == ClientInstanceMode.ReadyClient)
			{
				hubs[i].connectionToClient.Send(new SpawnMessage
				{
					assetId = identity.assetId,
					isLocalPlayer = identity.isLocalPlayer,
					isOwner = identity.isOwned,
					netId = identity.netId,
					position = identity.transform.position,
					rotation = identity.transform.rotation,
					scale = identity.transform.localScale,
					sceneId = identity.sceneId
				});
			}
		}
	}

	public static void SendToAll<T>(this T msg, int channel = 0) where T : struct, NetworkMessage
	{
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.connectionToClient != null && allHub.connectionToClient.isReady && allHub.Mode == ClientInstanceMode.ReadyClient)
			{
				allHub.connectionToClient.Send(msg, channel);
			}
		}
	}

	[Patch(typeof(LiteNetLib4MirrorServer), "OnPeerDisconnected", PatchType.Prefix, new Type[] { })]
	private static bool OnDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
	{
		try
		{
			ReferenceHub referenceHub = null;
			foreach (ReferenceHub hub in Hub.Hubs)
			{
				if (hub.connectionToClient == null || hub.connectionToClient.connectionId >= LiteNetLib4MirrorServer.Peers.Length)
				{
					continue;
				}
				try
				{
					NetPeer netPeer = LiteNetLib4MirrorServer.Peers[hub.connectionToClient.connectionId];
					if (netPeer != null && netPeer == peer)
					{
						referenceHub = hub;
						break;
					}
				}
				catch (Exception arg)
				{
					Plugin.Error($"OnDisconnected Hub Loop Exception: {arg}");
				}
			}
			if (referenceHub != null)
			{
				NetworkHelper.OnDisconnecting?.Invoke(peer, referenceHub, disconnectinfo.Reason, disconnectinfo.SocketErrorCode);
			}
		}
		catch (Exception arg2)
		{
			Plugin.Error($"OnDisconnected General Exception: {arg2}");
		}
		LiteNetLib4MirrorCore.LastDisconnectError = disconnectinfo.SocketErrorCode;
		LiteNetLib4MirrorCore.LastDisconnectReason = disconnectinfo.Reason;
		LiteNetLib4MirrorTransport.Singleton.OnServerDisconnected(peer.Id + 1);
		return false;
	}
}
