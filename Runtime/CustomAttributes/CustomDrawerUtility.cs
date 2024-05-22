#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DavidUtils.ExtensionMethods;
using MyBox;
using UnityEditor;

namespace DavidUtils.CustomAttributes
{
	public static class CustomDrawerUtility
	{
		/// <summary>
		///     Key is Associated with drawer type (the T in [CustomPropertyDrawer(typeof(T))])
		///     Value is PropertyDrawer Type
		/// </summary>
		private static readonly Dictionary<Type, Type> PropertyDrawersInAssembly = new();
		private static readonly Dictionary<int, PropertyDrawer> PropertyDrawersCache = new();
		private static readonly string IgnoreScope = typeof(int).Module.ScopeName;

		/// <summary>
		///     Create PropertyDrawer for specified property if any PropertyDrawerType for such property is found.
		///     FieldInfo and Attribute will be inserted in created drawer.
		/// </summary>
		public static PropertyDrawer GetPropertyDrawerForProperty(
			SerializedProperty property, FieldInfo fieldInfo, Attribute attribute
		)
		{
			int propertyId = property.GetUniquePropertyId();
			if (PropertyDrawersCache.TryGetValue(propertyId, out PropertyDrawer drawer)) return drawer;

			Type targetType = fieldInfo.FieldType;
			Type drawerType = GetPropertyDrawerTypeForFieldType(targetType);
			if (drawerType != null)
			{
				drawer = InstantiatePropertyDrawer(drawerType, fieldInfo, attribute);

				if (drawer == null)
					WarningsPool.LogWarning(
						property,
						$"Unable to instantiate CustomDrawer of type {drawerType} for {fieldInfo.FieldType}",
						property.serializedObject.targetObject
					);
			}

			PropertyDrawersCache[propertyId] = drawer;
			return drawer;
		}

		public static PropertyDrawer InstantiatePropertyDrawer(
			Type drawerType, FieldInfo fieldInfo, Attribute insertAttribute
		)
		{
			try
			{
				var drawerInstance = (PropertyDrawer)Activator.CreateInstance(drawerType);

				// Reassign the attribute and fieldInfo fields in the drawer so it can access the argument values
				FieldInfo fieldInfoField = drawerType.GetField(
					"m_FieldInfo",
					BindingFlags.Instance | BindingFlags.NonPublic
				);
				if (fieldInfoField != null) fieldInfoField.SetValue(drawerInstance, fieldInfo);
				FieldInfo attributeField = drawerType.GetField(
					"m_Attribute",
					BindingFlags.Instance | BindingFlags.NonPublic
				);
				if (attributeField != null) attributeField.SetValue(drawerInstance, insertAttribute);

				return drawerInstance;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		///     Try to get PropertyDrawer for a target Type, or any Base Type for a target Type
		/// </summary>
		public static Type GetPropertyDrawerTypeForFieldType(Type drawerTarget)
		{
			// Ignore .net types from mscorlib.dll
			if (drawerTarget.Module.ScopeName.Equals(IgnoreScope)) return null;
			CacheDrawersInAssembly();

			// Of all property drawers in the assembly we need to find one that affects target type
			// or one of the base types of target type
			Type checkType = drawerTarget;
			while (checkType != null)
			{
				if (PropertyDrawersInAssembly.TryGetValue(drawerTarget, out Type drawer)) return drawer;
				checkType = checkType.BaseType;
			}

			return null;
		}

		private static Type[] GetTypesSafe(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types;
			}
		}

		private static void CacheDrawersInAssembly()
		{
			if (PropertyDrawersInAssembly.NotNullOrEmpty()) return;

			Type propertyDrawerType = typeof(PropertyDrawer);
			IEnumerable<Type> allDrawerTypesInDomain = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(GetTypesSafe)
				.Where(t => t != null && propertyDrawerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

			foreach (Type drawerType in allDrawerTypesInDomain)
			{
				CustomAttributeData propertyDrawerAttribute =
					CustomAttributeData.GetCustomAttributes(drawerType).FirstOrDefault();
				if (propertyDrawerAttribute == null) continue;
				var drawerTargetType = propertyDrawerAttribute.ConstructorArguments.FirstOrDefault().Value as Type;
				if (drawerTargetType == null) continue;

				if (PropertyDrawersInAssembly.ContainsKey(drawerTargetType)) continue;
				PropertyDrawersInAssembly.Add(drawerTargetType, drawerType);
			}
		}
	}
}
#endif
