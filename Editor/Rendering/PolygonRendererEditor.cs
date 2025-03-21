using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;
using RenderMode = DavidUtils.Rendering.PolygonRenderer.PolygonRenderMode;

namespace DavidUtils.Editor.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PolygonRenderer), true)]
    public class PolygonRendererEditor: UnityEditor.Editor
    {
        private PolygonRenderer _polyRenderer;

        public override void OnInspectorGUI()
        {
            _polyRenderer = (PolygonRenderer) target;
            if (_polyRenderer == null) return;
            
            if (serializedObject.isEditingMultipleObjects)
            {
                MultipleGUI();
                return;
            }
            
            EditorGUILayout.Separator();
            
            PropertyField(
                "polygon",
                $"Polygon ({_polyRenderer.VertexCount} vertices)",
                r => r.UpdatePolygon()
            );
            
            EditorGUILayout.Separator();
            
            RenderingPropFields();
            
            EditorGUILayout.Separator();
            
            if (_polyRenderer.RenderMode is RenderMode.OutlinedMesh or RenderMode.Mesh) 
                SubPolygonPropFields();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void MultipleGUI()
        {
            EditorGUILayout.Separator();

            RenderingPropFields();
            
            EditorGUILayout.Separator();
            
            if (serializedObject.targetObjects.Cast<PolygonRenderer>().All(pr => pr.RenderMode is RenderMode.OutlinedMesh or RenderMode.Mesh)) 
                SubPolygonPropFields();
        }

        private void RenderingPropFields()
        {
            PropertyField("renderMode", $"Render Mode", (r) => r.UpdateRenderMode());
            
            switch (((PolygonRenderer)serializedObject.targetObject).RenderMode)
            {
                case RenderMode.Wire:
                    PropertyField("outlineColor", $"Outline", (r) => r.UpdateColor());
                    PropertyField("thickness", $"Line Thickness", (r) => r.UpdateThickness());
                    TerrainPropFields();
                    break;
                case RenderMode.Mesh:
                    PropertyField("color", $"Fill", (r) => r.UpdateColor());
                    break;
                case RenderMode.OutlinedMesh:
                    PropertyField("color", $"Fill",(r) => r.UpdateColor());
                    PropertyField("outlineColor", $"Outline", (r) => r.UpdateColor());
                    PropertyField("thickness", $"Line Thickness", (r) => r.UpdateThickness());
                    break;
            }

            EditorGUILayout.Separator();
            PropertyField("centeredScale", $"Scale", r => r.UpdatePolygon());
        }


        private void TerrainPropFields()
        {
            if (Terrain.activeTerrain == null) return;
            
            PropertyField("projectedOnTerrain", $"Project On Terrain", r => r.UpdateTerrainProjection());

            if (serializedObject.targetObjects.Cast<PolygonRenderer>().All(pr => pr.ProjectedOnTerrain))
                PropertyField("terrainHeightOffset", $"Height Offset", r => r.UpdateHeightTerrainOffset());
        }


        private bool _subPolygonFoldout = true;
        private void SubPolygonPropFields()
        {
            _subPolygonFoldout = EditorGUILayout.Foldout(_subPolygonFoldout, "SubPolygons", true);
            if (!_subPolygonFoldout) return;

            EditorGUI.indentLevel++;
            
            PropertyField("generateSubPolygons", "Generate SubPolygons",
                r => { r.UpdatePolygon(); r.UpdateSubPolygonRenderers(); });
            
            
            if (!serializedObject.targetObjects.Cast<PolygonRenderer>().All(pr => pr.generateSubPolygons)) return;
            
            // Generate on Coroutine Toggle
            PropertyField("generateSubsOnCoroutine", "Do it on Coroutine");
            
            // Max subpolygons that can be generated
            PropertyField("maxSubPolygonCount", "Max SubPolygons", r => r.UpdatePolygon());
            
            // How many Subpolygons per frame
            if (_polyRenderer.generateSubsOnCoroutine)
            {
                PropertyField("maxSubPolygonsPerFrame", $"Subpolys Per Frame");
                PropertyField("delayinSeconds_SubpolygonCoroutine", $"Seconds Delay");
            }
            
            // Show Sub Polygons with different colors
            PropertyField("showSubPolygons", "Show SubPolygons", r => r.UpdateSubPolygonRenderers());
            
            // Info of Subpolygon Segmentation
            if (!serializedObject.isEditingMultipleObjects && _polyRenderer.ShowSubPolygons)
                EditorGUILayout.LabelField(
                    $"Polygon has been segmented in {_polyRenderer.SubPolygonCount} polygons");
            
            EditorGUI.indentLevel--;
        }
        
        
        private void PropertyField(string propName, string label, bool includeChildren = true) =>
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName), new GUIContent(label), includeChildren);
        
        private bool PropertyField(string propName, string label, Action<PolygonRenderer> onChange, bool includeChildren = true)
        {
            PropertyField(propName, label, includeChildren);
            if (!serializedObject.ApplyModifiedProperties()) return false;
            
            serializedObject.targetObjects.Cast<PolygonRenderer>().ForEach(onChange);
            return true;
        }
    }
}
