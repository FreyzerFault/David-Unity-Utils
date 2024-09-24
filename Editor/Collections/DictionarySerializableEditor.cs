using System.Collections;
using DavidUtils.Collections;
using UnityEditor;

namespace DavidUtils.Editor.Collections
{
    [CustomEditor(typeof(DictionarySerializable<,>), true)]
    public class DictionarySerializableEditor: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            IDictionary dict = (IDictionary) target;
            if (dict == null) return;

            // var dict = (DictionarySerializable<Tkey,Tvalue>) target;
            // if (GUILayout.Button("Add Element"))
            // {
            //     dict.pairElements.Add(new DictionarySerializable<,>.KeyValuePairSerializable());
            // }
        }
    }
}
