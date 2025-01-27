using System;
using System.Net;

namespace Compendium.Guard;

public static class ServerGuardUtils
{
	public static bool IsInRange(string ipAddress, string CIDRmask)
	{
		try
		{
			string[] array = CIDRmask.Split(new char[1] { '/' });
			int num = BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
			int num2 = BitConverter.ToInt32(IPAddress.Parse(array[0]).GetAddressBytes(), 0);
			int num3 = IPAddress.HostToNetworkOrder(-1 << 32 - int.Parse(array[1]));
			return (num & num3) == (num2 & num3);
		}
		catch
		{
			return false;
		}
	}
}
