using System;
using System.Collections.Generic;
using System.Text;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Sounds;
using Compendium.Voice;
using Compendium.Voice.Profiles.Pitch;
using helpers.Pooling.Pools;
using InventorySystem.Items.Firearms;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.Voice;
using RelativePositioning;
using Respawning;
using RoundRestarting;
using UnityEngine;

namespace Compendium;

public static class HubNetworkExtensions
{
	public enum SoundId
	{
		Beep,
		/* disabled
		GunShot,*/
		Lever
	}

	private static readonly Dictionary<ReferenceHub, Vector3> _fakePositions = new Dictionary<ReferenceHub, Vector3>();

	private static readonly Dictionary<ReferenceHub, Dictionary<ReferenceHub, Vector3>> _fakePositionsMatrix = new Dictionary<ReferenceHub, Dictionary<ReferenceHub, Vector3>>();

	private static readonly Dictionary<ReferenceHub, Dictionary<Type, byte>> _fakeIntensity = new Dictionary<ReferenceHub, Dictionary<Type, byte>>();

	private static readonly Dictionary<ReferenceHub, RemoteAdminIconType> _raIcons = new Dictionary<ReferenceHub, RemoteAdminIconType>();

	private static readonly Dictionary<uint, List<uint>> _invisMatrix = new Dictionary<uint, List<uint>>();

	private static readonly HashSet<uint> _invisList = new HashSet<uint>();

	public static bool IsInvisibleTo(this ReferenceHub player, ReferenceHub target)
	{
		if (_invisList.Contains(player.netId))
		{
			return true;
		}
		if (_invisMatrix.TryGetValue(player.netId, out var value))
		{
			return value.Contains(target.netId);
		}
		return false;
	}

	public static bool IsInvisible(this ReferenceHub player)
	{
		return _invisList.Contains(player.netId);
	}

	public static void MakeInvisible(this ReferenceHub player)
	{
		_invisList.Add(player.netId);
	}

	public static void MakeVisible(this ReferenceHub player)
	{
		_invisList.Remove(player.netId);
	}

	public static void MakeInvisibleTo(this ReferenceHub player, ReferenceHub target)
	{
		if (_invisMatrix.TryGetValue(player.netId, out var value))
		{
			value.Add(target.netId);
			return;
		}
		_invisMatrix[player.netId] = new List<uint> { target.netId };
	}

	public static void MakeVisibleTo(this ReferenceHub player, ReferenceHub target)
	{
		if (_invisMatrix.TryGetValue(player.netId, out var value))
		{
			value.Remove(target.netId);
		}
	}

	public static bool TryGetFakePosition(this ReferenceHub hub, ReferenceHub target, out Vector3 position)
	{
		if (_fakePositions.TryGetValue(hub, out position))
		{
			return true;
		}
		if (target != null && _fakePositionsMatrix.TryGetValue(hub, out var value))
		{
			return value.TryGetValue(target, out position);
		}
		return false;
	}

	public static bool TryGetFakeIntensity(this ReferenceHub hub, Type type, out byte intensity)
	{
		if (_fakeIntensity.TryGetValue(hub, out var value))
		{
			return value.TryGetValue(type, out intensity);
		}
		intensity = 0;
		return false;
	}

	public static bool TryGetRaIcon(this ReferenceHub hub, out RemoteAdminIconType icon)
	{
		return _raIcons.TryGetValue(hub, out icon);
	}

	public static void FakeIntensity(this ReferenceHub hub, Type type, byte intensity)
	{
		if (!_fakeIntensity.ContainsKey(hub))
		{
			_fakeIntensity[hub] = new Dictionary<Type, byte>();
		}
		_fakeIntensity[hub][type] = intensity;
	}

	public static void FakePosition(this ReferenceHub hub, Vector3 position)
	{
		_fakePositions[hub] = position;
	}

	public static void SetRaIcon(this ReferenceHub hub, RemoteAdminIconType icon)
	{
		_raIcons[hub] = icon;
	}

	public static void FakePositionTo(this ReferenceHub hub, Vector3 position, params ReferenceHub[] targets)
	{
		if (!_fakePositionsMatrix.ContainsKey(hub))
		{
			_fakePositionsMatrix[hub] = new Dictionary<ReferenceHub, Vector3>();
		}
		targets.ForEach(delegate(ReferenceHub target)
		{
			_fakePositionsMatrix[hub][target] = position;
		});
	}

	public static void RemoveRaIcon(this ReferenceHub hub)
	{
		_raIcons.Remove(hub);
	}

	public static void RemoveFakePosition(this ReferenceHub hub)
	{
		_fakePositions.Remove(hub);
	}

