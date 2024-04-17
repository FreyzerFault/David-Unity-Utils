using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor
{
    public class GizmosUtils
    {
        public static void DrawArrow(Vector3 pos, Vector3 direction, Vector3 headOrientation = default, float size = 1)
        {
            Vector3 arrowVector = direction * size;
            
            // If headOrientation is same as direction of arrow, take another
            if (headOrientation == direction) headOrientation = Quaternion.AngleAxis(90, Vector3.up) * direction;
            
            // Axis rotation of head tips
            Vector3 tangent = Vector3.Cross(direction, headOrientation);
            
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
