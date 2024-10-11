using DavidUtils.Rendering;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.Rendering
{
    public class DynamicRendererEditor: UnityEditor.Editor, IUndoableEditor
    {
        protected bool colorFoldout = true;

        protected void ColorGUI<T>(DynamicRenderer<T> renderer) where T : Component
        {
            colorFoldout = EditorGUILayout.Foldout(colorFoldout, "COLOR", true, EditorStyles.foldoutHeader);
            if (!colorFoldout) return;
            
            EditorGUILayout.Separator();
            
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            
            bool singleColor = EditorGUILayout.Toggle("Single Color", renderer.singleColor);
                
            EditorGUILayout.Separator();
                
            Color baseColor = EditorGUILayout.ColorField("Base Color", renderer.BaseColor);
            
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
            
            EditorGUI.indentLevel--;

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
            switch (target)
            {
                case DynamicRenderer<PolygonRenderer> polyRenderer:
                    if (info.undoName == "Color Changed") 
                        polyRenderer.UpdateColor(); 
                    break;
                case DynamicRenderer<Renderer> renderer:
                    if (info.undoName == "Color Changed") 
                        renderer.UpdateColor(); 
                    break;
                default:
                    // SubType Custom Editor NOT IMPLEMENTED
                    Debug.LogError($"{target.GetType()} Custom Editor is not implemented.\n" +
                                   $"Base Type is {target.GetType().BaseType}");
                    base.OnInspectorGUI();
                    return;
            }

        };

        protected void OnEnable() => Undo.undoRedoEvent += UndoRedoEvent;
        protected void OnDisable() => Undo.undoRedoEvent -= UndoRedoEvent;

        #endregion
    }
}
