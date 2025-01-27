using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using helpers;
using helpers.Extensions;
using helpers.Time;
using InventorySystem.Items.Usables;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Events;
using UnityEngine;

namespace Compendium.Processors;

public static class RoundStatProcessor
{
	public static bool IsLocked;

	public static int TotalKills = 0;

	public static int TotalScpKills = 0;

	public static int TotalScpDamage = 0;

	public static int TotalDamage = 0;

	public static int TotalHealsUsed = 0;

	public static int TotalDeaths = 0;

	public static int TotalExplosiveGrenades = 0;

	public static int TotalFlashGrenades = 0;

	public static int TotalScpGrenades = 0;

	public static int TotalEscapes = 0;

	public static int TotalScp079Assists = 0;

	public static TimeSpan FastestEscape = TimeSpan.MinValue;

	public static TimeSpan FastestDeath = TimeSpan.MinValue;

	public static RoleTypeId FastestEscapeRole = RoleTypeId.None;

	public static ReferenceHub FastestDeathPlayer = null;

	public static ReferenceHub FastestEscapePlayer = null;

	public static readonly Dictionary<ReferenceHub, int> HumanKills = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> ScpKills = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> HumanDamage = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> ScpDamage = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> Deaths = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> ExplosiveGrenades = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> FlashGrenades = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> ScpGrenades = new Dictionary<ReferenceHub, int>();

	public static readonly Dictionary<ReferenceHub, int> HealsUsed = new Dictionary<ReferenceHub, int>();

	[Event]
	public static void OnAssist(Scp079GainExperienceEvent ev)
	{
		if (!IsLocked && ev.Player != null && ev.Reason == Scp079HudTranslation.ExpGainTerminationAssist)
		{
			TotalScp079Assists++;
		}
	}

	[Event]
	public static void OnDamage(PlayerDamageEvent ev)
	{
		if (IsLocked || ev.Player == null || ev.DamageHandler == null)
		{
			return;
		}
		if (ev.Player.IsSCP)
		{
			TotalScpDamage += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
		}
		else
		{
			TotalDamage += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
		}
		if (!(ev.DamageHandler is AttackerDamageHandler attackerDamageHandler) || !(attackerDamageHandler.Attacker.Hub != null) || attackerDamageHandler.Attacker.Hub.IsServer())
		{
			return;
		}
		if (ev.Player.IsSCP)
		{
			if (!ScpDamage.ContainsKey(attackerDamageHandler.Attacker.Hub))
			{
				ScpDamage[attackerDamageHandler.Attacker.Hub] = Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
			}
			else
			{
				ScpDamage[attackerDamageHandler.Attacker.Hub] += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
			}
		}
		else if (!HumanDamage.ContainsKey(attackerDamageHandler.Attacker.Hub))
		{
			HumanDamage[attackerDamageHandler.Attacker.Hub] = Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
		}
		else
		{
			HumanDamage[attackerDamageHandler.Attacker.Hub] += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
		}
	}

	[Event]
	public static void OnUsable(PlayerUsedItemEvent ev)
	{
		if (IsLocked || (object)ev.Item == null || !(ev.Item is Medkit))
		{
			return;
		}
		TotalHealsUsed++;
		if (ev.Player != null)
		{
			if (!HealsUsed.ContainsKey(ev.Player.ReferenceHub))
			{
				HealsUsed[ev.Player.ReferenceHub] = 1;
			}
			else
			{
				HealsUsed[ev.Player.ReferenceHub]++;
			}
		}
	}

	[Event]
	public static void OnEscaped(PlayerEscapeEvent ev)
	{
		if (!IsLocked)
		{
			TotalEscapes++;
			if (ev.Player != null && FastestEscape <= TimeSpan.MinValue)
			{
				FastestEscape = Round.Duration;
				FastestEscapePlayer = ev.Player.ReferenceHub;
				FastestEscapeRole = ev.Player.Role;
			}
		}
	}

