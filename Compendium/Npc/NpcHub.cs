using System.Collections.Generic;
using System.Linq;
using BetterCommands;
using CentralAuth;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Extensions;
using helpers;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace Compendium.Npc;

public class NpcHub
{
	public static readonly HashSet<NpcHub> List;

	private CoroutineHandle updateHandle;

	public ReferenceHub Target { get; set; }

	public ReferenceHub Hub { get; private set; }

	public GameObject GameObject { get; private set; }

	public NetworkConnectionToClient Connection { get; private set; }

	public int CustomId { get; }

	public string Name
	{
		get
		{
			return Hub.nicknameSync.Network_myNickSync;
		}
		set
		{
			Hub.nicknameSync.Network_myNickSync = value;
		}
	}

	public bool IsAttacking { get; set; }

	public bool IsSpawned
	{
		get
		{
			return Role != RoleTypeId.Spectator;
		}
		set
		{
			if (IsSpawned != value && !value)
			{
				Role = RoleTypeId.None;
			}
		}
	}

	public bool IsGodMode
	{
		get
		{
			return Hub.characterClassManager.GodMode;
		}
		set
		{
			Hub.characterClassManager.GodMode = value;
		}
	}

	public float Health
	{
		get
		{
			return Hub.playerStats.GetModule<HealthStat>().CurValue;
		}
		set
		{
			Hub.playerStats.GetModule<HealthStat>().CurValue = value;
		}
	}

	public float MaxHealth => Hub.playerStats.GetModule<HealthStat>().MaxValue;

