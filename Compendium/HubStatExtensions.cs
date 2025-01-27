using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;

namespace Compendium;

public static class HubStatExtensions
{
	public static float Stamina(this ReferenceHub hub, float? newValue = null)
	{
		if (!hub.playerStats.TryGetModule<StaminaStat>(out var module))
		{
			return 0f;
		}
		if (!newValue.HasValue)
		{
			return module.CurValue;
		}
		return module.CurValue = newValue.Value;
	}

	public static float HumeShield(this ReferenceHub hub, float? newValue = null)
	{
		if (!hub.playerStats.TryGetModule<HumeShieldStat>(out var module))
		{
			return 0f;
		}
		if (!newValue.HasValue)
		{
			return module.CurValue;
		}
		return module.CurValue = newValue.Value;
	}

	public static float Vigor(this ReferenceHub hub, float? newValue = null)
	{
		if (hub.Role() is Scp106Role scp106Role)
		{
			if (!scp106Role.SubroutineModule.TryGetSubroutine<Scp106VigorAbilityBase>(out var subroutine))
			{
				return 0f;
			}
			if (!newValue.HasValue)
			{
				return subroutine.VigorAmount;
			}
			return subroutine.VigorAmount = newValue.Value;
		}
		return 0f;
	}

	public static void Heal(this ReferenceHub hub, float hp)
	{
		hub.Health(hp + hub.Health());
	}

	public static void Kill(this ReferenceHub hub, DeathTranslation? reason = null)
	{
		hub.playerStats.KillPlayer(new UniversalDamageHandler(float.MaxValue, reason ?? DeathTranslations.Warhead));
	}

	public static void Kill(this ReferenceHub hub, string reason)
	{
		hub.playerStats.KillPlayer(new CustomReasonDamageHandler(reason, -1f));
	}

	public static void Damage(this ReferenceHub hub, float damage, DeathTranslation? reason = null)
	{
		hub.Damage(new UniversalDamageHandler(damage, reason ?? DeathTranslations.Warhead));
	}

	public static void Damage(this ReferenceHub hub, DamageHandlerBase damageHandlerBase)
	{
		damageHandlerBase.ApplyDamage(hub);
	}

	public static float Health(this ReferenceHub hub, float? newValue = null)
	{
		if (!hub.playerStats.TryGetModule<HealthStat>(out var module))
		{
			return 0f;
		}
		if (!newValue.HasValue)
		{
			return module.CurValue;
		}
		return module.CurValue = newValue.Value;
	}

	public static float MaxHealth(this ReferenceHub hub)
	{
		if (!hub.playerStats.TryGetModule<HealthStat>(out var module))
		{
			return 0f;
		}
		return module.MaxValue;
	}

	public static bool IsGrounded(this ReferenceHub hub)
	{
		if (!(hub.Role() is IFpcRole fpcRole))
		{
			return false;
		}
		return fpcRole.FpcModule.IsGrounded;
	}
}