	public static void RemoveAllFakePositions(this ReferenceHub hub)
	{
		_fakePositions.Remove(hub);
		_fakePositionsMatrix.Remove(hub);
	}

	public static void RemoveTargetFakePosition(this ReferenceHub hub, params ReferenceHub[] targets)
	{
		if (_fakePositionsMatrix.TryGetValue(hub, out var matrix))
		{
			targets.ForEach(delegate(ReferenceHub target)
			{
				matrix.Remove(target);
			});
		}
	}

	public static void RemoveFakeIntensity(this ReferenceHub hub, Type type)
	{
		if (!_fakeIntensity.ContainsKey(hub))
		{
			_fakeIntensity[hub].Remove(type);
		}
	}

	public static void PlaySound(this ReferenceHub hub, SoundId soundId, params object[] args)
	{

        switch (soundId)
		{
		case SoundId.Beep:
			hub.SendFakeTargetRpc(null, typeof(AmbientSoundPlayer), "RpcPlaySound", 7);
			break;
				/* disabled
		case SoundId.GunShot:
			hub.connectionToClient.Send(new GunAudioMessage
			{
				Weapon = (ItemType)args[0],
				MaxDistance = (byte)args[1],
				AudioClipId = (byte)args[2],
				ShooterHub = hub,
				ShooterPosition = new RelativePosition((Vector3)args[3])
			});
			break;
				*/
		case SoundId.Lever:
			hub.SendFakeTargetRpc(hub.networkIdentity, typeof(PlayerInteract), "RpcLeverSound");
			break;
		}
	}

	public static void PlayBeepSound(this ReferenceHub hub)
	{
		hub.PlaySound(SoundId.Beep);
	}

	/* disabled
	public static void PlayGunSound(this ReferenceHub hub, ItemType weaponType, byte volume, byte id, Vector3 position)
	{
		hub.PlaySound(SoundId.GunShot, weaponType, volume, id, position);
	} */

	public static void PlayCassie(this ReferenceHub hub, string announcement, bool isHold = false, bool isNoisy = false, bool isSubtitles = false)
	{
		RespawnEffectsController.AllControllers.ForEach(delegate(RespawnEffectsController ctrl)
		{
			if ((object)ctrl != null)
			{
				hub.SendFakeTargetRpc(ctrl.netIdentity, typeof(RespawnEffectsController), "RpcCassieAnnouncement", announcement, isHold, isNoisy, isSubtitles);
			}
		});
	}

	public static void PlayCassie(this ReferenceHub hub, string words, string translation, bool isHold = false, bool isNoisy = true, bool isSubtitles = true)
	{
		StringBuilder announcement = StringBuilderPool.Pool.Get();
		string[] array = words.Split(new char[1] { '\n' });
		string[] array2 = translation.Split(new char[1] { '\n' });
		for (int i = 0; i < array.Length; i++)
		{
			announcement.Append(array2[i] + "<size=0> " + array[i].Replace(' ', '\u2005') + " </size><split>");
		}
		string text = StringBuilderPool.Pool.PushReturn(announcement);
		RespawnEffectsController.AllControllers.ForEach(delegate(RespawnEffectsController ctrl)
		{
			if ((object)ctrl != null)
			{
				hub.SendFakeTargetRpc(ctrl.netIdentity, typeof(RespawnEffectsController), "RpcCassieAnnouncement", announcement, isHold, isNoisy, isSubtitles);
			}
		});
	}

