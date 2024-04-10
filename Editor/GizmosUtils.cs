using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor
{
    public class GizmosUtils
    {
        public static void DrawArrow(Vector3 pos, Vector3 direction, float size = 1)
        {
            Vector3 tangent = Vector3.Cross(direction, Vector3.up);
            Vector3 arrowVector = direction * size;
            Gizmos.DrawLineList(
                new[]
                {
                    pos,
                    pos + arrowVector,
                    pos + arrowVector,
                    pos + arrowVector - Quaternion.AngleAxis(30, tangent) * arrowVector * 0.4f,
                    pos + arrowVector,
                    pos + arrowVector - Quaternion.AngleAxis(-30, tangent) * arrowVector * 0.4f
                }
            );
        }
        
        public static void DrawLabel(Vector3 position, string text, GUIStyle style = default, Vector3 positionOffset = default)
        {
            // POSITION
            Vector3 pos = position + positionOffset;
            Handles.Label(pos, text, style);
        }
    }
}