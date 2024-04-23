using UnityEditor;
using UnityEngine;

namespace DavidUtils.DebugExtensions
{
    public class HandlesExtensions : MonoBehaviour
    {
        public static void DrawLabel(
            Vector3 position,
            string text,
            Color? textColor = null,
            int fontSize = 12,
            FontStyle fontStyle = FontStyle.Bold
            )
        {
            Handles.Label(position, text, 
                new GUIStyle
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                });
        }
    }
}
