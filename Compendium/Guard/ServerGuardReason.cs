namespace Compendium.Guard;

public enum ServerGuardReason
{
	None,
	Ignore,
	ProxyNetwork,
	BlockedAsn,
	BlockedCidr,
	PrivateAccount,
	NotSetupAccount,
	AccountAge
}
