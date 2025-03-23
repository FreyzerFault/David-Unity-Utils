using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;
using RenderMode = DavidUtils.Rendering.PolygonRenderer.PolygonRenderMode;

namespace DavidUtils.Editor.Rendering.Editor.Rendering
{
    [CustomEditor(typeof(VoronoiRenderer), true)]
    public class VoronoiRendererEditor: DynamicRendererEditor
    {
        private bool _thicknessFoldout = true;
        
        public override void OnInspectorGUI()
        {
            var voronoiRenderer = (VoronoiRenderer) target;
            if (voronoiRenderer == null) return;
            
            RenderModeGUI(voronoiRenderer);
            
            EditorGUILayout.Separator();
            
            ColorGUI(voronoiRenderer);
            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            
            ThicknessGUI(voronoiRenderer);
        }
        
        private void RenderModeGUI(VoronoiRenderer renderer)
        {
            EditorGUI.BeginChangeCheck();
            RenderMode renderMode = (RenderMode) EditorGUILayout.EnumPopup("Render Mode", renderer.RenderMode);
            if (EditorGUI.EndChangeCheck()) renderer.RenderMode = renderMode;
        }
        
        private void ColorGUI(VoronoiRenderer renderer)
        {
            base.ColorGUI(renderer);
            
            if (!colorFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            Color outlineColor = EditorGUILayout.ColorField("Outline Color", renderer.OutlineColor);
            if (EditorGUI.EndChangeCheck()) renderer.OutlineColor = outlineColor;
            EditorGUI.indentLevel--;
        }
        
        private void ThicknessGUI(VoronoiRenderer renderer)
        {
            _thicknessFoldout = EditorGUILayout.Foldout(_thicknessFoldout, "THICKNESS", true, EditorStyles.foldoutHeader);
            if (!_thicknessFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            float thickness = EditorGUILayout.Slider("Thickness", renderer.Thickness, 0, 1);
            if (EditorGUI.EndChangeCheck()) renderer.Thickness = thickness;
            EditorGUI.indentLevel--;
        }
    }
}