	public static void SetTargetInfo(this ReferenceHub hub, string info, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			hub.SendFakeSyncVar(target.networkIdentity, typeof(NicknameSync), "Network_customPlayerInfoString", info);
		});
	}

	public static void SetTargetRoomColor(this RoomLightController light, Color color, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeSyncVar(light.netIdentity, typeof(RoomLightController), "NetworkOverrideColor", color);
			target.SendFakeSyncVar(light.netIdentity, typeof(RoomLightController), "NetworkLightsEnabled", true);
		});
	}

	public static void SetTargetNickname(this ReferenceHub hub, string nick, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeSyncVar(hub.networkIdentity, typeof(NicknameSync), "Network_displayName", nick);
		});
	}

	public static void SetTargetRole(this ReferenceHub hub, RoleTypeId role, byte unitId = 0, params ReferenceHub[] targets)
	{
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out var result))
		{
			return;
		}
		bool isRisky = role.GetTeam() == Team.Dead || !hub.IsAlive();
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteUShort(38952);
		networkWriterPooled.WriteUInt(hub.netId);
		networkWriterPooled.WriteRoleType(role);
		if (result is HumanRole humanRole && humanRole.UsesUnitNames)
		{
			if (!(hub.Role() is HumanRole))
			{
				isRisky = true;
			}
			networkWriterPooled.WriteByte(unitId);
		}
		FpcStandardRoleBase fpcStandardRoleBase = result as FpcStandardRoleBase;
		if ((UnityEngine.Object)(object)fpcStandardRoleBase != null)
		{
			if (!(hub.Role() is FpcStandardRoleBase fpcStandardRoleBase2))
			{
				isRisky = true;
			}
			else
			{
				fpcStandardRoleBase = fpcStandardRoleBase2;
			}
			fpcStandardRoleBase.FpcModule.MouseLook.GetSyncValues(0, out var syncH, out var _);
			networkWriterPooled.WriteRelativePosition(hub.RelativePosition());
			networkWriterPooled.WriteUShort(syncH);
		}
		if (result is ZombieRole)
		{
			if (!(hub.Role() is ZombieRole))
			{
				isRisky = true;
			}
			networkWriterPooled.WriteUShort((ushort)Mathf.Clamp(Mathf.CeilToInt(hub.MaxHealth()), 0, 65535));
		}
		ArraySegment<byte> arraySegment = networkWriterPooled.ToArraySegment();
		targets.ForEach(delegate(ReferenceHub target)
		{
			if (target != hub || !isRisky)
			{
				target.connectionToClient.Send(arraySegment);
			}
			else
			{
				Plugin.Warn($"Blocked a possible self-desync attempt of '{hub.Nick()}' with role '{role}'");
			}
		});
		NetworkWriterPool.Return(networkWriterPooled);
		hub.Position(hub.Position() + Vector3.up * 0.25f);
	}

	public static void SetTargetWarheadLevel(this ReferenceHub hub, bool isEnabled = true)
	{
		hub.SendFakeSyncVar(AlphaWarheadOutsitePanel.nukeside.netIdentity, typeof(AlphaWarheadNukesitePanel), "Networkenabled", isEnabled);
	}

	public static void SetTargetWarheadKeycard(this ReferenceHub hub, bool isEntered = true)
	{
		hub.SendFakeSyncVar(GameObject.Find("OutsitePanelScript").GetComponentInParent<AlphaWarheadOutsitePanel>().netIdentity, typeof(AlphaWarheadOutsitePanel), "NetworkkeycardEntered", isEntered);
	}

	public static void SetAspectRatio(this ReferenceHub hub, float ratio = 1f)
	{
		hub.aspectRatioSync.CmdSetAspectRatio(ratio);
	}

	/* disabled
	public static void SetTargetWindowStatus(this BreakableWindow window, BreakableWindow.BreakableWindowStatus status, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeSyncVar(window.netIdentity, typeof(BreakableWindow), "NetworksyncStatus", status);
		});
	}*/

	public static void SetTargetWarheadStatus(this ReferenceHub hub, AlphaWarheadSyncInfo status)
	{
		hub.SendFakeSyncVar(AlphaWarheadController.Singleton.netIdentity, typeof(AlphaWarheadController), "NetworkInfo", status);
	}

	public static void SetTargetServerName(this ReferenceHub hub, string name)
	{
		hub.SendFakeSyncVar(null, typeof(ServerConfigSynchronizer), "NetworkServerName", name);
	}

	public static void SetTargetGlobalBadge(this ReferenceHub hub, string text, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeSyncVar(hub.networkIdentity, typeof(ServerRoles), "NetworkGlobalBadge", text);
		});
	}

	public static void SetTargetRankColor(this ReferenceHub hub, string color, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeSyncVar(hub.networkIdentity, typeof(ServerRoles), "Network_myColor", color);
		});
	}

	public static void SetTargetRankText(this ReferenceHub hub, string text, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeSyncVar(hub.networkIdentity, typeof(ServerRoles), "Network_myText", text);
		});
	}

	public static void SetTargetRank(this ReferenceHub hub, string color, string text, params ReferenceHub[] targets)
	{
		hub.SetTargetRankColor(color, targets);
		hub.SetTargetRankText(text, targets);
	}

	/*
	public static void SetTargetMapSeed(this ReferenceHub hub, int seed)
	{
		hub.SendFakeSyncVar(SeedSynchronizer._singleton.netIdentity, typeof(SeedSynchronizer), "Network_syncSeed", seed);
	}*/

	public static void SetTargetMouseSpawn(this ReferenceHub hub, byte spawn)
	{
		hub.SendFakeSyncVar(UnityEngine.Object.FindObjectOfType<SqueakSpawner>().netIdentity, typeof(SqueakSpawner), "NetworksyncSpawn", spawn);
	}

	public static void SetTargetChaosCount(this ReferenceHub hub, int count)
	{
		hub.SendFakeSyncVar(RoundSummary.singleton.netIdentity, typeof(RoundSummary), "Network_chaosTargetCount", count);
	}

	public static void SetTargetIntercomText(this ReferenceHub hub, string text)
	{
		hub.SendFakeSyncVar(IntercomDisplay._singleton.netIdentity, typeof(IntercomDisplay), "Network_overrideText", text);
	}

	public static void SetTargetIntercomState(this ReferenceHub hub, IntercomState state)
	{
		hub.SendFakeSyncVar(Intercom._singleton.netIdentity, typeof(Intercom), "Network_state", (byte)state);
	}

	public static void SendWarheadShake(this ReferenceHub hub, bool achieve = true)
	{
		hub.SendFakeTargetRpc(AlphaWarheadController.Singleton.netIdentity, typeof(AlphaWarheadController), "RpcShake", achieve);
	}

	public static void SendHitmarker(this ReferenceHub hub, float size = 1f)
	{
		Hitmarker.SendHitmarkerDirectly(hub.connectionToClient, size);
	}

	public static void SendDimScreen(this ReferenceHub hub)
	{
		hub.SendFakeTargetRpc(RoundSummary.singleton.netIdentity, typeof(RoundSummary), "RpcDimScreen");
	}

	public static void SendShowRoundSummary(this ReferenceHub hub, RoundSummary.SumInfo_ClassList startClassList, RoundSummary.SumInfo_ClassList endClassList, RoundSummary.LeadingTeam leadingTeam, int escapedClassD, int escapedScientists, int scpKills, int roundCd, int durationSeconds)
	{
		hub.SendFakeTargetRpc(RoundSummary.singleton.netIdentity, typeof(RoundSummary), "RpcShowRoundSummary", startClassList, endClassList, leadingTeam, escapedClassD, escapedScientists, scpKills, roundCd, durationSeconds);
	}

	public static void SendHiddenRole(this ReferenceHub hub, string text)
	{
		hub.serverRoles.TargetSetHiddenRole(hub.connectionToClient, text);
	}

	public static void SendTeslaTrigger(this TeslaGate gate, params ReferenceHub[] targets)
	{
		targets.ForEach(delegate(ReferenceHub target)
		{
			target.SendFakeTargetRpc(gate.netIdentity, typeof(TeslaGate), "RpcInstantBurst");
		});
	}

	public static void SendRoundRestart(this ReferenceHub hub, bool shouldReconnect = true, bool extendedTime = false, bool isFast = false, float offset = 0f, ushort? redirect = null)
	{
		hub.connectionToClient.Send(new RoundRestartMessage(redirect.HasValue ? RoundRestartType.RedirectRestart : (isFast ? RoundRestartType.RedirectRestart : RoundRestartType.FullRestart), offset, (ushort)(redirect.HasValue ? redirect.Value : 0), shouldReconnect, extendedTime));
	}

	public static void SetSize(this ReferenceHub hub, Vector3 size)
	{
		if (hub.transform.localScale == size)
		{
			return;
		}
		hub.transform.localScale = size;
		foreach (ReferenceHub hub2 in Hub.Hubs)
		{
			NetworkServer.SendSpawnMessage(hub.connectionToClient.identity, hub2.connectionToClient);
		}
	}

	public static void SetScale(this ReferenceHub hub, float scale)
	{
		hub.SetSize(hub.transform.localScale * scale);
	}

	public static void SetVoicePitch(this ReferenceHub hub, float voicePitch)
	{
		if (voicePitch == 1f)
		{
			hub.DisableVoicePitch();
			return;
		}
		IVoiceProfile profile = Compendium.Voice.VoiceChat.GetProfile(hub);
		if (profile != null && profile is PitchProfile pitchProfile)
		{
			pitchProfile.Pitch = voicePitch;
			return;
		}
		PitchProfile pitchProfile2 = new PitchProfile(hub);
		Compendium.Voice.VoiceChat.SetProfile(hub, pitchProfile2);
		pitchProfile2.Pitch = voicePitch;
	}

	public static void DisableVoicePitch(this ReferenceHub hub)
	{
		IVoiceProfile profile = Compendium.Voice.VoiceChat.GetProfile(hub);
		if (profile != null && profile is PitchProfile pitchProfile)
		{
			pitchProfile.Pitch = 1f;
		}
	}

	public static void PlayTo(this ReferenceHub hub, string audioId)
	{
		Compendium.Sounds.Audio.PlayTo(hub, audioId, hub.transform.position, audioId);
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		_fakePositions.Clear();
		_fakePositionsMatrix.Clear();
		_fakeIntensity.Clear();
		_raIcons.Clear();
		_invisList.Clear();
		_invisMatrix.Clear();
	}
}
