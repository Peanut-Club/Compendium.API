using System.Linq;
using CentralAuth;
using Compendium.Messages;
using InventorySystem.Disarming;
using MapGeneration;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Core;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace Compendium;

public static class HubWorldExtensions
{
	public static void Broadcast(this ReferenceHub hub, object content, int time, bool clear = true)
	{
		if (clear)
		{
			global::Broadcast.Singleton?.TargetClearElements(hub.connectionToClient);
		}
		global::Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, content.ToString(), (ushort)time, global::Broadcast.BroadcastFlags.Normal);
	}

	public static void MessageBox(this ReferenceHub hub, object content)
	{
		hub.gameConsoleTransmission.SendToClient($"[REPORTING] {content}", "red");
	}

	public static void Hint(this ReferenceHub hub, object content, float duration = 5f)
	{
		MessageScheduler.Schedule(hub, HintMessage.Create(content?.ToString() ?? "empty", duration));
	}

	public static void Message(this ReferenceHub hub, object content, bool isRemoteAdmin = false)
	{
		if (hub.Mode != ClientInstanceMode.ReadyClient)
		{
			Log.Info(content.ToString(), isRemoteAdmin ? "REMOTE ADMIN" : "GAME CONSOLE");
		}
		else if (!isRemoteAdmin)
		{
			hub.gameConsoleTransmission.SendToClient(content.ToString(), "red");
		}
		else
		{
			hub.queryProcessor.SendToClient(content.ToString(), isSuccess: true, logInConsole: false, string.Empty);
		}
	}

	public static Vector3 Position(this ReferenceHub hub, Vector3? newPos = null, Quaternion? newRot = null)
	{
		if (newPos.HasValue)
		{
			if (newRot.HasValue)
			{
				hub.TryOverridePosition(newPos.Value, newRot.Value.eulerAngles);
			}
			else
			{
				hub.TryOverridePosition(newPos.Value, Vector3.zero);
			}
			return newPos.Value;
		}
		if (hub.Role() is IFpcRole fpcRole && fpcRole != null && fpcRole.FpcModule != null)
		{
			return fpcRole.FpcModule.Position;
		}
		if (hub.Role() is Scp079Role scp079Role && scp079Role.CurrentCamera != null && scp079Role.CurrentCamera.Room != null)
		{
			return scp079Role.CurrentCamera.Room.transform.position;
		}
		return hub.PlayerCameraReference.position;
	}

	public static RelativePosition RelativePosition(this ReferenceHub hub)
	{
		return new RelativePosition(hub.transform.position);
	}

	public static Quaternion Rotation(this ReferenceHub hub, Quaternion? newRot = null)
	{
		if (newRot.HasValue)
		{
			if (hub.Role() is IFpcRole fpcRole && fpcRole.FpcModule != null && fpcRole.FpcModule.MouseLook != null)
			{
				fpcRole.FpcModule.MouseLook.CurrentHorizontal = newRot.Value.y;
				fpcRole.FpcModule.MouseLook.CurrentVertical = newRot.Value.z;
				fpcRole.FpcModule.MouseLook.ApplySyncValues((ushort)newRot.Value.y, (ushort)newRot.Value.z);
			}
			return newRot.Value;
		}
		if (hub.Role() is IFpcRole fpcRole2 && fpcRole2.FpcModule != null && fpcRole2.FpcModule.MouseLook != null)
		{
			return new Quaternion(0f, fpcRole2.FpcModule.MouseLook.CurrentHorizontal, fpcRole2.FpcModule.MouseLook.CurrentVertical, 0f);
		}
		if (hub.Role() is Scp079Role scp079Role && scp079Role.CurrentCamera != null)
		{
			return new Quaternion(0f, scp079Role.CurrentCamera.HorizontalRotation, scp079Role.VerticalRotation, scp079Role.CurrentCamera.RollRotation);
		}
		return hub.PlayerCameraReference.rotation;
	}

	public static RoomIdentifier Room(this ReferenceHub hub)
	{
		return RoomIdUtils.RoomAtPosition(hub.Position());
	}

	public static FacilityZone Zone(this ReferenceHub hub)
	{
		RoomIdentifier roomIdentifier = hub.Room();
		if (roomIdentifier != null)
		{
			return roomIdentifier.Zone;
		}
		return FacilityZone.None;
	}

	public static RoomName RoomId(this ReferenceHub hub)
	{
		RoomIdentifier roomIdentifier = hub.Room();
		if (roomIdentifier != null)
		{
			return roomIdentifier.Name;
		}
		return MapGeneration.RoomName.Unnamed;
	}

	public static string RoomName(this ReferenceHub hub)
	{
		RoomIdentifier roomIdentifier = hub.Room();
		if (roomIdentifier != null)
		{
			return roomIdentifier.name;
		}
		return "unknown room";
	}

	public static bool IsHandcuffed(this ReferenceHub hub)
	{
		return hub.inventory.IsDisarmed();
	}

	public static bool HasHandcuffed(this ReferenceHub hub)
	{
		return hub.GetCuffed() != null;
	}

	public static void Handcuff(this ReferenceHub hub, ReferenceHub cuffer = null)
	{
		if (cuffer != null)
		{
			hub.inventory.SetDisarmedStatus(cuffer.inventory);
			return;
		}
		hub.inventory.SetDisarmedStatus(null);
		DisarmedPlayers.Entries.Add(new DisarmedPlayers.DisarmedEntry(hub.netId, 0u));
		new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated();
	}

	public static void Uncuff(this ReferenceHub hub)
	{
		hub.inventory.SetDisarmedStatus(null);
	}

	public static ReferenceHub GetCuffer(this ReferenceHub hub)
	{
		foreach (DisarmedPlayers.DisarmedEntry entry in DisarmedPlayers.Entries)
		{
			if (entry.DisarmedPlayer == hub.netId)
			{
				if (entry.Disarmer == 0)
				{
					return ReferenceHub.HostHub;
				}
				return Hub.GetHub(entry.Disarmer);
			}
		}
		return null;
	}

	public static ReferenceHub GetCuffed(this ReferenceHub hub)
	{
		return Hub.GetHub(DisarmedPlayers.Entries.FirstOrDefault((DisarmedPlayers.DisarmedEntry x) => x.Disarmer == hub.netId).DisarmedPlayer);
	}
}
