using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.DevTools.Reflection
{
	// Use Reflection to EXPOSE Fields, Props, methods... from any class
	// Useful to get values from inspector in a Declarative way
	[Serializable]
	public class AttributesExposer
	{
		public Object targetObj;
		public MemberInfo[] targetFieldOptions = Array.Empty<MemberInfo>();
		[HideInInspector] public int fieldIndex;
		public bool onlyExposed = true;
		public bool inherited = true;

		public string label;

		public Action<MemberInfo> OnFieldSelected;

		public MemberInfo SelectedField => HasFields && fieldIndex < targetFieldOptions.Length
			? targetFieldOptions[fieldIndex]
			: null;

		public bool HasFields => targetFieldOptions is { Length: > 0 };

		public AttributesExposer(string label = "Expose") => this.label = label;

		public AttributesExposer(MonoBehaviour target, string label = "Expose") : this(label)
		{
			targetObj = target;
			LoadExposedFields(target);
		}

		public void SelectField(int i = 0)
		{
			fieldIndex = i;
			OnFieldSelected?.Invoke(SelectedField);
		}

		public void LoadExposedFields(Object target = null)
		{
			if (target == null && targetObj == null) return;

			bool newTarget = target != null && target != targetObj;
			if (newTarget) targetObj = target;

			targetFieldOptions = GetFieldsAndProps().ToArray();
			if (onlyExposed) targetFieldOptions = FilterExposed(targetFieldOptions).ToArray();

			// Autoselect first field if no field selected or target is NEW
			if (newTarget || SelectedField == null) SelectField();
		}

		public static IEnumerable<MemberInfo> GetMembers(object target)
			=> target.GetType()
				.GetMembers();

		public IEnumerable<MemberInfo> GetFieldsAndProps() =>
			GetFields().Cast<MemberInfo>().Concat(GetProperties());

		public IEnumerable<FieldInfo> GetFields()
		{
			Type targetType = targetObj.GetType();

			List<FieldInfo> fields = new();

			while (targetType != null)
			{
				fields.AddRange(
					targetType
						.GetFields()
						.Where(p => !p.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any())
						.ToArray()
				);
				targetType = inherited ? targetType.BaseType : null;
			}

			return fields;
		}

		public IEnumerable<PropertyInfo> GetProperties()
		{
			Type targetType = targetObj.GetType();

			List<PropertyInfo> props = new();

			while (targetType != null)
			{
				props.AddRange(
					targetType
						.GetProperties()
						.Where(p => !p.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any())
						.ToArray()
				);
				targetType = inherited ? targetType.BaseType : null;
			}

			return props;
		}

		public static IEnumerable<MemberInfo> FilterExposed(IEnumerable<MemberInfo> members)
			=> members.Where(member => member.GetCustomAttribute<ExposedFieldAttribute>() != null);


		#region VALUE

		public string StringValue => GetStringValue(SelectedField);
		public int IntValue => GetIntValue(SelectedField);
		public float FloatValue => GetFloatValue(SelectedField);
		public bool BoolValue => GetBoolValue(SelectedField);
		public object ObjectValue => GetValue(SelectedField);

		public string GetStringValue(MemberInfo info) => info switch
		{
			FieldInfo field => field.GetValue(targetObj).ToString(),
			PropertyInfo prop => prop.GetValue(targetObj).ToString(),
			_ => null
		};

		public int GetIntValue(MemberInfo info) => info switch
		{
			FieldInfo field => (int)field.GetValue(targetObj),
			PropertyInfo prop => (int)prop.GetValue(targetObj),
			_ => -1
		};

		public float GetFloatValue(MemberInfo info) => info switch
		{
			FieldInfo field => (float)field.GetValue(targetObj),
			PropertyInfo prop => (float)prop.GetValue(targetObj),
			_ => -1
		};

		public bool GetBoolValue(MemberInfo info) => info switch
		{
			FieldInfo field => (bool)field.GetValue(targetObj),
			PropertyInfo prop => (bool)prop.GetValue(targetObj),
			_ => false
		};

		public object GetValue(MemberInfo info) => info switch
		{
			FieldInfo field => field.GetValue(targetObj),
			PropertyInfo prop => prop.GetValue(targetObj),
			_ => false
		};

		#endregion
	}
}