	public RoleTypeId Role
	{
		get
		{
			return Hub.roleManager.CurrentRole.RoleTypeId;
		}
		set
		{
			Hub.roleManager.ServerSetRole(value, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
		}
	}

	public Vector3 Position
	{
		get
		{
			if (!(Hub.roleManager.CurrentRole is IFpcRole fpcRole))
			{
				return Vector3.zero;
			}
			return fpcRole.FpcModule.Position;
		}
		set
		{
			(Hub.roleManager.CurrentRole as IFpcRole).FpcModule.ServerOverridePosition(value, Vector3.zero);
		}
	}

	public Quaternion Rotation
	{
		get
		{
			return Hub.PlayerCameraReference.rotation;
		}
		set
		{
			if (Hub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				(ushort, ushort) tuple = value.ToClientUShorts();
				fpcRole.FpcModule.MouseLook.ApplySyncValues(tuple.Item1, tuple.Item2);
			}
		}
	}

	static NpcHub()
	{
		List = new HashSet<NpcHub>();
		PlayerAuthenticationManager.OnInstanceModeChanged += delegate(ReferenceHub hub, ClientInstanceMode mode)
		{
			if (IsNpc(hub))
			{
				hub.authManager._targetInstanceMode = ClientInstanceMode.Host;
			}
		};
		PlayerAuthenticationManager.OnSyncedUserIdAssigned += delegate(ReferenceHub hub)
		{
			if (IsNpc(hub))
			{
				hub.authManager.SyncedUserId = "ID_Dedicated";
			}
		};
	}

	public NpcHub(string name = null)
	{
		GameObject playerPrefab = NetworkManager.singleton.playerPrefab;
		GameObject gameObject = Object.Instantiate(playerPrefab);
		ReferenceHub component = gameObject.GetComponent<ReferenceHub>();
		GameObject = gameObject;
		Hub = component;
		List.Add(this);
		try
		{
			component.roleManager.InitializeNewRole(RoleTypeId.None, RoleChangeReason.None);
		}
		catch
		{
		}
		if (!CollectionExtensions.TryDequeue(RecyclablePlayerId.FreeIds, out var element))
		{
			element = ++RecyclablePlayerId._autoIncrement;
		}
		RecyclablePlayerId recyclablePlayerId = new RecyclablePlayerId(element);
		CustomId = recyclablePlayerId.Value;
		name = (string.IsNullOrWhiteSpace(name) ? $"Npc ({recyclablePlayerId.Value})" : name);
		Connection = new NpcConnection(recyclablePlayerId.Value);
		NetworkServer.AddPlayerForConnection(Connection, gameObject);
		try
		{
			component.authManager.SyncedUserId = null;
		}
		catch
		{
		}
		Name = name;
		updateHandle = Timing.RunCoroutine(Update());
	}

	public void LookAt(Vector3 position)
	{
		Rotation = Quaternion.LookRotation(position - Position, Vector3.up);
	}

	public void Despawn()
	{
		IsSpawned = false;
	}

	public void Spawn(RoleTypeId role, Vector3 pos)
	{
		Role = role;
		Position = pos;
	}

	public void Destroy()
	{
		Plugin.Info($"Destroying NPC: {CustomId}");
		try
		{
			Despawn();
		}
		catch
		{
		}
		Timing.CallDelayed(0.3f, delegate
		{
			try
			{
				Hub._playerId.Destroy();
			}
			catch
			{
			}
			try
			{
				NetworkServer.Destroy(GameObject);
			}
			catch
			{
			}
			Hub = null;
			Connection = null;
			GameObject = null;
			List.Remove(this);
		});
	}

	public void Pool()
	{
		Timing.KillCoroutines(updateHandle);
		Target = null;
		IsAttacking = false;
		Despawn();
		List.Remove(this);
	}

	public void UnPool()
	{
		Hub.SetSize(Vector3.one);
		updateHandle = Timing.RunCoroutine(Update());
		List.Add(this);
	}

	public IEnumerator<float> Update()
	{
		IFpcRole fpcRole = default(IFpcRole);
		IFpcRole fpcRole2 = default(IFpcRole);
		while (true)
		{
			yield return float.NegativeInfinity;
			yield return float.NegativeInfinity;
			try
			{
				int num2;
				if (IsSpawned && (object)Target != null)
				{
					PlayerRoleBase currentRole = Target.roleManager.CurrentRole;
					fpcRole = currentRole as IFpcRole;
					if (fpcRole != null && (object)fpcRole.FpcModule != null && fpcRole.FpcModule.ModuleReady && !((Object)(object)Hub.roleManager.CurrentRole == null))
					{
						currentRole = Hub.roleManager.CurrentRole;
						fpcRole2 = currentRole as IFpcRole;
						if (fpcRole2 != null && (object)fpcRole2.FpcModule != null)
						{
							num2 = ((!fpcRole2.FpcModule.ModuleReady) ? 1 : 0);
							goto IL_013e;
						}
					}
				}
				num2 = 1;
				goto IL_013e;
				IL_013e:
				if (num2 != 0)
				{
					continue;
				}
				Vector3 position = fpcRole.FpcModule.Position;
				Vector3 position2 = position;
				float num = Vector3.Distance(position, Position);
				position2.y = Position.y;
				LookAt(position2);
				if (fpcRole.FpcModule.CurrentMovementState == PlayerMovementState.Sneaking)
				{
					fpcRole2.FpcModule.CurrentMovementState = PlayerMovementState.Sneaking;
				}
				else
				{
					fpcRole2.FpcModule.CurrentMovementState = PlayerMovementState.Sprinting;
				}
				if (fpcRole2.FpcModule.CurrentMovementState == PlayerMovementState.Sneaking)
				{
					if (num >= 5f)
					{
						Position = position;
					}
					else if (num >= 1f)
					{
						Position += Hub.PlayerCameraReference.forward / 20f * fpcRole.FpcModule.SprintSpeed;
					}
				}
				else if (num >= 10f)
				{
					Position = position;
				}
				else if (num >= 2f)
				{
					Position += Hub.PlayerCameraReference.forward / 10f * fpcRole.FpcModule.SprintSpeed;
				}
				fpcRole = null;
				fpcRole2 = null;
			}
			catch
			{
			}
		}
	}

	public static NpcHub Spawn(string name, RoleTypeId role, Vector3 pos)
	{
		NpcPool pool = NpcPool.Pool;
		NpcHub npc = null;
		if (!pool.Queue.TryDequeue(out npc))
		{
			npc = SpawnNew(name, role, pos);
		}
		else
		{
			npc.Name = name;
			npc.Role = role;
			npc.Hub.ClearItems();
			Timing.CallDelayed(0.3f, delegate
			{
				npc.Position = pos;
			});
		}
		return npc;
	}

	public static NpcHub SpawnNew(string name, RoleTypeId role, Vector3 pos)
	{
		NpcHub npc = new NpcHub(name);
		Timing.CallDelayed(0.3f, delegate
		{
			npc.Role = role;
			npc.Hub.ClearItems();
			Timing.CallDelayed(0.3f, delegate
			{
				npc.Position = pos;
			});
		});
		return npc;
	}

	public static bool IsNpc(ReferenceHub hub)
	{
		return List.Any((NpcHub npc) => npc.Hub != null && npc.Hub.netId == hub.netId);
	}

	public static bool TryGetNpc(int id, out NpcHub npc)
	{
		return List.TryGetFirst((NpcHub npc) => npc.Hub != null && npc.CustomId == id, out npc);
	}

	public static bool TryGetNpc(ReferenceHub hub, out NpcHub npc)
	{
		return List.TryGetFirst((NpcHub npc) => npc.Hub != null && npc.Hub.netId == hub.netId, out npc);
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnStart()
	{
		try
		{
			for (int i = 0; i < Plugin.Config.FeatureSettings.NpcPreload; i++)
			{
				NpcHub npc = SpawnNew("NPC", RoleTypeId.None, Vector3.zero);
				Timing.CallDelayed(5f, delegate
				{
					NpcPool.Pool.Push(npc);
				});
			}
		}
		catch
		{
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		try
		{
			foreach (NpcHub item in List)
			{
				try
				{
					item.Destroy();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		Mirror.Extensions.Clear(NpcPool.Pool.Queue);
		List.Clear();
	}

	[BetterCommands.Command("spawnnpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "snpc" })]
	[Description("Spawns an NPC.")]
	private static string SpawnNpcCommand(ReferenceHub sender)
	{
		NpcHub npcHub = Spawn(sender.Nick() + "'s NPC", sender.RoleId(), sender.Position());
		return $"Spawned NPC: {npcHub.CustomId}";
	}

	[BetterCommands.Command("despawnnpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "dnpc" })]
	[Description("Despawns an NPC.")]
	private static string DespawnNpcCommand(ReferenceHub sender, int npcId)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		NpcPool.Pool.Push(npc);
		return "NPC returned to the pool.";
	}

	[BetterCommands.Command("destroynpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "denpc" })]
	[Description("Completely destroys an NPC.")]
	private static string DestroyNpcCommand(ReferenceHub sender, int npcId)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		npc.Destroy();
		return "NPC destroyed.";
	}

	[BetterCommands.Command("follownpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "fnpc" })]
	[Description("Makes an NPC follow another player.")]
	private static string FollowCommand(ReferenceHub sender, int npcId, ReferenceHub target)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		if (npc.Target != null && npc.Target == target)
		{
			npc.Target = null;
			return "Stopped targeting " + target.Nick();
		}
		npc.Target = target;
		return "Started targeting " + target.Nick();
	}

	[BetterCommands.Command("scalenpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "scnpc" })]
	[Description("Scales an NPC.")]
	private static string ScaleCommand(ReferenceHub sender, int npcId, float scale)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		npc.Hub.SetScale(scale);
		return "NPC scaled.";
	}

	[BetterCommands.Command("sizenpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "sinpc" })]
	[Description("Changes an NPC's model size.")]
	private static string SizeCommand(ReferenceHub sender, int npcId, Vector3 size)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		npc.Hub.SetSize(size);
		return "NPC scaled.";
	}

	[BetterCommands.Command("itemnpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "itnpc" })]
	[Description("Sets the currently held item of an NPC.")]
	private static string SetItemCommand(ReferenceHub sender, int npcId, ItemType item)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		npc.Hub.SetCurrentItemId(item);
		return $"NPC {npc.CustomId} is now holding {item}";
	}

	[BetterCommands.Command("rolenpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "ronpc" })]
	[Description("Sets the role of a NPC.")]
	private static string RoleCommand(ReferenceHub sender, int npcId, RoleTypeId role)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		npc.Role = role;
		return $"Role of '{npc.CustomId}' is now {role}";
	}

	[BetterCommands.Command("godnpc", new CommandType[] { CommandType.RemoteAdmin })]
	[CommandAliases(new object[] { "gnpc" })]
	[Description("Changes god-mode status of a NPC.")]
	private static string GodModeCommand(ReferenceHub sender, int npcId)
	{
		if (!TryGetNpc(npcId, out var npc))
		{
			return "No such NPCs were found.";
		}
		npc.Hub.characterClassManager.GodMode = !npc.Hub.characterClassManager.GodMode;
		return $"God-Mode status of NPC {npc.CustomId} is now {npc.Hub.characterClassManager.GodMode}";
	}

	[BetterCommands.Command("listnpc", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Shows a list of active NPCs")]
	private static string ListNpcCommand(ReferenceHub sender)
	{
		string text = $"Active NPCs ({List.Count}):\n";
		foreach (NpcHub item in List)
		{
			text += string.Format("[{0}]: {1} [{2}]\n", item.CustomId, item.Name, item.IsSpawned ? "SPAWNED" : "DESPAWNED");
		}
		return text;
	}
}
