using System;
using System.Reflection;

namespace Compendium.Attributes;

public static class AttributeRegistryEvents
{
	public static event Action<Attribute, Type, MemberInfo, object> OnAttributeAdded;

	public static event Action<Attribute, Type, MemberInfo, object> OnAttributeRemoved;

	internal static void FireAdded(Attribute attribute, Type type, MemberInfo member, object handle)
	{
		AttributeRegistryEvents.OnAttributeAdded?.Invoke(attribute, type, member, handle);
	}

	internal static void FireRemoved(Attribute attribute, Type type, MemberInfo member, object handle)
	{
		AttributeRegistryEvents.OnAttributeRemoved?.Invoke(attribute, type, member, handle);
	}
}
