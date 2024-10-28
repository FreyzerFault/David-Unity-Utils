using System;
using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;
using Fields = DavidUtils.Editor.DevTools.CustomFields.MyInputFields;

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
                    Fields.InputField(serializedObject.FindProperty("spherePrefab"), "Sphere Prefab", () => _renderer.UpdateRenderMode());
                    break;
                case PointsRenderer.RenderMode.Circle:
                    Fields.InputField(serializedObject.FindProperty("circlePrefab"), "Circle Prefab", () => _renderer.UpdateRenderMode());
                    break;
                case PointsRenderer.RenderMode.Point:
                    Fields.InputField(serializedObject.FindProperty("pointPrefab"), "Point Prefab", () => _renderer.UpdateRenderMode());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            EditorGUILayout.Separator();
            
            Fields.InputField(serializedObject.FindProperty("renderMode"), "Render Mode", () => _renderer.UpdateRenderMode());
            Fields.InputField(serializedObject.FindProperty("radius"), "Point Radius", () => _renderer.UpdateRadius());
            
            ColorGUI(_renderer);
            TestingGUI(_renderer);
            
            serializedObject.ApplyModifiedProperties();
        }


    }
}
