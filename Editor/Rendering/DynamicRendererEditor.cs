using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Rendering
{
    [CustomEditor(typeof(DynamicRenderer<>), true)]
    public class DynamicRendererEditor: UnityEditor.Editor, IUndoableEditor
    {
        bool colorFoldout = true;
        
        public override void OnInspectorGUI()
        {
            DynamicRenderer<Renderer> renderer = (DynamicRenderer<Renderer>) target;
            if (renderer == null) return;
            
            colorFoldout = EditorGUILayout.Foldout(colorFoldout, "COLOR", true, EditorStyles.foldoutHeader);
            
            EditorGUILayout.Separator();
            
            EditorGUI.indentLevel++;
            if (colorFoldout) ColorGUI(renderer);
            EditorGUI.indentLevel--;
        }

        private void ColorGUI<T>(DynamicRenderer<T> renderer) where T : Component
        {
            EditorGUI.BeginChangeCheck();
            
            bool singleColor = EditorGUILayout.Toggle("Single Color", renderer.singleColor);
                
            EditorGUILayout.Separator();
                
            Color baseColor = EditorGUILayout.ColorField("Point Color", renderer.BaseColor);
            
            var colorPalette = renderer.ColorPalette;
            
            // PALETTE
            if (!renderer.singleColor)
            {
                float colorStep = EditorGUILayout.Slider("Color Step", renderer.ColorPalette.paletteStep, 0, 1);
                int colorRange = EditorGUILayout.IntSlider("Color Range", renderer.ColorPalette.paletteRange, 0, 100);
                colorPalette = new DynamicRenderer<T>.ColorPaletteData
                {
                    baseColor = renderer.BaseColor,
                    paletteStep = colorStep,
                    paletteRange = colorRange
                };
            }

            if (!EditorGUI.EndChangeCheck()) return;
            
            Undo.RecordObject(renderer, "Color Changed");
            renderer.singleColor = singleColor;
            renderer.ColorPalette = colorPalette;
            renderer.BaseColor = baseColor;
            renderer.UpdateColor();
        }

        
        #region UNDO

        public virtual Undo.UndoRedoEventCallback UndoRedoEvent => delegate (in UndoRedoInfo info)
        {
            Debug.Log("UNDO TRIGGERED");
            
            DynamicRenderer<Renderer> renderer = (DynamicRenderer<Renderer>) target;
            if (renderer == null) return;

            if (info.undoName == "Color Changed") 
                renderer.UpdateColor(); 
        };

        protected void OnEnable() => Undo.undoRedoEvent += UndoRedoEvent;
        protected void OnDisable() => Undo.undoRedoEvent -= UndoRedoEvent;

        #endregion
    }
}
