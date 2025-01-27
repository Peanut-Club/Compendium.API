using System.Collections;
using System.Collections.Generic;

namespace Compendium.IO.Saving;

public class CollectionSaveData<TElement> : SimpleSaveData<List<TElement>>, IList<TElement>, ICollection<TElement>, IEnumerable<TElement>, IEnumerable
{
	public TElement this[int index]
	{
		get
		{
			return base.Value[index];
		}
		set
		{
			base.Value[index] = value;
		}
	}

	public int Count => base.Value.Count;

	public bool IsReadOnly => false;

	public CollectionSaveData()
	{
		base.Value = new List<TElement>();
	}

	public void Add(TElement item)
	{
		base.Value.Add(item);
	}

	public void Clear()
	{
		base.Value.Clear();
	}

	public bool Contains(TElement item)
	{
		return base.Value.Contains(item);
	}

	public void CopyTo(TElement[] array, int arrayIndex)
	{
		base.Value.CopyTo(array, arrayIndex);
	}

	public IEnumerator<TElement> GetEnumerator()
	{
		return base.Value.GetEnumerator();
	}

	public int IndexOf(TElement item)
	{
		return base.Value.IndexOf(item);
	}

	public void Insert(int index, TElement item)
	{
		base.Value.Insert(index, item);
	}

	public bool Remove(TElement item)
	{
		return base.Value.Remove(item);
	}

	public void RemoveAt(int index)
	{
		base.Value.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return base.Value.GetEnumerator();
	}
}
