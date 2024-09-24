using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Rendering
{
    [CustomEditor(typeof(PolygonRenderer))]
    public class PolygonRendererEditor: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUILayout.Separator();
            
            var polyRenderer = (PolygonRenderer) target;
            if (polyRenderer == null) return;
            
            polyRenderer.ShowSubPolygons = EditorGUILayout.Toggle("Show subPolygons", polyRenderer.ShowSubPolygons);
            if (polyRenderer.ShowSubPolygons) 
                EditorGUILayout.LabelField($"Polygon has been segmented in {polyRenderer.SubPolygonCount} polygons");
            
        }
    }
}
