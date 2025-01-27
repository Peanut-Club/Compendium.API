using System;
using System.Text;
using CentralAuth;
using Compendium.Enums;
using Compendium.Npc;
using CustomPlayerEffects;
using helpers;
using helpers.Patching;
using helpers.Pooling.Pools;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.Visibility;
using RelativePositioning;
using RemoteAdmin;
using RemoteAdmin.Communication;
using UnityEngine;
using VoiceChat;

namespace Compendium;

public static class HubExtensionPatches
{
	[Patch(typeof(FpcServerPositionDistributor), "WriteAll", PatchType.Prefix, new Type[] { })]
	private static bool InvisPatch(ReferenceHub receiver, NetworkWriter writer)
	{
		ushort index = 0;
		ICustomVisibilityRole customVisibilityRole = receiver.roleManager.CurrentRole as ICustomVisibilityRole;
		bool hasRole = false;
		VisibilityController controller = null;
		if (customVisibilityRole != null)
		{
			hasRole = true;
			controller = customVisibilityRole.VisibilityController;
		}
		else
		{
			hasRole = false;
			controller = null;
		}
		ReferenceHub.AllHubs.ForEach(delegate(ReferenceHub hub)
		{
			if (hub.netId != receiver.netId && hub.Role() is IFpcRole fpcRole)
			{
				bool flag = hasRole && !controller.ValidateVisibility(hub);
				if (!flag)
				{
					if (hub.IsInvisible())
					{
						flag = true;
					}
					else if (hub.IsInvisibleTo(receiver))
					{
						flag = true;
					}
				}
				FpcSyncData newSyncData = FpcServerPositionDistributor.GetNewSyncData(receiver, hub, fpcRole.FpcModule, flag);
				if (!flag)
				{
					FpcServerPositionDistributor._bufferPlayerIDs[index] = hub.PlayerId;
					FpcServerPositionDistributor._bufferSyncData[index] = newSyncData;
					index++;
				}
			}
		});
		writer.WriteUShort(index);
		for (int i = 0; i < index; i++)
		{
			writer.WriteRecyclablePlayerId(new RecyclablePlayerId(FpcServerPositionDistributor._bufferPlayerIDs[i]));
			FpcServerPositionDistributor._bufferSyncData[i].Write(writer);
		}
		return false;
	}

	//[Patch(typeof(RaPlayerList), "ReceiveData", PatchType.Prefix, new Type[] { typeof(CommandSender), typeof(string) })]
	private static bool RaPlayerListPatch(RaPlayerList __instance, CommandSender sender, string data)
	{
		string[] array = data.Split(new char[1] { ' ' });
		if (array.Length != 3)
		{
			return false;
		}
		if (!int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2))
		{
			return false;
		}
		if (!Enum.IsDefined(typeof(RaPlayerList.PlayerSorting), result2))
		{
			return false;
		}
		bool flag = result == 1;
		bool flag2 = array[2].Equals("1");
		RaPlayerList.PlayerSorting sortingType = (RaPlayerList.PlayerSorting)result2;
		bool viewHiddenBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
		bool viewHiddenGlobalBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);
		if (sender is PlayerCommandSender playerCommandSender && playerCommandSender.ReferenceHub.authManager.NorthwoodStaff)
		{
			viewHiddenBadges = true;
			viewHiddenGlobalBadges = true;
		}
		StringBuilder stringBuilder = StringBuilderPool.Pool.Get();
		stringBuilder.Append("\n");
		foreach (ReferenceHub item in flag2 ? __instance.SortPlayersDescending(sortingType) : __instance.SortPlayers(sortingType))
		{
			if (item.Mode == ClientInstanceMode.ReadyClient && !NpcHub.IsNpc(item))
			{
				bool isInOverwatch = item.serverRoles.IsInOverwatch;
				bool flag3 = VoiceChatMutes.IsMuted(item);
				RemoteAdminIconType icon;
				bool flag4 = item.TryGetRaIcon(out icon);
				stringBuilder.Append(RaPlayerList.GetPrefix(item, viewHiddenBadges, viewHiddenGlobalBadges));
				if (isInOverwatch || (flag4 && icon == RemoteAdminIconType.Overwatch))
				{
					stringBuilder.Append("<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc>\uf06e</color><color=white>]</color></link> ");
				}
				if (flag3 || (flag4 && icon == RemoteAdminIconType.Muted))
				{
					stringBuilder.Append("<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ");
				}
				stringBuilder.Append("<color={RA_ClassColor}>(").Append(item.PlayerId).Append(") ");
				stringBuilder.Append(item.nicknameSync.CombinedName.Replace("\n", string.Empty).Replace("RA_", string.Empty)).Append("</color>");
				stringBuilder.AppendLine();
			}
		}
		sender.RaReply($"${__instance.DataId} {StringBuilderPool.Pool.PushReturn(stringBuilder)}", success: true, !flag, string.Empty);
		return false;
	}

	[Patch(typeof(FpcServerPositionDistributor), "GetNewSyncData", PatchType.Prefix, new Type[] { })]
	public static bool GenerateNewSyncData(ReferenceHub receiver, ReferenceHub target, FirstPersonMovementModule fpmm, bool isInvisible, ref FpcSyncData __result)
	{
		Vector3 position = Vector3.zero;
		if (!target.TryGetFakePosition(target, out position))
		{
			position = target.transform.position;
		}
		FpcSyncData prevSyncData = FpcServerPositionDistributor.GetPrevSyncData(receiver, target);
		FpcSyncData fpcSyncData = (isInvisible ? default(FpcSyncData) : new FpcSyncData(prevSyncData, fpmm.SyncMovementState, fpmm.IsGrounded, new RelativePosition(position), fpmm.MouseLook));
		FpcServerPositionDistributor.PreviouslySent[receiver.netId][target.netId] = fpcSyncData;
		__result = fpcSyncData;
		return false;
	}

	[Patch(typeof(PlayerEffectsController), "ServerSyncEffect", PatchType.Prefix, new Type[] { })]
	public static bool SyncEffectIntensity(PlayerEffectsController __instance, StatusEffectBase effect)
	{
		for (int i = 0; i < __instance.EffectsLength; i++)
		{
			StatusEffectBase statusEffectBase = __instance.AllEffects[i];
			if (statusEffectBase == effect)
			{
				if (__instance._hub.TryGetFakeIntensity(statusEffectBase.GetType(), out var intensity))
				{
					__instance._syncEffectsIntensity[i] = intensity;
				}
				else
				{
					__instance._syncEffectsIntensity[i] = statusEffectBase.Intensity;
				}
				return false;
			}
		}
		return false;
	}
}
