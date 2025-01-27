using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using helpers;
using helpers.Attributes;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Compendium;

public static class MirrorHelper
{
	private static volatile Dictionary<Type, MethodInfo> _writers = new Dictionary<Type, MethodInfo>();

	private static volatile Dictionary<string, ulong> _syncVars = new Dictionary<string, ulong>();

	private static volatile Dictionary<string, string> _rpcMatrix = new Dictionary<string, string>();

	[Load]
	private static void Load()
	{
		try
		{
			Assembly assembly = typeof(RoleTypeId).Assembly;
			Type generatedClass = assembly.GetType("Mirror.GeneratedNetworkCode");
			new Thread((ThreadStart)delegate
			{
				foreach (MethodInfo item in typeof(NetworkWriterExtensions).GetMethods().Where(delegate(MethodInfo x)
				{
					if (!x.IsGenericMethod && x.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
					{
						ParameterInfo[] parameters2 = x.GetParameters();
						if (parameters2 == null)
						{
							return false;
						}
						return parameters2.Length == 2;
					}
					return false;
				}))
				{
					Type parameterType = item.GetParameters().First((ParameterInfo x) => x.ParameterType != typeof(NetworkWriter)).ParameterType;
					_writers[parameterType] = item;
				}
				foreach (MethodInfo item2 in generatedClass.GetMethods().Where(delegate(MethodInfo x)
				{
					if (!x.IsGenericMethod)
					{
						ParameterInfo[] parameters = x.GetParameters();
						if (parameters != null && parameters.Length == 2)
						{
							return x.ReturnType == typeof(void);
						}
					}
					return false;
				}))
				{
					Type parameterType2 = item2.GetParameters().First((ParameterInfo x) => x.ParameterType != typeof(NetworkWriter)).ParameterType;
					_writers[parameterType2] = item2;
				}
				foreach (Type item3 in from x in assembly.GetTypes()
					where x.Name.EndsWith("Serializer")
					select x)
				{
					foreach (MethodInfo item4 in from x in item3.GetMethods()
						where x.ReturnType == typeof(void) && x.Name.StartsWith("Write")
						select x)
					{
						Type parameterType3 = item4.GetParameters().First((ParameterInfo x) => x.ParameterType != typeof(NetworkWriter)).ParameterType;
						_writers[parameterType3] = item4;
					}
				}
				foreach (PropertyInfo item5 in from m in assembly.GetTypes().SelectMany((Type x) => x.GetProperties())
					where m.Name.StartsWith("Network")
					select m)
				{
					MethodInfo setMethod = item5.GetSetMethod();
					if ((object)setMethod != null)
					{
						MethodBody methodBody = setMethod.GetMethodBody();
						if (methodBody != null)
						{
							byte[] iLAsByteArray = methodBody.GetILAsByteArray();
							if (!_syncVars.ContainsKey(item5.ReflectedType.Name + "." + item5.Name))
							{
								Dictionary<string, ulong> syncVars = _syncVars;
								string key = item5.ReflectedType.Name + "." + item5.Name;
								OpCode ldc_I = OpCodes.Ldc_I8;
								syncVars.Add(key, iLAsByteArray[iLAsByteArray.LastIndexOf((byte)ldc_I.Value) + 1]);
							}
						}
					}
				}
				foreach (MethodInfo item6 in from m in assembly.GetTypes().SelectMany((Type x) => x.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					where m.GetCustomAttributes(typeof(ClientRpcAttribute), inherit: false).Length != 0 || m.GetCustomAttributes(typeof(TargetRpcAttribute), inherit: false).Length != 0
					select m)
				{
					MethodBody methodBody2 = item6.GetMethodBody();
					if (methodBody2 != null)
					{
						byte[] iLAsByteArray2 = methodBody2.GetILAsByteArray();
						if (!_rpcMatrix.ContainsKey(item6.ReflectedType.Name + "." + item6.Name))
						{
							Dictionary<string, string> rpcMatrix = _rpcMatrix;
							string key2 = item6.ReflectedType.Name + "." + item6.Name;
							Module module = item6.Module;
							OpCode ldstr = OpCodes.Ldstr;
							rpcMatrix.Add(key2, module.ResolveString(BitConverter.ToInt32(iLAsByteArray2, iLAsByteArray2.IndexOf((byte)ldstr.Value) + 1)));
						}
					}
				}
				Plugin.Info($"Mirror Networking loaded! writers={_writers.Count} syncVars={_syncVars.Count} rpc={_rpcMatrix.Count}");
			}).Start();
		}
		catch (Exception message)
		{
			Plugin.Error("Failed to load Mirror!");
			Plugin.Error(message);
		}
	}

	public static void SendFakeSyncVar(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string propertyName, object value)
	{
		if ((object)behaviorOwner == null)
		{
			behaviorOwner = ReferenceHub.HostHub.networkIdentity;
		}
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		NetworkWriterPooled networkWriterPooled2 = NetworkWriterPool.Get();
		behaviorOwner.MakeCustomSyncWriter(targetType, null, CustomSyncVarGenerator, networkWriterPooled, networkWriterPooled2);
		target.connectionToClient.Send(new EntityStateMessage
		{
			netId = behaviorOwner.netId,
			payload = networkWriterPooled.ToArraySegment()
		});
		NetworkWriterPool.Return(networkWriterPooled);
		NetworkWriterPool.Return(networkWriterPooled2);
		void CustomSyncVarGenerator(NetworkWriter targetWriter)
		{
			targetWriter.WriteULong(_syncVars[targetType.Name + "." + propertyName]);
			_writers[value.GetType()]?.Invoke(null, new object[2] { targetWriter, value });
		}
	}

	public static void ResyncSyncVar(this NetworkIdentity behaviorOwner, Type targetType, string propertyName)
	{
		if ((object)behaviorOwner == null)
		{
			behaviorOwner = ReferenceHub.HostHub.networkIdentity;
		}
		Component component = behaviorOwner.gameObject.GetComponent(targetType);
		if (!(component is NetworkBehaviour networkBehaviour))
		{
			Plugin.Warn("Attempted to re-synchronize variables of a behaviour not derived from NetworkBehaviour: '" + targetType.FullName + "'");
		}
		else
		{
			networkBehaviour.SetSyncVarDirtyBit(_syncVars[targetType.Name + "." + propertyName]);
		}
	}

	public static void SendFakeTargetRpc(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string rpcName, params object[] values)
	{
		if ((object)behaviorOwner == null)
		{
			behaviorOwner = ReferenceHub.HostHub.networkIdentity;
		}
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		foreach (object obj in values)
		{
			_writers[obj.GetType()].Invoke(null, new object[2] { networkWriterPooled, obj });
		}
		RpcMessage rpcMessage = default(RpcMessage);
		rpcMessage.netId = behaviorOwner.netId;
		rpcMessage.componentIndex = (byte)behaviorOwner.GetComponentIndex(targetType);
		rpcMessage.functionHash = (ushort)_rpcMatrix[targetType.Name + "." + rpcName].GetStableHashCode();
		rpcMessage.payload = networkWriterPooled.ToArraySegment();
		RpcMessage message = rpcMessage;
		if (target.connectionToClient != null)
		{
			target.connectionToClient.Send(message);
		}
		else
		{
			Plugin.Warn("Failed to send fake RPC to " + target.GetLogName(includeIp: true) + ": target's client connection is null!");
		}
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public static void SendFakeSyncObject(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customAction)
	{
		if ((object)behaviorOwner == null)
		{
			behaviorOwner = ReferenceHub.HostHub.networkIdentity;
		}
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		NetworkWriterPooled networkWriterPooled2 = NetworkWriterPool.Get();
		behaviorOwner.MakeCustomSyncWriter(targetType, customAction, null, networkWriterPooled, networkWriterPooled2);
		target.networkIdentity.connectionToClient.Send(new EntityStateMessage
		{
			netId = behaviorOwner.netId,
			payload = networkWriterPooled.ToArraySegment()
		});
		NetworkWriterPool.Return(networkWriterPooled);
		NetworkWriterPool.Return(networkWriterPooled2);
	}

	public static void EditNetworkObject(this NetworkIdentity identity, Action<NetworkIdentity> customAction)
	{
		customAction(identity);
		ObjectDestroyMessage objectDestroyMessage = new ObjectDestroyMessage
		{
			netId = identity.netId
		};
		Hub.Hubs.ForEach(delegate(ReferenceHub hub)
		{
			hub.connectionToClient.Send(objectDestroyMessage);
			NetworkServer.SendSpawnMessage(identity, hub.connectionToClient);
		});
	}

	public static int GetComponentIndex(this NetworkIdentity identity, Type type)
	{
		return Array.FindIndex(identity.NetworkBehaviours, (NetworkBehaviour x) => x.GetType() == type);
	}

	public static void MakeCustomSyncWriter(this NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customSyncObject, Action<NetworkWriter> customSyncVar, NetworkWriter owner, NetworkWriter observer)
	{
		if ((object)behaviorOwner == null)
		{
			behaviorOwner = ReferenceHub.HostHub.networkIdentity;
		}
		ulong value = 0uL;
		NetworkBehaviour networkBehaviour = null;
		for (int i = 0; i < behaviorOwner.NetworkBehaviours.Length; i++)
		{
			if (behaviorOwner.NetworkBehaviours[i].GetType() == targetType)
			{
				networkBehaviour = behaviorOwner.NetworkBehaviours[i];
				value = (ulong)(1L << (i & 0x1F));
				break;
			}
		}
		Compression.CompressVarUInt(owner, value);
		int position = owner.Position;
		owner.WriteByte(0);
		int position2 = owner.Position;
		if (customSyncObject != null)
		{
			customSyncObject(owner);
		}
		else
		{
			networkBehaviour.SerializeObjectsDelta(owner);
		}
		customSyncVar?.Invoke(owner);
		int position3 = owner.Position;
		owner.Position = position;
		owner.WriteByte((byte)((uint)(position3 - position2) & 0xFFu));
		owner.Position = position3;
		if (networkBehaviour.syncMode != 0)
		{
			observer.WriteBytes(owner.ToArraySegment().Array, position, owner.Position - position);
		}
	}
}
