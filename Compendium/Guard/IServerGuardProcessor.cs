using System;

namespace Compendium.Guard;

public interface IServerGuardProcessor
{
	void Process(ReferenceHub player, Action<ServerGuardReason> callback);
}
