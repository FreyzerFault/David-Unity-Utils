using System;
using System.Linq;
using System.Reflection;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.Editor.DevTools.Logging;
using DavidUtils.Editor.DevTools.Serialization;
using DavidUtils.Utils;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DavidUtils.Editor.DevTools.CustomAttributes
{
    public static class ConditionalUtility
    {
        public static bool IsConditionMatch(Object owner, ConditionalData condition)
		{
			if (!condition.IsSet) return true;

			SerializedObject so = new(owner);
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
			$"Conditional Attribute is trying to check field {field.Colored(DebugColor.Brown)} which is not present",
			property.serializedObject.targetObject
		);

		private static void LogFieldNotFound(Object owner, string field) => WarningsPool.LogWarning(
			owner,
			$"Conditional Attribute is trying to check field {field.Colored(DebugColor.Brown)} which is not present",
			owner
		);

		public static void LogMethodNotFound(Object owner, string method) => WarningsPool.LogWarning(
			owner,
			$"Conditional Attribute is trying to invoke method {method.Colored(DebugColor.Brown)} " +
			"which is missing or not with a bool return type",
			owner
		);

		private static bool IsConditionMatch(SerializedProperty property, bool inverse, string[] compareAgainst)
		{
			if (property == null) return true;

			string asString = property.AsStringValue().ToUpper();

			if (compareAgainst is { Length: > 0 })
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
				return value?.GetType().GetCustomAttribute<FlagsAttribute>() != null;
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

			switch (propertyValueAsString)
			{
				//Handle Everything
				case "-1":
					return true;
				//Handle Nothing
				case "0":
					return false;
			}

			string[] separateFlags = propertyValueAsString.Split(',');
			return separateFlags.Any(flag => ValueMatches(flag.Trim()));


			bool ValueMatches(string value) => 
				compareAgainst.Any(compare => value == compare);
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
				string cleanPath = path[..path.IndexOf('[')];
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
						element[element.IndexOf("[", StringComparison.Ordinal)..]
							.Replace("[", "")
							.Replace("]", "")
					);
					element = element[..element.IndexOf("[", StringComparison.Ordinal)];
				}

				parent = i == 0
					? property.serializedObject.FindProperty(element)
					: parent?.FindPropertyRelative(element);

				if (index >= 0 && parent != null) parent = parent.GetArrayElementAtIndex(index);
			}

			return parent;
		}
		

		///     Call and check Method Condition, if any
		public static bool IsMethodConditionMatch(this ConditionalData data, object owner)
		{
			if (data.predicateMethod.IsNullOrEmpty()) return true;

			MethodInfo ownerPredicateMethod = GetMethodCondition(owner, data.predicateMethod, ref data.initializedMethodInfo, ref data.cachedMethodInfo);
			if (ownerPredicateMethod == null) return true;

			var match = (bool)ownerPredicateMethod.Invoke(owner, null);
			if (data.inverse) match = !match;
			return match;
		}

		///    Get the Method Info to check the condition
		private static MethodInfo GetMethodCondition(object owner, string predicateMethod, ref bool initializedMethodInfo, ref MethodInfo cachedMethodInfo)
		{
			if (predicateMethod.IsNullOrEmpty()) return null;
			if (initializedMethodInfo) return cachedMethodInfo;
			initializedMethodInfo = true;

			Type ownerType = owner.GetType();
			const BindingFlags bindings = 
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			
			MethodInfo method = ownerType.GetMethods(bindings).SingleOrDefault(m => m.Name == predicateMethod);

			if (method == null || method.ReturnType != typeof(bool))
			{
				LogMethodNotFound((Object)owner, predicateMethod);
				cachedMethodInfo = null;
			}
			else
				cachedMethodInfo = method;

			return cachedMethodInfo;
		}
    }
}
