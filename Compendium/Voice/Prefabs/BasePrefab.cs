using System;
using PlayerRoles;

namespace Compendium.Voice.Prefabs;

public class BasePrefab : IVoicePrefab
{
	private RoleTypeId[] _roles;

	public RoleTypeId[] Roles => _roles;

	public virtual Type Type { get; }

	public BasePrefab(params RoleTypeId[] roles)
	{
		_roles = roles;
	}

	public virtual IVoiceProfile Instantiate(ReferenceHub owner)
	{
		return null;
	}
}
