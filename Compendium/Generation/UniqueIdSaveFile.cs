using System.Collections.Generic;
using System.IO;
using Compendium.IO.Saving;

namespace Compendium.Generation;

public class UniqueIdSaveFile : SaveData
{
	public List<string> IDs { get; } = new List<string>();


	public override bool IsBinary => false;

	public override void Read(StreamReader reader)
	{
		IDs.Clear();
		base.Read(reader);
		string text = null;
		while ((text = reader.ReadLine()) != null)
		{
			IDs.Add(text);
		}
	}

	public override void Write(StreamWriter writer)
	{
		base.Write(writer);
		foreach (string iD in IDs)
		{
			writer.WriteLine(iD);
		}
	}
}
