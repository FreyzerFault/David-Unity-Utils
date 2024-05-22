#if UNITY_EDITOR
using System;
using System.Reflection;
using DavidUtils.ExtensionMethods;
using MyBox;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DavidUtils.CustomAttributes
{
	public static class ConditionalUtility
	{
		public static bool IsConditionMatch(Object owner, ConditionalData condition)
		{
			if (!condition.IsSet) return true;

			var so = new SerializedObject(owner);
			foreach ((string Field, bool Inverse, string[] CompareAgainst) fieldCondition in condition)
			{
				if (fieldCondition.Field.IsNullOrEmpty()) continue;

				SerializedProperty property = so.FindProperty(fieldCondition.Field);
				if (property == null) LogFieldNotFound(so.targetObject, fieldCondition.Field);

				bool passed = IsConditionMatch(property, fieldCondition.Inverse, fieldCondition.CompareAgainst);
				if (!passed) return false;
			}

			return condition.IsMethodConditionMatch(owner);
		}

		public static bool IsPropertyConditionMatch(SerializedProperty property, ConditionalData condition)
		{
			if (!condition.IsSet) return true;

			foreach ((string Field, bool Inverse, string[] CompareAgainst) fieldCondition in condition)
			{
				SerializedProperty relativeProperty = FindRelativeProperty(property, fieldCondition.Field);
				if (relativeProperty == null) LogFieldNotFound(property, fieldCondition.Field);

				bool passed = IsConditionMatch(relativeProperty, fieldCondition.Inverse, fieldCondition.CompareAgainst);
				if (!passed) return false;
			}

			return condition.IsMethodConditionMatch(property.GetParent());
		}

		private static void LogFieldNotFound(SerializedProperty property, string field) => WarningsPool.LogWarning(
			property,
			$"Conditional Attribute is trying to check field {field.Colored(Colors.brown)} which is not present",
			property.serializedObject.targetObject
		);

		private static void LogFieldNotFound(Object owner, string field) => WarningsPool.LogWarning(
			owner,
			$"Conditional Attribute is trying to check field {field.Colored(Colors.brown)} which is not present",
			owner
		);

		public static void LogMethodNotFound(Object owner, string method) => WarningsPool.LogWarning(
			owner,
			$"Conditional Attribute is trying to invoke method {method.Colored(Colors.brown)} " +
			"which is missing or not with a bool return type",
			owner
		);

		private static bool IsConditionMatch(SerializedProperty property, bool inverse, string[] compareAgainst)
		{
			if (property == null) return true;

			string asString = property.AsStringValue().ToUpper();

			if (compareAgainst != null && compareAgainst.Length > 0)
			{
				bool matchAny = CompareAgainstValues(asString, compareAgainst, IsFlagsEnum());
				if (inverse) matchAny = !matchAny;
				return matchAny;
			}

			bool someValueAssigned = asString != "FALSE" && asString != "0" && asString != "NULL";
			if (someValueAssigned) return !inverse;

			return inverse;


			bool IsFlagsEnum()
			{
				if (property.propertyType != SerializedPropertyType.Enum) return false;
				object value = property.GetValue();
				if (value == null) return false;
				return value.GetType().GetCustomAttribute<FlagsAttribute>() != null;
			}
		}


		/// <summary>
		///     True if the property value matches any of the values in '_compareValues'
		/// </summary>
		private static bool CompareAgainstValues(
			string propertyValueAsString, string[] compareAgainst, bool handleFlags
		)
		{
			if (!handleFlags) return ValueMatches(propertyValueAsString);

			if (propertyValueAsString == "-1") //Handle Everything
				return true;
			if (propertyValueAsString == "0") //Handle Nothing
				return false;

			string[] separateFlags = propertyValueAsString.Split(',');
			foreach (string flag in separateFlags)
				if (ValueMatches(flag.Trim()))
					return true;

			return false;


			bool ValueMatches(string value)
			{
				foreach (string compare in compareAgainst)
					if (value == compare)
						return true;
				return false;
			}
		}

		/// <summary>
		///     Get the other Property which is stored alongside with specified Property, by name
		/// </summary>
		private static SerializedProperty FindRelativeProperty(SerializedProperty property, string propertyName)
		{
			if (property.depth == 0) return property.serializedObject.FindProperty(propertyName);

			string path = property.propertyPath.Replace(".Array.data[", "[");
			string[] elements = path.Split('.');

			SerializedProperty nestedProperty = NestedPropertyOrigin(property, elements);

			// if nested property is null = we hit an array property
			if (nestedProperty == null)
			{
				string cleanPath = path.Substring(0, path.IndexOf('['));
				SerializedProperty arrayProp = property.serializedObject.FindProperty(cleanPath);
				WarningsPool.LogCollectionsNotSupportedWarning(arrayProp, nameof(ConditionalFieldAttribute));

				return null;
			}

			return nestedProperty.FindPropertyRelative(propertyName);
		}

		// For [Serialized] types with [Conditional] fields
		private static SerializedProperty NestedPropertyOrigin(SerializedProperty property, string[] elements)
		{
			SerializedProperty parent = null;

			for (var i = 0; i < elements.Length - 1; i++)
			{
				string element = elements[i];
				int index = -1;
				if (element.Contains("["))
				{
					index = Convert.ToInt32(
						element.Substring(element.IndexOf("[", StringComparison.Ordinal))
							.Replace("[", "")
							.Replace("]", "")
					);
					element = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
				}

				parent = i == 0
					? property.serializedObject.FindProperty(element)
					: parent != null
						? parent.FindPropertyRelative(element)
						: null;

				if (index >= 0 && parent != null) parent = parent.GetArrayElementAtIndex(index);
			}

			return parent;
		}
	}
}
#endif
