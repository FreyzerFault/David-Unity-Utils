using UnityEditor;

namespace DavidUtils.Editor.Rendering
{
    public interface IUndoableEditor
    {
        public Undo.UndoRedoEventCallback UndoRedoEvent { get; }
        
        // Copiar esto en los scripts que implementen esta interfaz:
        
        private void OnEnable() => Undo.undoRedoEvent += UndoRedoEvent;
        private void OnDisable() => Undo.undoRedoEvent -= UndoRedoEvent;
    }
}
