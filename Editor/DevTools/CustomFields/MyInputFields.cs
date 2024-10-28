using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.DevTools.CustomFields
{
    public static class MyInputFields
    {
        public static bool UndoRedoPerformed => Event.current.commandName == "UndoRedoPerformed" &&
                                          Event.current.type == EventType.ExecuteCommand;
        
        public enum FieldOptions
        {
            ToggleLeft
        }
        
        public static void InputField_Multiple<T>(SerializedObject sObj, string propName, string label,
            Action<T> onChanged = null, params FieldOptions[] options) =>
            InputField(sObj, propName, label, () => sObj.targetObjects.Cast<T>().ForEach(onChanged), options);

        public static void InputField(SerializedObject sObj, string propName, string label,
            Action onChanged = null, params FieldOptions[] options)
        {
            SerializedProperty prop = sObj.FindProperty(propName);
            if (prop == null) Debug.LogWarning($"Input Field {propName} in Object {sObj.targetObject.name} can't be found as Property");
            InputField(prop, label, onChanged, options);
        }

        public static void InputField(SerializedProperty prop, string label, Action onChanged = null, 
            params FieldOptions[] options)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUILayout.IntField(label, prop.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = options.Contains(FieldOptions.ToggleLeft)
                        ? EditorGUILayout.ToggleLeft(label, prop.boolValue)
                        : EditorGUILayout.Toggle(label, prop.boolValue);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUILayout.FloatField(label, prop.floatValue);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = EditorGUILayout.TextField(label, prop.stringValue);
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = EditorGUILayout.ColorField(label, prop.colorValue);
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = EditorGUILayout.ObjectField(label, prop.objectReferenceValue, prop.objectReferenceValue?.GetType(), true);
                    break;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = EditorGUILayout.Popup(label, prop.enumValueIndex, prop.enumDisplayNames);
                    break;
                case SerializedPropertyType.Vector2:
                    break;
                case SerializedPropertyType.Vector3:
                    break;
                case SerializedPropertyType.Vector4:
                    break;
                case SerializedPropertyType.Rect:
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    break;
                case SerializedPropertyType.ExposedReference:
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    break;
                case SerializedPropertyType.Vector3Int:
                    break;
                case SerializedPropertyType.RectInt:
                    break;
                case SerializedPropertyType.BoundsInt:
                    break;
                case SerializedPropertyType.ManagedReference:
                    break;
                case SerializedPropertyType.Hash128:
                    break;
                case SerializedPropertyType.RenderingLayerMask:
                    break;
                default:
                    EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
                    break;
            }
            
            if (onChanged == null) return;

            if (prop.serializedObject.ApplyModifiedProperties())
                onChanged();
        }


        #region POSITIONED FIELDS

        public static void InputField_Positioned_Multiple<T>(Rect rect, SerializedObject sObj, string propName, string label,
            Action<T> onChanged = null, params FieldOptions[] options) =>
            InputField_Positioned(rect, sObj, propName, label, () => sObj.targetObjects.Cast<T>().ForEach(onChanged), options);

        public static void InputField_Positioned(Rect rect, SerializedObject sObj, string propName, string label,
            Action onChanged = null, params FieldOptions[] options)
        {
            SerializedProperty prop = sObj.FindProperty(propName);
            if (prop == null) Debug.LogWarning($"Input Field {propName} in Object {sObj.targetObject.name} can't be found as Property");
            InputField_Positioned(rect, prop, label, onChanged, options);
        }
        
        public static void InputField_Positioned(Rect rect, SerializedProperty prop, string label,
            Action onChanged = null, params FieldOptions[] options)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUI.IntField(rect, label, prop.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = options.Contains(FieldOptions.ToggleLeft)
                        ? EditorGUI.ToggleLeft(rect, label, prop.boolValue)
                        : EditorGUI.Toggle(rect, label, prop.boolValue);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUI.FloatField(rect, label, prop.floatValue);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = EditorGUI.TextField(rect, label, prop.stringValue);
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = EditorGUI.ColorField(rect, label, prop.colorValue);
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = EditorGUI.ObjectField(rect, label, prop.objectReferenceValue, prop.objectReferenceValue?.GetType(), true);
                    break;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = EditorGUI.Popup(rect, label, prop.enumValueIndex, prop.enumDisplayNames);
                    break;
                case SerializedPropertyType.Vector2:
                    break;
                case SerializedPropertyType.Vector3:
                    break;
                case SerializedPropertyType.Vector4:
                    break;
                case SerializedPropertyType.Rect:
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    break;
                case SerializedPropertyType.ExposedReference:
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    break;
                case SerializedPropertyType.Vector3Int:
                    break;
                case SerializedPropertyType.RectInt:
                    break;
                case SerializedPropertyType.BoundsInt:
                    break;
                case SerializedPropertyType.ManagedReference:
                    break;
                case SerializedPropertyType.Hash128:
                    break;
                case SerializedPropertyType.RenderingLayerMask:
                    break;
                default:
                    EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);
                    break;
            }
            
            if (prop.serializedObject.ApplyModifiedProperties())
                onChanged();
        }

        #endregion
    }
}
