using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Rendering
{
    [CustomEditor(typeof(PointsRenderer))]
    public class PointsRendererEditor: DynamicRendererEditor
    {
        public override void OnInspectorGUI()
        {
            PointsRenderer renderer = (PointsRenderer)target;
            if (renderer == null) return;

            RenderModeGUI(renderer);
            RadiusGUI(renderer);
            ColorGUI(renderer);
            TestingGUI(renderer);
        }

        public static void RenderModeGUI(PointsRenderer renderer)
        {
            EditorGUI.BeginChangeCheck();
            
            PointsRenderer.RenderMode mode = (PointsRenderer.RenderMode) EditorGUILayout.EnumPopup("Render Mode", renderer.Mode);
            
            if (!EditorGUI.EndChangeCheck()) return;
            
            Undo.RecordObject(renderer, UndoName_RenderModeChanged);
            renderer.Mode = mode;
        }

        public static void RadiusGUI(PointsRenderer renderer)
        {
            EditorGUI.BeginChangeCheck();
            
            float radius = EditorGUILayout.FloatField("Point Radius", renderer.Radius);
            
            if (!EditorGUI.EndChangeCheck()) return;
            
            Undo.RecordObject(renderer, UndoName_PointRadiusChanged);
            renderer.Radius = radius;
        }
        
        
        #region UNDO
        
        private static string UndoName_PointRadiusChanged => "Point Radius Changed";
        private static string UndoName_RenderModeChanged => "Render Mode Changed";
        
        public override Undo.UndoRedoEventCallback UndoRedoEvent => delegate (in UndoRedoInfo info)
        {
            base.UndoRedoEvent(info);

            var renderer = (PointsRenderer) target;
            if (renderer == null) return;
            
            // Line Visibility not needed to be updated
            if (info.undoName == UndoName_RenderModeChanged) renderer.UpdateRenderMode();
            if (info.undoName == UndoName_PointRadiusChanged) renderer.UpdateRadius();
        };
        
        #endregion
    }
}