	[Event]
	public static void OnGrenadeThrown(PlayerThrowProjectileEvent ev)
	{
		if (IsLocked || (object)ev.Item == null)
		{
			return;
		}
		if (ev.Item.ItemTypeId == ItemType.SCP018)
		{
			TotalScpGrenades++;
			if (ev.Thrower != null)
			{
				if (!ScpGrenades.ContainsKey(ev.Thrower.ReferenceHub))
				{
					ScpGrenades[ev.Thrower.ReferenceHub] = 1;
				}
				else
				{
					ScpGrenades[ev.Thrower.ReferenceHub]++;
				}
			}
		}
		else if (ev.Item.ItemTypeId == ItemType.GrenadeHE)
		{
			TotalExplosiveGrenades++;
			if (ev.Thrower != null)
			{
				if (!ExplosiveGrenades.ContainsKey(ev.Thrower.ReferenceHub))
				{
					ExplosiveGrenades[ev.Thrower.ReferenceHub] = 1;
				}
				else
				{
					ExplosiveGrenades[ev.Thrower.ReferenceHub]++;
				}
			}
		}
		else
		{
			if (ev.Item.ItemTypeId != ItemType.GrenadeFlash)
			{
				return;
			}
			TotalFlashGrenades++;
			if (ev.Thrower != null)
			{
				if (!FlashGrenades.ContainsKey(ev.Thrower.ReferenceHub))
				{
					FlashGrenades[ev.Thrower.ReferenceHub] = 1;
				}
				else
				{
					FlashGrenades[ev.Thrower.ReferenceHub]++;
				}
			}
		}
	}

