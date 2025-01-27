using System.Linq;

namespace Compendium.HttpServer.Authentification;

public class HttpAuthentificationKey
{
	public string[] Permits { get; set; }

	public string Id { get; set; }

	public bool IsPermitted(string endpointPerm)
	{
		if (Permits == null || !Permits.Any())
		{
			return false;
		}
		if (Permits.Contains<string>("*") || Permits.Contains<string>(endpointPerm))
		{
			return true;
		}
		if (!endpointPerm.Contains("."))
		{
			return false;
		}
		string[] array = endpointPerm.Split(new char[1] { '.' });
		for (int i = 0; i < array.Length; i++)
		{
			if (Permits.Contains<string>("*." + array[i]) || Permits.Contains<string>(array[i] + "."))
			{
				return true;
			}
		}
		return false;
	}
}
