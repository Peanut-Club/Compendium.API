using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdminToys;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Extensions;
using Compendium.Prefabs;
using Compendium.Settings;
using Footprinting;
using helpers;
using helpers.Attributes;
using helpers.Dynamic;
using Mirror;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Enums;
using UnityEngine;

namespace Compendium;

public static class RoundHelper
{
	private static RoundState _state;

	private static object[] _stateArgs;

	public static readonly RoundState[] States = Enum.GetValues(typeof(RoundState)).Cast<RoundState>().ToArray();

	public static RoundState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_stateArgs == null)
			{
				_stateArgs = new object[1];
			}
			_state = value;
			_stateArgs[0] = value;
			AttributeRegistry<RoundStateChangedAttribute>.ForEachOfCondition(delegate(object[] data, AttributeData<RoundStateChangedAttribute> attribute)
			{
				if (attribute.Data == null)
				{
					return false;
				}
				int num = States.IndexOf(_state);
				return ((bool)attribute.Data[num + 1]) ? true : false;
			}, delegate(AttributeData<RoundStateChangedAttribute> attribute)
			{
				if (attribute.Member != null && attribute.Member is MethodInfo method)
				{
					method.InvokeDynamic(attribute.MemberHandle, ((bool)attribute.Data[0]) ? _stateArgs : CachedArray.EmptyObject);
				}
			});
		}
	}

	public static bool IsStarted => State == RoundState.InProgress;

	public static bool IsEnding => State == RoundState.Ending;

	public static bool IsRestarting => State == RoundState.Restarting;

	public static bool IsWaitingForPlayers => State == RoundState.WaitingForPlayers;

	public static bool IsReady => State != RoundState.Restarting;
	/* disabled
	public static ReferenceHub[] GetLastPlayers(bool isHumanPriority = true)
	{
		if (State != 0)
		{
			return CachedArray<ReferenceHub>.Array;
		}
		List<ReferenceHub> list = Pools.PoolList<ReferenceHub>();
		int num = Hub.GetHubs(Team.Scientists).Count() + Hub.GetHubs(Team.FoundationForces).Count();
		int num2 = Hub.GetHubs(RoleTypeId.ClassD).Count();
		int num3 = RoundSummary.singleton?.ChaosTargetCount ?? 0;
		int num4 = Hub.GetHubs(Team.SCPs).Count();
		Faction faction = Faction.FoundationEnemy;
		int num5 = 0;
		if (num > 0)
		{
			num5++;
		}
		if (num2 > 0 || num3 > 0)
		{
			num5++;
		}
		if (num4 > 0)
		{
			num5++;
		}
		if (num <= 0)
		{
			faction = Faction.FoundationStaff;
		}
		else if (num4 <= 0)
		{
			faction = Faction.SCP;
		}
		if (num5 != 2 || (num != 1 && (num3 != 1 || num2 != 0) && (num2 != 1 || num3 != 0) && num4 != 1))
		{
			list.ReturnList();
			return CachedArray<ReferenceHub>.Array;
		}
		switch (faction)
		{
		case Faction.SCP:
			if (Respawn.NtfTickets < 0.5f && num == 1)
			{
				list.Add(Hub.GetHubs(Faction.FoundationStaff).First());
			}
			else if ((num2 == 0 && num3 == 1) || (num2 == 1 && num3 == 0))
			{
				list.Add(Hub.GetHubs(Faction.FoundationEnemy).First());
			}
			else if (num == 1)
			{
				list.Add(Hub.GetHubs(Faction.FoundationStaff).First());
			}
			break;
		case Faction.FoundationEnemy:
			if (isHumanPriority && num == 1)
			{
				list.Add(Hub.GetHubs(Faction.FoundationStaff).First());
			}
			else if (num4 == 1)
			{
				list.Add(Hub.GetHubs(Team.SCPs).First());
			}
			else if (num == 1)
			{
				list.Add(Hub.GetHubs(Faction.FoundationStaff).First());
			}
			break;
		case Faction.FoundationStaff:
			if (!isHumanPriority && num4 == 1)
			{
				list.Add(Hub.GetHubs(Team.SCPs).First());
			}
			else if (num3 == 1 && num2 == 0)
			{
				list.Add(PickChaosTargetByDistance());
			}
			else if (num3 == 0 && num2 == 1)
			{
				list.Add(Hub.GetHubs(Team.ClassD).First());
			}
			else if (num4 == 1)
			{
				list.Add(Hub.GetHubs(Team.SCPs).First());
			}
			break;
		}
		ReferenceHub[] result = list.ToArray();
		list.ReturnList();
		return result;
	}*/

	public static ReferenceHub PickChaosTargetByDistance()
	{
		IEnumerable<ReferenceHub> hubs = Hub.GetHubs(Team.ChaosInsurgency);
		if (hubs.Count() <= 0)
		{
			return null;
		}
		IEnumerable<ReferenceHub> hubs2 = Hub.GetHubs(Team.SCPs);
		List<Tuple<ReferenceHub, ReferenceHub, float>> list = Pools.PoolList<Tuple<ReferenceHub, ReferenceHub, float>>();
		foreach (ReferenceHub item in hubs)
		{
			foreach (ReferenceHub item2 in hubs2)
			{
				list.Add(new Tuple<ReferenceHub, ReferenceHub, float>(item2, item, item.Position().DistanceSquared(item2.Position())));
			}
		}
		Tuple<ReferenceHub, ReferenceHub, float> tuple = list.OrderBy((Tuple<ReferenceHub, ReferenceHub, float> d) => d.Item3).FirstOrDefault();
		list.ReturnList();
		return tuple?.Item2 ?? null;
	}

	[Load]
	private static void Load()
	{
		AttributeRegistry<RoundStateChangedAttribute>.DataGenerator = AttributeDataGenerator;
	}

	[Event(ServerEventType.RoundEnd)]
	private static void OnEnd()
	{
		State = RoundState.Ending;
	}

	[Event(ServerEventType.RoundStart)]
	private static void OnStart()
	{
		State = RoundState.InProgress;
		foreach (FeatureSettings.ConfigVector lightPosition in Plugin.Config.FeatureSettings.LightPositions)
		{
			try
			{
				if ((lightPosition.X != 0f || lightPosition.Y != 0f || lightPosition.Z != 0f) && PrefabHelper.TryInstantiatePrefab(PrefabName.LightSource, out LightSourceToy component))
				{
					Vector3 position = lightPosition.ToUnityVector();
					component.transform.position = position;
					component.transform.localScale = Vector3.one;
					component.NetworkPosition = lightPosition.ToUnityVector();
					component.SpawnerFootprint = new Footprint(ReferenceHub.LocalHub);
					NetworkServer.Spawn(component.gameObject);
					Plugin.Info($"Spawned a light source at '{component.NetworkPosition}'");
				}
			}
			catch (Exception arg)
			{
				Plugin.Error($"Failed to spawn a light:\n{arg}");
			}
		}
	}

	[Event(ServerEventType.RoundRestart)]
	private static void OnRestart()
	{
		State = RoundState.Restarting;
	}

	[Event(ServerEventType.WaitingForPlayers)]
	private static void OnWaiting()
	{
		State = RoundState.WaitingForPlayers;
	}

	private static object[] AttributeDataGenerator(Type type, MemberInfo member, RoundStateChangedAttribute attribute)
	{
		if ((object)member == null || !(member is MethodInfo methodInfo))
		{
			return null;
		}
		object[] array = new object[1 + States.Length];
		ParameterInfo[] parameters = methodInfo.GetParameters();
		array[0] = parameters != null && parameters.Length != 0;
		for (int i = 0; i < States.Length; i++)
		{
			array[i + 1] = attribute.TargetStates.IsEmpty() || attribute.TargetStates.Contains(States[i]);
		}
		return array;
	}
}
