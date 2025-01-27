using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compendium.Comparison;
using helpers;
using helpers.Extensions;

namespace Compendium.Attributes;

public static class AttributeRegistry<TAttribute> where TAttribute : Attribute
{
	private static readonly List<AttributeData<TAttribute>> _list = new List<AttributeData<TAttribute>>();

	public static IReadOnlyList<AttributeData<TAttribute>> Attributes { get; private set; } = _list.AsReadOnly();


	public static Func<Type, MemberInfo, TAttribute, object[]> DataGenerator { get; set; }

	public static void ForEachOfCondition(Func<object[], AttributeData<TAttribute>, bool> predicate, Action<AttributeData<TAttribute>> action, params object[] data)
	{
		for (int i = 0; i < _list.Count; i++)
		{
			AttributeData<TAttribute> attributeData = _list[i];
			if (predicate(data, attributeData))
			{
				action(attributeData);
			}
		}
	}

	public static void ForEach(Action<AttributeData<TAttribute>> action)
	{
		for (int i = 0; i < _list.Count; i++)
		{
			action(_list[i]);
		}
	}

	public static void Register()
	{
		Register(Assembly.GetCallingAssembly());
	}

	public static void Register(Assembly assembly)
	{
		assembly.ForEachType(delegate(Type t)
		{
			Register(t, null);
		});
	}

	public static void Register(Type type, object handle)
	{
		if (type.TryGetAttribute<TAttribute>(out var attributeValue) && !TryGetAttribute(type, out var _))
		{
			AttributeData<TAttribute> item = new AttributeData<TAttribute>(type, attributeValue, GenerateData(type, null, attributeValue));
			_list.Add(item);
			AttributeRegistryEvents.FireAdded(attributeValue, type, null, handle);
		}
		type.ForEachField(delegate(FieldInfo f)
		{
			Register(f, handle);
		});
		type.ForEachMethod(delegate(MethodInfo m)
		{
			Register(m, handle);
		});
		type.ForEachProperty(delegate(PropertyInfo p)
		{
			Register(p, handle);
		});
		Attributes = _list.AsReadOnly();
	}

	public static void Register(MemberInfo member, object handle)
	{
		if (member.TryGetAttribute<TAttribute>(out var attributeValue) && !TryGetAttribute(member, handle, out var _))
		{
			AttributeData<TAttribute> item = new AttributeData<TAttribute>(member, member.DeclaringType, attributeValue, handle, GenerateData(member.DeclaringType, member, attributeValue));
			_list.Add(item);
			AttributeRegistryEvents.FireAdded(attributeValue, member.DeclaringType, member, handle);
		}
	}

	public static void Unregister()
	{
		Unregister(Assembly.GetCallingAssembly());
	}

	public static void Unregister(Assembly assembly)
	{
		assembly.ForEachType(delegate(Type t)
		{
			Unregister(t, null);
		});
	}

	public static void Unregister(Type type, object handle)
	{
		IEnumerable<AttributeData<TAttribute>> enumerable = _list.Where((AttributeData<TAttribute> x) => !x.IsMember && x.Type == type && NullableObjectComparison.Compare(x.MemberHandle, handle));
		if (enumerable.Count() <= 0)
		{
			return;
		}
		enumerable.ForEach(delegate(AttributeData<TAttribute> attr)
		{
			if (_list.Remove(attr))
			{
				AttributeRegistryEvents.FireRemoved(attr.Attribute, attr.Type, attr.Member, attr.MemberHandle);
			}
		});
		type.ForEachField(delegate(FieldInfo f)
		{
			Unregister(f, handle);
		});
		type.ForEachMethod(delegate(MethodInfo m)
		{
			Unregister(m, handle);
		});
		type.ForEachProperty(delegate(PropertyInfo p)
		{
			Unregister(p, handle);
		});
		Attributes = _list.AsReadOnly();
	}

	public static void Unregister(MemberInfo member, object handle)
	{
		IEnumerable<AttributeData<TAttribute>> enumerable = _list.Where((AttributeData<TAttribute> x) => x.IsMember && x.Member == member && NullableObjectComparison.Compare(x.MemberHandle, handle));
		if (enumerable.Count() <= 0)
		{
			return;
		}
		enumerable.ForEach(delegate(AttributeData<TAttribute> attr)
		{
			if (_list.Remove(attr))
			{
				AttributeRegistryEvents.FireRemoved(attr.Attribute, attr.Type, attr.Member, attr.MemberHandle);
			}
		});
		Attributes = _list.AsReadOnly();
	}

	public static bool TryGetAttribute(MemberInfo member, object handle, out TAttribute attribute)
	{
		if (_list.TryGetFirst((AttributeData<TAttribute> a) => a.IsMember && a.Member == member && NullableObjectComparison.Compare(handle, a.MemberHandle), out var value))
		{
			attribute = value.Attribute;
			return true;
		}
		attribute = null;
		return false;
	}

	public static bool TryGetAttribute(Type type, out TAttribute attribute)
	{
		if (_list.TryGetFirst((AttributeData<TAttribute> a) => !a.IsMember && a.Type == type, out var value))
		{
			attribute = value.Attribute;
			return true;
		}
		attribute = null;
		return false;
	}

	private static object[] GenerateData(Type type, MemberInfo member, TAttribute attribute)
	{
		if (DataGenerator == null)
		{
			return null;
		}
		return DataGenerator(type, member, attribute);
	}
}
