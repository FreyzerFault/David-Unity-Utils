using UnityEditor;
using UnityEngine;

namespace DavidUtils.DebugExtensions
{
    public class HandlesExtensions : MonoBehaviour
    {
        public static void DrawLabel(Vector3 position, string text, GUIStyle style = default, Vector3 positionOffset = default)
        {
            // POSITION
            Vector3 pos = position + positionOffset;
            Handles.Label(pos, text, style);
        }
    }
}
