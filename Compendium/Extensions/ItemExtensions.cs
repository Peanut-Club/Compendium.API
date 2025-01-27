using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Extensions;

public static class ItemExtensions
{
	public static IReadOnlyList<ItemType> AllItems { get; } = Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList()
		.AsReadOnly();


	public static IReadOnlyList<ItemType> AllValidItems { get; } = AllItems.Where((ItemType i) => i != ItemType.None).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Ammo { get; } = AllValidItems.Where((ItemType i) => i.IsAmmo()).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Firearms { get; } = AllValidItems.Where((ItemType i) => i.IsFirearm()).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Explosives { get; } = AllValidItems.Where((ItemType i) => i.IsExplosive()).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Keycards { get; } = AllValidItems.Where((ItemType i) => i.IsKeycard()).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Medicals { get; } = AllValidItems.Where((ItemType i) => i.IsMedical()).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Usables { get; } = AllValidItems.Where((ItemType i) => i.IsUsable()).ToList().AsReadOnly();


	public static IReadOnlyList<ItemType> Scps { get; } = AllValidItems.Where((ItemType i) => i.IsScp()).ToList().AsReadOnly();


	public static bool IsAmmo(this ItemType item)
	{
		if (item != ItemType.Ammo12gauge && item != ItemType.Ammo44cal && item != ItemType.Ammo556x45 && item != ItemType.Ammo762x39)
		{
			return item == ItemType.Ammo9x19;
		}
		return true;
	}

	public static bool IsFirearm(this ItemType item, bool countMicroHid = true, bool countJailbird = true)
	{
		if (item != ItemType.GunA7 && item != ItemType.GunAK && item != ItemType.GunCOM15 && item != ItemType.GunCOM18 && item != ItemType.GunCom45 && item != ItemType.GunCrossvec && item != ItemType.GunE11SR && item != ItemType.GunFRMG0 && item != ItemType.GunFSP9 && item != ItemType.GunLogicer && item != ItemType.GunRevolver && item != ItemType.GunShotgun && item != ItemType.ParticleDisruptor && (!countMicroHid || item != ItemType.MicroHID))
		{
			if (countJailbird)
			{
				return item == ItemType.Jailbird;
			}
			return false;
		}
		return true;
	}

	public static bool IsExplosive(this ItemType item)
	{
		if (item != ItemType.GrenadeFlash && item != ItemType.GrenadeHE)
		{
			return item == ItemType.SCP018;
		}
		return true;
	}

	public static bool IsArmor(this ItemType item)
	{
		if (item != ItemType.ArmorCombat && item != ItemType.ArmorHeavy)
		{
			return item == ItemType.ArmorLight;
		}
		return true;
	}

	public static bool IsKeycard(this ItemType item)
	{
		if (item != ItemType.KeycardChaosInsurgency && item != ItemType.KeycardContainmentEngineer && item != ItemType.KeycardFacilityManager && item != ItemType.KeycardGuard && item != 0 && item != ItemType.KeycardMTFCaptain && item != ItemType.KeycardMTFOperative && item != ItemType.KeycardMTFPrivate && item != ItemType.KeycardO5 && item != ItemType.KeycardResearchCoordinator && item != ItemType.KeycardScientist)
		{
			return item == ItemType.KeycardZoneManager;
		}
		return true;
	}

	public static bool IsMedical(this ItemType item)
	{
		if (item != ItemType.Medkit && item != ItemType.SCP500)
		{
			return item == ItemType.Painkillers;
		}
		return true;
	}

	public static bool IsUsable(this ItemType item)
	{
		if (item != ItemType.Adrenaline && item != ItemType.Medkit && item != ItemType.Painkillers && item != ItemType.SCP500 && item != ItemType.SCP1576 && item != ItemType.SCP1853 && item != ItemType.SCP207 && item != ItemType.SCP2176 && item != ItemType.SCP244a && item != ItemType.SCP244b && item != ItemType.SCP268 && item != ItemType.SCP330 && item != ItemType.SCP500)
		{
			return item == ItemType.AntiSCP207;
		}
		return true;
	}

	public static bool IsScp(this ItemType item)
	{
		if (item != ItemType.SCP018 && item != ItemType.SCP1576 && item != ItemType.SCP1853 && item != ItemType.SCP207 && item != ItemType.SCP2176 && item != ItemType.SCP244a && item != ItemType.SCP244b && item != ItemType.SCP268 && item != ItemType.SCP330 && item != ItemType.SCP500)
		{
			return item == ItemType.AntiSCP207;
		}
		return true;
	}

