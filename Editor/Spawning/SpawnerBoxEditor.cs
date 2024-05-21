using DavidUtils.Spawning;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor
{
    [CustomEditor(typeof(SpawnerBox), true)]
    public class SpawnerBoxEditor : UnityEditor.Editor
    {
        private int numItemsSpawned = 1;
        
        public override void OnInspectorGUI()
        {
            var spawner = (SpawnerBox)target;

            DrawDefaultInspector();
            
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Spawn", GUILayout.ExpandWidth(true)))
                for (var i = 0; i < numItemsSpawned; i++)
                {
                    if (spawner is SpawnerBoxInTerrain boxInTerrain)
                        boxInTerrain.SpawnRandom();
                    else
                        spawner.SpawnRandom();
                }

            GUILayout.Space(20);

            numItemsSpawned = EditorGUILayout.IntField("Num Items", numItemsSpawned, GUILayout.MaxWidth(300));

            GUILayout.EndHorizontal();
        }
    }
}
