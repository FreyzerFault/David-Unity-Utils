using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.Editor.DevTools.Logging;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.DevTools.CustomAttributes
{
    [CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
    public class ConditionalFieldAttributeDrawer: PropertyDrawer
    {
        private bool _toShow = true;
		private bool _initialized;
		private PropertyDrawer _customPropertyDrawer;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (attribute is not ConditionalFieldAttribute conditional) return EditorGUI.GetPropertyHeight(property);

			CachePropertyDrawer(property);
			_toShow = ConditionalUtility.IsPropertyConditionMatch(property, conditional.data);
			if (!_toShow) return -2;

			if (_customPropertyDrawer != null) return _customPropertyDrawer.GetPropertyHeight(property, label);
			return EditorGUI.GetPropertyHeight(property);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!_toShow) return;

			if (!CustomDrawerUsed()) EditorGUI.PropertyField(position, property, label, true);


			bool CustomDrawerUsed()
			{
				if (_customPropertyDrawer == null) return false;

				try
				{
					_customPropertyDrawer.OnGUI(position, property, label);
					return true;
				}
				catch (Exception e)
				{
					WarningsPool.LogWarning(
						property,
						"Unable to use CustomDrawer of type " + _customPropertyDrawer.GetType() + ": " + e,
						property.serializedObject.targetObject
					);

					return false;
				}
			}
		}

		/// <summary>
		///     Try to find and cache any PropertyDrawer or PropertyAttribute on the field
		/// </summary>
		private void CachePropertyDrawer(SerializedProperty property)
		{
			if (_initialized) return;
			_initialized = true;
			if (fieldInfo == null) return;

			PropertyDrawer customDrawer =
				CustomDrawerUtility.GetPropertyDrawerForProperty(property, fieldInfo, attribute) ??
				TryCreateAttributeDrawer();

			_customPropertyDrawer = customDrawer;


			// Try to get drawer for any other Attribute on the field
			PropertyDrawer TryCreateAttributeDrawer()
			{
				Attribute secondAttribute = TryGetSecondAttribute();
				if (secondAttribute == null) return null;

				Type attributeType = secondAttribute.GetType();
				Type customDrawerType = CustomDrawerUtility.GetPropertyDrawerTypeForFieldType(attributeType);
				return customDrawerType != null
					? CustomDrawerUtility.InstantiatePropertyDrawer(customDrawerType, fieldInfo, secondAttribute) 
					: null;


				//Get second attribute if any
				Attribute TryGetSecondAttribute() => (PropertyAttribute)fieldInfo
					.GetCustomAttributes(typeof(PropertyAttribute), false)
					.FirstOrDefault(a => a is not ConditionalFieldAttribute);
			}
		}
    }
}
