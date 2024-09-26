using System;
using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;
using RenderMode = DavidUtils.Rendering.PolygonRenderer.PolygonRenderMode;

namespace DavidUtils.Editor.Rendering
{
    [CustomEditor(typeof(PolygonRenderer))]
    public class PolygonRendererEditor: UnityEditor.Editor
    {
        private PolygonRenderer _polyRenderer;

        public override void OnInspectorGUI()
        {
            _polyRenderer = (PolygonRenderer) target;
            if (_polyRenderer == null) return;
            
            EditorGUILayout.Separator();
            
            PropertyField(
                "polygon",
                $"Polygon ({_polyRenderer.VertexCount} vertices)",
                _polyRenderer.UpdatePolygon
            );
            
            EditorGUILayout.Separator();
            
            RenderingPropFields();
            
            EditorGUILayout.Separator();
            
            if (_polyRenderer.RenderMode is RenderMode.OutlinedMesh or RenderMode.Mesh) 
                SubPolygonPropFields();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void RenderingPropFields()
        {
            PropertyField("renderMode", $"Render Mode", _polyRenderer.UpdateRenderMode);
            
            switch (_polyRenderer.RenderMode)
            {
                case RenderMode.Wire:
                    PropertyField("outlineColor", $"Outline", _polyRenderer.UpdateColor);
                    PropertyField("thickness", $"Line Thickness", _polyRenderer.UpdateThickness);
                    TerrainPropFields();
                    break;
                case RenderMode.Mesh:
                    PropertyField("color", $"Fill", _polyRenderer.UpdateColor);
                    break;
                case RenderMode.OutlinedMesh:
                    PropertyField("color", $"Fill", _polyRenderer.UpdateColor);
                    PropertyField("outlineColor", $"Outline", _polyRenderer.UpdateColor);
                    PropertyField("thickness", $"Line Thickness", _polyRenderer.UpdateThickness);
                    break;
            }

            EditorGUILayout.Separator();
            PropertyField("centeredScale", $"Scale", _polyRenderer.UpdatePolygon);
        }


        private void TerrainPropFields()
        {
            if (Terrain.activeTerrain == null) return;
            
            PropertyField("projectedOnTerrain", $"Project On Terrain", _polyRenderer.UpdateTerrainProjection);

            if (_polyRenderer.ProjectedOnTerrain)
                PropertyField("terrainHeightOffset", $"Height Offset", _polyRenderer.UpdateHeightTerrainOffset);
        }


        private bool _subPolygonFoldout = true;
        private void SubPolygonPropFields()
        {
            _subPolygonFoldout = EditorGUILayout.Foldout(_subPolygonFoldout, "SubPolygons", true);
            if (!_subPolygonFoldout) return;

            EditorGUI.indentLevel++;
            
            PropertyField("generateSubPolygons", "Generate SubPolygons",
                () => { _polyRenderer.UpdatePolygon(); _polyRenderer.UpdateSubPolygonRenderers(); });

            if (!_polyRenderer.generateSubPolygons) return;
            
            PropertyField("maxSubPolygonsPerFrame", $"Per Frame");
            
            EditorGUI.BeginChangeCheck();
            _polyRenderer.ShowSubPolygons =
                EditorGUILayout.Toggle("Show subPolygons", _polyRenderer.ShowSubPolygons);
            if (_polyRenderer.ShowSubPolygons)
                EditorGUILayout.LabelField(
                    $"Polygon has been segmented in {_polyRenderer.SubPolygonCount} polygons");
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _polyRenderer.UpdateSubPolygonRenderers();
            }
            
            EditorGUI.indentLevel--;
        }
        
        
        private void PropertyField(string propName, string label, bool includeChildren = true) =>
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName), new GUIContent(label), includeChildren);
        
        private void PropertyField(string propName, string label, Action onChange)
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(propName, label);
            if (!EditorGUI.EndChangeCheck()) return;
            serializedObject.ApplyModifiedProperties();
            onChange?.Invoke();
        }
    }
}
