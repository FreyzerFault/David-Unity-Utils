using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using MyBox;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.DebugExtensions
{
    public static class GizmosExtensions
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

        public static void DrawQuadWire(Vector3 pos, Vector3 extent)
        {
            var vertices = new[]
            {
                pos,
                pos + Vector3.Project(extent, Vector3.forward),
                pos + extent,
                pos + Vector3.Project(extent, Vector3.right)
            };
            Gizmos.DrawLineStrip(vertices, true);
        }

        public static void DrawQuad(Vector3 pos, Vector3 extent, Color color = default)
        {
            var vertices = new[]
            {
                pos,
                pos + Vector3.Project(extent, Vector3.forward),
                pos + extent,
                pos + Vector3.Project(extent, Vector3.right)
            };
            Handles.DrawSolidRectangleWithOutline(vertices, color, color);
        }
        
        public static void DrawQuad(Vector3[] vertices, Color color = default) => 
            Handles.DrawSolidRectangleWithOutline(vertices, color, Color.yellow);

        public static void DrawGrid(float cellRows, float cellCols, Vector3 pos, Vector2 size) 
        {
            float cellSize = 1f / cellRows;

            for (var y = 0; y < cellRows; y++)
            for (var x = 0; x < cellRows; x++)
                DrawQuadWire(pos + new Vector3(x * size.x, 0, y * size.y) * cellSize, cellSize * size.ToVector3xz());
        }
        
        public static void DrawBezier(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, int segments = 20)
        {
            Vector3[] bezierPoints = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float) segments;
                bezierPoints[i] = Mathf.Pow(1 - t, 3) * p1 + 3 * Mathf.Pow(1 - t, 2) * t * p2 + 3 * (1 - t) * Mathf.Pow(t, 2) * p3 + Mathf.Pow(t, 3) * p4;
            }
            Gizmos.DrawLineStrip(bezierPoints, false);
        }

        public static void DrawBezierThick(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float thickness = 1, Color color = default) => 
            Handles.DrawBezier(p1,p2,p3,p4, color, null, thickness);
        
        public static void DrawLineThick(Vector3[] points, float thickness = 1, Color color = default, bool loop = true)
        {
            if (loop) points = points.Append(points[0]).ToArray();
            Color[] colors = new Color[points.Length];
            Array.Fill(colors, color);
            Handles.DrawAAPolyLine(thickness, colors, points);
        }

        public static void DrawPolygonWire(Vector3[] vertices, float thickness = 1, Color color = default) => 
            DrawLineThick(vertices.Append(vertices[0]).ToArray(), thickness, color);
        
        
        public static void DrawTri(Vector3[] vertices, Color color = default) => 
            Handles.DrawSolidRectangleWithOutline(vertices.Append(vertices[2]).ToArray(), color, color);
    }
}
