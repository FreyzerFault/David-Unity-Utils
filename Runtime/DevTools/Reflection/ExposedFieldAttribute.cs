using System;
using System.Linq;
using System.Reflection;

namespace DavidUtils.DevTools.Reflection
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ExposedFieldAttribute : Attribute
	{
	}

	public static class ExposedFieldMethods
	{
		public static FieldInfo[] GetExposedFields(object target) => target.GetType()
			.GetFields()
			.Where(f => f.GetCustomAttribute<ExposedFieldAttribute>() != null)
			.ToArray();
	}
}
