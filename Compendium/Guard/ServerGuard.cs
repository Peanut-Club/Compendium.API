using System;
using System.Collections.Generic;
using BetterCommands;
using BetterCommands.Permissions;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Guard.Steam;
using Compendium.Guard.Vpn;
using PluginAPI.Events;
using Utils.NonAllocLINQ;

namespace Compendium.Guard;

public static class ServerGuard
{
	private static readonly List<ServerGuardData> saveData;

	private static readonly List<string> whitelistData;

	public static IServerGuardProcessor VpnProcessor { get; }

	public static IServerGuardProcessor SteamProcessor { get; }

	public static event Action<IServerGuardProcessor, ReferenceHub, ServerGuardReason> OnKicking;

	public static event Action<string, string, ServerGuardReason> OnRejecting;

	public static bool IsOnWhitelist(string ipOrId)
	{
		return whitelistData.Contains(ipOrId);
	}

	public static void AddToWhitelist(string ipOrId)
	{
		if (!whitelistData.Contains(ipOrId))
		{
			whitelistData.Add(ipOrId);
			SaveFiles();
		}
	}

	public static void RemoveFromWhitelist(string ipOrId)
	{
		if (whitelistData.Remove(ipOrId))
		{
			SaveFiles();
		}
	}

	public static ServerGuardReason GetFlag(string ipOrId)
	{
		if (saveData.TryGetFirst((ServerGuardData s) => s.Ip == ipOrId || s.Id == ipOrId, out var first))
		{
			return first.Reason;
		}
		return ServerGuardReason.None;
	}

	public static void SetFlag(string ip, string id, ServerGuardReason reason)
	{
		if (saveData.TryGetFirst((ServerGuardData s) => s.Ip == ip || s.Id == id, out var first))
		{
			first.Ip = ip;
			first.Id = id;
			first.Reason = reason;
		}
		else
		{
			saveData.Add(new ServerGuardData
			{
				Ip = ip,
				Id = id,
				Reason = reason
			});
		}
		if (reason != 0 && reason != ServerGuardReason.Ignore)
		{
			RemoveFromWhitelist(ip);
			RemoveFromWhitelist(id);
		}
		SaveFiles();
	}

	public static PreauthCancellationData ProcessAuth(CentralAuthPreauthFlags preauthFlags, string ip, string id)
	{
		try
		{
			if (!preauthFlags.HasFlagFast(CentralAuthPreauthFlags.NorthwoodStaff) && !IsOnWhitelist(id) && !IsOnWhitelist(ip))
			{
				ServerGuardReason flag = GetFlag(ip);
				if (flag == ServerGuardReason.None || flag == ServerGuardReason.Ignore)
				{
					flag = GetFlag(id);
				}
				if (flag == ServerGuardReason.None || flag == ServerGuardReason.Ignore)
				{
					return PreauthCancellationData.Accept();
				}
				ServerGuard.OnRejecting?.Invoke(ip, id, flag);
				Plugin.Warn($"Rejecting incoming connection of '{ip} | {id}': {flag}");
				if (!Plugin.Config.GuardSettings.KickReasons.TryGetValue(flag, out var value))
				{
					value = "Undefined reason.";
				}
				return PreauthCancellationData.Reject("Tvé připojení bylo odmítnuto: " + value + "\nPro whitelist se připoj na náš Discord server (invite je v server info) a založ si žádost v kanále #support.", isForced: false);
			}
		}
		catch (Exception arg)
		{
			Plugin.Error($"An error occured during auth: {arg}");
		}
		return PreauthCancellationData.Accept();
	}

