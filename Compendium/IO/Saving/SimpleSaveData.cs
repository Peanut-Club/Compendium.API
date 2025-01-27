using System.IO;
using helpers.Network.Extensions.Data;

namespace Compendium.IO.Saving;

public class SimpleSaveData<TValue> : SaveData
{
	public TValue Value { get; set; }

	public override bool IsBinary => true;

	public override void Read(BinaryReader reader)
	{
		base.Read(reader);
		object obj = reader.ReadObject();
		if (obj == null)
		{
			Value = default(TValue);
			return;
		}
		if (!(obj is TValue value) || 1 == 0)
		{
			throw new InvalidDataException("Object type '" + obj.GetType().FullName + "' cannot be converted to " + typeof(TValue).FullName);
		}
		Value = value;
	}

	public override void Write(BinaryWriter writer)
	{
		base.Write(writer);
		writer.WriteObject(Value);
	}
}