	public static ItemCategory GetCategory(this ItemType item)
	{
		if (item.IsExplosive())
		{
			return ItemCategory.Grenade;
		}
		if (item.IsAmmo())
		{
			return ItemCategory.Ammo;
		}
		if (item.IsMedical())
		{
			return ItemCategory.Medical;
		}
		if (item.IsArmor())
		{
			return ItemCategory.Armor;
		}
		if (item.IsFirearm())
		{
			return ItemCategory.Firearm;
		}
		if (item.IsKeycard())
		{
			return ItemCategory.Keycard;
		}
		if (item.IsScp())
		{
			return ItemCategory.SCPItem;
		}
		if (item == ItemType.Radio)
		{
			return ItemCategory.Radio;
		}
		return ItemCategory.None;
	}

	public static ItemType GetAmmoType(this ItemType firearmType)
	{
		switch (firearmType)
		{
		case ItemType.GunLogicer:
		case ItemType.GunAK:
		case ItemType.GunA7:
			return ItemType.Ammo762x39;
		case ItemType.GunCOM15:
		case ItemType.GunFSP9:
		case ItemType.GunCOM18:
		case ItemType.GunCom45:
			return ItemType.Ammo9x19;
		case ItemType.GunE11SR:
		case ItemType.GunFRMG0:
			return ItemType.Ammo556x45;
		case ItemType.GunRevolver:
			return ItemType.Ammo44cal;
		case ItemType.GunShotgun:
			return ItemType.Ammo12gauge;
		case ItemType.MicroHID:
			return ItemType.MicroHID;
		case ItemType.ParticleDisruptor:
			return ItemType.ParticleDisruptor;
		case ItemType.Jailbird:
			return ItemType.Jailbird;
		default:
			return ItemType.None;
		}
	}

	public static string GetName(this ItemType item)
	{
		if (1 == 0)
		{
		}
		string result = item switch
		{
			ItemType.Ammo12gauge => "12 gauge ammo", 
			ItemType.Ammo44cal => ".44 caliber ammo", 
			ItemType.Ammo556x45 => "5.56 x 45mm ammo", 
			ItemType.Ammo762x39 => "7.62 x 39mm ammo", 
			ItemType.Ammo9x19 => "9 x 19mm ammo", 
			ItemType.AntiSCP207 => "Anti-SCP-207", 
			ItemType.ArmorCombat => "Combat Armor", 
			ItemType.ArmorHeavy => "Heavy Armor", 
			ItemType.ArmorLight => "Light Armor", 
			ItemType.GrenadeFlash => "Flash Grenade", 
			ItemType.GrenadeHE => "Frag Grenade", 
			ItemType.GunA7 => "A7", 
			ItemType.GunAK => "AK", 
			ItemType.GunCOM15 => "COM-15", 
			ItemType.GunCOM18 => "COM-18", 
			ItemType.GunCom45 => "COM-45", 
			ItemType.GunCrossvec => "Crossvec", 
			ItemType.GunE11SR => "Epsilon E-11 SR", 
			ItemType.GunFRMG0 => "FR-MG-0", 
			ItemType.GunFSP9 => "FSP-9", 
			ItemType.GunLogicer => "Logicer", 
			ItemType.GunRevolver => ".44 Revolver", 
			ItemType.GunShotgun => "Shotgun", 
			ItemType.KeycardChaosInsurgency => "Chaos Insurgency Access Device", 
			ItemType.KeycardContainmentEngineer => "Containment Engineer Keycard", 
			ItemType.KeycardFacilityManager => "Facility Manager Keycard", 
			ItemType.KeycardGuard => "Facility Guard Keycard", 
			ItemType.KeycardJanitor => "Janitor Keycard", 
			ItemType.KeycardMTFCaptain => "MTF Captain Keycard", 
			ItemType.KeycardMTFOperative => "MTF Operative Keycard", 
			ItemType.KeycardMTFPrivate => "MTF Private Keycard", 
			ItemType.KeycardO5 => "O-5 Keycard", 
			ItemType.KeycardResearchCoordinator => "Research Coordinator Keycard", 
			ItemType.KeycardScientist => "Scientist Keycard", 
			ItemType.KeycardZoneManager => "Zone Manager Keycard", 
			ItemType.MicroHID => "Micro-H.I.D.", 
			ItemType.ParticleDisruptor => "3-X Particle Disruptor", 
			ItemType.SCP018 => "SCP-018", 
			ItemType.SCP1576 => "SCP-1576", 
			ItemType.SCP1853 => "SCP-1853", 
			ItemType.SCP207 => "SCP-207", 
			ItemType.SCP2176 => "SCP-2176", 
			ItemType.SCP244a => "SCP-244-A", 
			ItemType.SCP244b => "SCP-244-B", 
			ItemType.SCP268 => "SCP-268", 
			ItemType.SCP330 => "SCP-330", 
			ItemType.SCP500 => "SCP-500", 
			_ => item.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