	public static void ProcessPlayer(ReferenceHub hub)
	{
		if (hub.IsNorthwoodModerator() || hub.IsNorthwoodStaff() || IsOnWhitelist(hub.Ip()) || IsOnWhitelist(hub.UserId()))
		{
			return;
		}
		ServerGuardReason flag = GetFlag(hub.UserId());
		if (flag == ServerGuardReason.None || flag == ServerGuardReason.Ignore)
		{
			flag = GetFlag(hub.Ip());
		}
		if (flag != 0 && flag != ServerGuardReason.Ignore)
		{
			Plugin.Warn($"Kicking player '{hub.GetLogName()}' due to a cached flag: {flag}");
			if (!Plugin.Config.GuardSettings.KickReasons.TryGetValue(flag, out var value))
			{
				value = "Undefined reason.";
			}
			ServerGuard.OnKicking?.Invoke(VpnProcessor, hub, flag);
			hub.Kick("Tvé připojení bylo odmítnuto: " + value + "\nPro whitelist se připoj na náš Discord server (invite je v server info) a založ si žádost v kanále #support.");
			return;
		}
		VpnProcessor?.Process(hub, delegate(ServerGuardReason vpnFlag)
		{
			if (vpnFlag != ServerGuardReason.Ignore)
			{
				if (vpnFlag > ServerGuardReason.None)
				{
					RemoveFromWhitelist(hub.Ip());
					RemoveFromWhitelist(hub.UserId());
					SetFlag(hub.Ip(), hub.UserId(), vpnFlag);
					Plugin.Warn($"Kicking player '{hub.GetLogName()}' due to a positive VPN flag: {vpnFlag}");
					if (!Plugin.Config.GuardSettings.KickReasons.TryGetValue(vpnFlag, out var value2))
					{
						value2 = "Undefined reason.";
					}
					ServerGuard.OnKicking?.Invoke(VpnProcessor, hub, vpnFlag);
					hub.Kick("Tvé připojení bylo odmítnuto: " + value2 + "\nPro whitelist se připoj na náš Discord server (invite je v server info) a založ si žádost v kanále #support.");
				}
				else
				{
					AddToWhitelist(hub.Ip());
				}
			}
			if (hub.ParsedUserId().Type == UserIdType.Steam)
			{
				SteamProcessor?.Process(hub, delegate(ServerGuardReason steamFlag)
				{
					switch (steamFlag)
					{
					default:
					{
						RemoveFromWhitelist(hub.Ip());
						RemoveFromWhitelist(hub.UserId());
						if (steamFlag != ServerGuardReason.NotSetupAccount && steamFlag != ServerGuardReason.PrivateAccount)
						{
							Plugin.Warn("Flagging!");
							SetFlag(hub.Ip(), hub.UserId(), steamFlag);
						}
						Plugin.Warn($"Kicking player '{hub.GetLogName()}' due to a positive STEAM flag: {steamFlag}");
						if (!Plugin.Config.GuardSettings.KickReasons.TryGetValue(steamFlag, out var value3))
						{
							value3 = "Undefined reason.";
						}
						ServerGuard.OnKicking?.Invoke(SteamProcessor, hub, steamFlag);
						hub.Kick("Tvé připojení bylo odmítnuto: " + value3 + "\nPro whitelist se připoj na náš Discord server (invite je v server info) a založ si žádost v kanále #support.");
						break;
					}
					case ServerGuardReason.None:
						AddToWhitelist(hub.UserId());
						break;
					case ServerGuardReason.Ignore:
						break;
					}
				});
			}
		});
	}

	private static void SaveFiles()
	{
		try
		{
			Directories.SetData("GuardWhitelist.json", "guardWhitelist", useGlobal: true, whitelistData);
			Directories.SetData("GuardData.json", "guardFlags", useGlobal: true, saveData);
			Plugin.Debug("Saved data.");
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed while saving data: {arg}");
		}
	}

	private static void LoadFiles()
	{
		try
		{
			saveData.Clear();
			saveData.AddRange(Directories.GetData("GuardData.json", "guardFlags", useGlobal: true, saveData));
			whitelistData.Clear();
			whitelistData.AddRange(Directories.GetData("GuardWhitelist.json", "guardWhitelist", useGlobal: true, whitelistData));
			foreach (string user in WhiteList.Users)
			{
				whitelistData.AddIfNotContains(user);
			}
			SaveFiles();
			Plugin.Info($"Loaded {saveData.Count} saved guard data");
			Plugin.Info($"Loaded {whitelistData.Count} whitelists");
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed while loading save files: {arg}");
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnWaiting()
	{
		LoadFiles();
	}

	[Event]
	private static void OnPlayerJoined(PlayerJoinedEvent ev)
	{
		ProcessPlayer(ev.Player.ReferenceHub);
	}

	[Event]
	private static PreauthCancellationData OnPlayerAuth(PlayerPreauthEvent ev)
	{
		return ProcessAuth(ev.CentralFlags, ev.IpAddress, ev.UserId);
	}

	[Command("guardflag", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Sets the target's guard flag.")]
	[Permission(PermissionLevel.Administrator)]
	private static string SetFlagCommand(ReferenceHub sender, string targetIp, string targetId, ServerGuardReason flag)
	{
		SetFlag(targetIp, targetId, flag);
		return $"Flag of '{targetId} | {targetIp}' set to {flag}";
	}

	[Command("guardwh", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Sets the target's whitelist status.")]
	[Permission(PermissionLevel.Administrator)]
	private static string WhitelistCommand(ReferenceHub sender, string whitelistTarget)
	{
		saveData.RemoveAll((ServerGuardData s) => s.Ip == whitelistTarget || s.Id == whitelistTarget);
		if (whitelistData.Contains(whitelistTarget))
		{
			RemoveFromWhitelist(whitelistTarget);
			return "Whitelist of '" + whitelistTarget + "' has been removed.";
		}
		AddToWhitelist(whitelistTarget);
		return "Whitelist of '" + whitelistTarget + "' has been added.";
	}

	[Command("guardreload", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[Description("Reloads Server Guard's save files.")]
	[Permission(PermissionLevel.Administrator)]
	private static string ReloadCommand(ReferenceHub sender)
	{
		LoadFiles();
		return "Save files reloaded.";
	}

	static ServerGuard()
	{
		saveData = new List<ServerGuardData>();
		whitelistData = new List<string>();
		VpnProcessor = new VpnProcessor();
		SteamProcessor = new SteamProcessor();
	}
}