	[Event]
	public static void OnDeath(PlayerDeathEvent ev)
	{
		if (IsLocked)
		{
			return;
		}
		TotalDeaths++;
		if (ev.Player == null || ev.DamageHandler == null)
		{
			return;
		}
		if (!Deaths.ContainsKey(ev.Player.ReferenceHub))
		{
			Deaths[ev.Player.ReferenceHub] = 1;
		}
		else
		{
			Deaths[ev.Player.ReferenceHub]++;
		}
		if (FastestDeath <= TimeSpan.MinValue)
		{
			FastestDeath = Round.Duration;
			FastestDeathPlayer = ev.Player.ReferenceHub;
		}
		if (ev.Player.IsSCP)
		{
			TotalScpKills++;
		}
		else
		{
			TotalKills++;
		}
		if (!(ev.DamageHandler is AttackerDamageHandler attackerDamageHandler) || !(attackerDamageHandler.Attacker.Hub != null) || attackerDamageHandler.Attacker.Hub.IsServer())
		{
			return;
		}
		if (ev.Player.IsSCP)
		{
			if (!ScpKills.ContainsKey(attackerDamageHandler.Attacker.Hub))
			{
				ScpKills[attackerDamageHandler.Attacker.Hub] = 1;
			}
			else
			{
				ScpKills[attackerDamageHandler.Attacker.Hub]++;
			}
		}
		else if (!HumanKills.ContainsKey(attackerDamageHandler.Attacker.Hub))
		{
			HumanKills[attackerDamageHandler.Attacker.Hub] = 1;
		}
		else
		{
			HumanKills[attackerDamageHandler.Attacker.Hub]++;
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	public static void OnWaiting()
	{
		TotalKills = 0;
		TotalScpKills = 0;
		TotalScpDamage = 0;
		TotalHealsUsed = 0;
		TotalExplosiveGrenades = 0;
		TotalFlashGrenades = 0;
		TotalScpGrenades = 0;
		TotalEscapes = 0;
		TotalScp079Assists = 0;
		TotalDeaths = 0;
		FastestEscape = TimeSpan.MinValue;
		FastestDeath = TimeSpan.MinValue;
		FastestEscapeRole = RoleTypeId.None;
		FastestEscapePlayer = null;
		FastestDeathPlayer = null;
		HumanKills.Clear();
		ScpKills.Clear();
		HumanDamage.Clear();
		ScpDamage.Clear();
		ExplosiveGrenades.Clear();
		FlashGrenades.Clear();
		ScpGrenades.Clear();
		HealsUsed.Clear();
		Deaths.Clear();
		IsLocked = false;
	}

	[RoundStateChanged(new RoundState[] { RoundState.Ending })]
	public static void OnRoundEnd()
	{
		IsLocked = true;
		foreach (ReferenceHub hub in Hub.Hubs)
		{
			StringBuilder stringBuilder = Pools.PoolStringBuilder();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("<size=18><align=left>");
			stringBuilder.AppendLine("<b><color=#FF0000>[STATISTIKY - GLOBÁLNÍ]</color></b>");
			stringBuilder.AppendLine(string.Format("<b><color={0}>Počet zabití: <color={1}>{2}</color> / <color={3}>{4}</color> (<color={5}>SCP</color>)</color></b>", "#33FFA5", "#90FF33", TotalKills, "#90FF33", TotalScpKills, "#FF0000"));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Damage: <color={1}>{2} HP</color> / <color={3}>{4}</color> HP (<color={5}>SCP</color>)</color></b>", "#33FFA5", "#90FF33", TotalDamage, "#90FF33", TotalScpDamage, "#FF0000"));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Počet použitých medkitů: <color={1}>{2}</color></color></b>", "#33FFA5", "#90FF33", TotalHealsUsed));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Počet smrtí: <color={1}>{2}</color></color></b>", "#33FFA5", "#90FF33", TotalDeaths));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Počet granátů: <color={1}>{2} HE / <color={3}>{4}</color> FL / <color={5}>{6}</color> SCP</color></color></b>", "#33FFA5", "#90FF33", TotalExplosiveGrenades, "#90FF33", TotalFlashGrenades, "#90FF33", TotalScpGrenades));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Počet útěků: <color={1}>{2}</color></color></b>", "#33FFA5", "#90FF33", TotalEscapes));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Počet asistencí SCP-079: <color={1}>{2}</color></color></b>", "#33FFA5", "#90FF33", TotalScp079Assists));
			if (FastestEscapeRole != RoleTypeId.None)
			{
				stringBuilder.AppendLine("<b><color=#FF0000>První útěk: <color=#33FFA5>" + (FastestEscapePlayer?.Nick() ?? "Neznámý hráč") + "</color> za <color=#" + FastestEscapeRole.GetRoleColorHex() + ">" + FastestEscapeRole.ToString().SpaceByPascalCase() + "</color> (<color=#FF0000>" + FastestEscape.UserFriendlySpan() + "</color>)</color></b>");
			}
			if (FastestDeathPlayer != null)
			{
				stringBuilder.AppendLine("<b><color=#FF0000>První smrt: <color=#33FFA5>" + (FastestDeathPlayer?.Nick() ?? "Neznámý hráč") + "</color> (<color=#FF0000>" + FastestDeath.UserFriendlySpan() + "</color>)</color></b>");
			}
			KeyValuePair<ReferenceHub, int> keyValuePair = HumanKills.OrderByDescending((KeyValuePair<ReferenceHub, int> p) => p.Value).FirstOrDefault();
			if (keyValuePair.Key != null)
			{
				stringBuilder.AppendLine(string.Format("<b><color={0}>Nejvíce zabití: <color={1}>{2}</color> (<color={3}>{4}</color>)</color></b>", "#FF0000", "#33FFA5", keyValuePair.Key.Nick(), "#33FFA5", keyValuePair.Value));
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("<b><color=#FF0000>[STATISTIKY - PERSONÁLNÍ]</color></b>");
			stringBuilder.AppendLine(string.Format("<b><color={0}>Zabití: <color={1}>{2}</color> / <color={3}>{4}</color> (<color={5}>SCP</color>)</color></b>", "#90FF33", "#33FFA5", HumanKills.TryGetValue(hub, out var value) ? value : 0, "#33FFA5", ScpKills.TryGetValue(hub, out var value2) ? value2 : 0, "#FF0000"));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Smrtí: <color={1}>{2}</color></color></b>", "#90FF33", "#33FFA5", Deaths.TryGetValue(hub, out var value3) ? value3 : 0));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Granátů: <color={1}>{2} HE / <color={3}>{4}</color> FLASH / <color={5}>{6}</color> SCP</color></color></b>", "#90FF33", "#33FFA5", ExplosiveGrenades.TryGetValue(hub, out var value4) ? value4 : 0, "#33FFA5", FlashGrenades.TryGetValue(hub, out var value5) ? value5 : 0, "#33FFA5", ScpGrenades.TryGetValue(hub, out var value6) ? value6 : 0));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Medkitů: <color={1}>{2}</color></color></b>", "#90FF33", "#33FFA5", HealsUsed.TryGetValue(hub, out var value7) ? value7 : 0));
			stringBuilder.AppendLine(string.Format("<b><color={0}>Damage: <color={1}>{2} HP</color> / <color={3}>{4}</color> HP (<color={5}>SCP</color>)</color></b>", "#90FF33", "#33FFA5", HumanDamage.TryGetValue(hub, out var value8) ? value8 : 0, "#33FFA5", ScpDamage.TryGetValue(hub, out var value9) ? value9 : 0, "#FF0000"));
			stringBuilder.AppendLine("</align></size>");
			hub.Hint(stringBuilder.ReturnStringBuilderValue(), 50f);
		}
	}
}
