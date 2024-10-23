using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Rendering
{
    [CustomEditor(typeof(PointsRenderer))]
    public class PointsRendererEditor: DynamicRendererEditor
    {
        private PointsRenderer _renderer;
        
        private void OnEnable()
        {
            _renderer = target as PointsRenderer;
        }

        public override void OnInspectorGUI()
        {
            if (_renderer == null) return;

            switch (_renderer.Mode)
            {
                case PointsRenderer.RenderMode.Sphere:
                    InputField(serializedObject.FindProperty("spherePrefab"), "Sphere Prefab", _renderer.UpdateRenderMode, typeof(GameObject));
                    break;
                case PointsRenderer.RenderMode.Circle:
                    InputField(serializedObject.FindProperty("circlePrefab"), "Circle Prefab", _renderer.UpdateRenderMode, typeof(GameObject));
                    break;
                case PointsRenderer.RenderMode.Point:
                    InputField(serializedObject.FindProperty("pointPrefab"), "Point Prefab", _renderer.UpdateRenderMode, typeof(GameObject));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            EditorGUILayout.Separator();
            
            InputField(serializedObject.FindProperty("renderMode"), "Render Mode", _renderer.UpdateRenderMode);
            InputField(serializedObject.FindProperty("radius"), "Point Radius", _renderer.UpdateRadius);
            
            ColorGUI(_renderer);
            TestingGUI(_renderer);
            
            serializedObject.ApplyModifiedProperties();
        }


        public static void InputField_Multiple<T>(SerializedObject sObj, string propName, string label,
            Action<T> onChanged = null, Type type = null) =>
            InputField(sObj, propName, label, () => sObj.targetObjects.Cast<T>().ForEach(onChanged), type);

        public static void InputField(SerializedObject sObj, string propName, string label,
            Action onChanged = null, Type type = null)
        {
            SerializedProperty prop = sObj.FindProperty(propName);
            if (prop == null) Debug.LogWarning($"Input Field {propName} in Object {sObj.targetObject.name} can't be found as Property");
            InputField(prop, label, onChanged, type);
        }

        public static void InputField(SerializedProperty prop, string label, Action onChanged = null, Type type = null)
        {
            if (onChanged != null) EditorGUI.BeginChangeCheck();
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUILayout.IntField(label, prop.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = EditorGUILayout.Toggle(label, prop.boolValue);
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
                    prop.objectReferenceValue = EditorGUILayout.ObjectField(label, prop.objectReferenceValue, prop.objectReferenceValue?.GetType() ?? type, true);
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
            
            if (onChanged == null || !EditorGUI.EndChangeCheck()) return;
            
            prop.serializedObject.ApplyModifiedProperties();
            onChanged();
        }
    }
}
