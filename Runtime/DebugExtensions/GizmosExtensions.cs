using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.DebugExtensions
{
    public static class GizmosExtensions
    {
        private const int DEFAULT_THICKNESS = 5;
        
        #region ARROWS

        public static void DrawArrowWire(Vector3 pos, Vector3 direction, Vector3 headOrientation = default, float length = 1, float capSize = 0.4f, float thickness = DEFAULT_THICKNESS, Color color = default)
        {
            Vector3 tipPos = pos + direction * length;
            
            // If headOrientation is same as direction of arrow, take another
            if (headOrientation == direction) headOrientation = Quaternion.AngleAxis(90, Vector3.up) * direction;
            
            // Axis rotation of head tips
            Vector3 tangent = Vector3.Cross(direction, headOrientation);

            Vector3[] vertices = {
                pos,
                tipPos,
                tipPos,
                tipPos - Quaternion.AngleAxis(30, tangent) * direction * capSize,
                tipPos,
                tipPos - Quaternion.AngleAxis(-30, tangent) * direction * capSize
            };
            DrawLineThick(vertices, thickness, color);
        }

        public static void DrawArrow(Vector3 pos, Vector3 direction, Vector3 headOrientation = default, float length = 1, float capSize = 0.4f, Color color = default)
        {
            Vector3 tipPos = pos + direction * length;
            
            DrawLineThick(pos, tipPos - direction * capSize, 10, color);
            DrawCone(tipPos  - direction * capSize, capSize/ 2, capSize, Quaternion.FromToRotation(Vector3.up, direction), color);
        }

        #endregion

        #region QUAD

        public static void DrawQuadWire(Vector3 pos, Vector3 extent, float thickness = 1, Color color = default)
        {
            var vertices = new[]
            {
                pos,
                pos + Vector3.Project(extent, Vector3.forward),
                pos + extent,
                pos + Vector3.Project(extent, Vector3.right)
            };
            DrawLineThick(vertices, thickness, color, true);
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

        #endregion

        #region GRID

        public static void DrawGrid(float cellRows, float cellCols, Vector3 pos, Vector2 size) 
        {
            float cellSize = 1f / cellRows;

            for (var y = 0; y < cellRows; y++)
            for (var x = 0; x < cellRows; x++)
                DrawQuadWire(pos + new Vector3(x * size.x, 0, y * size.y) * cellSize, cellSize * size.ToVector3xz());
        }
        
        #endregion

        #region BEZIER CURVES

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
        
        #endregion

        #region LINES

        public static void DrawLineThick(Vector3 a, Vector3 b, float thickness = DEFAULT_THICKNESS, Color color = default, bool loop = true)
        {
            Handles.color = color;
            Handles.DrawLine(a, b, thickness);
        }
        
        public static void DrawLineThick(Vector3[] points, float thickness = DEFAULT_THICKNESS, Color color = default, bool loop = false)
        {
            if (loop) points = points.Append(points[0]).ToArray();
            Color[] colors = new Color[points.Length];
            Array.Fill(colors, color);
            Handles.DrawAAPolyLine(thickness, colors, points);
        }

        #endregion

        #region POLYGONS

        public static void DrawPolygonWire(Vector3[] vertices, float thickness = DEFAULT_THICKNESS, Color color = default) => 
            DrawLineThick(vertices.Append(vertices[0]).ToArray(), thickness, color);
        
        
        public static void DrawPolygon(Vector3[] vertices, Color color = default)
        {
            Vector3 mainVertex = vertices[0];
            for (int i = 1, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                Vector3 a = vertices[j], b = vertices[i];
                DrawTri(new Vector3[] { mainVertex, a, b }, color);
            }
        }

        #endregion

        #region TRIANGLE
        
        public static void DrawTriWire(Vector3[] vertices, float thickness = 1, Color color = default) => 
            DrawLineThick(vertices, thickness, color);
        
        public static void DrawTri(Vector3[] vertices, Color color = default) => 
            Handles.DrawSolidRectangleWithOutline(vertices.Append(vertices[2]).ToArray(), color, color);

        #endregion

        #region CIRCLES

        public static void DrawCircleWire(Vector3 pos, Vector3 normal, float radius, float thickness = 1, Color color = default)
        {
            Handles.color = color;
            Handles.DrawWireDisc(pos, normal, radius, thickness);
        }
        
        public static void DrawCircle(Vector3 pos, Vector3 normal, float radius, Color color = default)
        {
            Handles.color = color;
            Handles.DrawSolidDisc(pos, normal, radius);
        }

        #endregion

        #region CILINDER

        public static void DrawCilinderWire(Vector3 pos, float radius, float height, Quaternion rotation = default, float thickness = 1, Color color = default)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3 back = rotation * Vector3.back;
            DrawCircleWire(pos, up, radius, thickness, color);
            DrawCircleWire(pos + up * height, up, radius, thickness, color);
            DrawQuadWire(pos - right * radius, right * radius * 2 + up * height, thickness, color);
            DrawQuadWire(pos - back * radius, back * radius * 2 + up * height, thickness, color);
        }
        
        public static void DrawCilinder(Vector3 pos, float radius, float height, Quaternion rotation = default, Color color = default)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3 back = rotation * Vector3.back;
            DrawCircle(pos, up, radius, color);
            DrawCircle(pos, up * height, radius, color);
            DrawQuad(pos - right * radius, right * radius * 2 + up * height, color);
            DrawQuad(pos - back * radius, back * radius * 2 + up * height, color);
        }

        #endregion

        #region CONE

        public static void DrawConeWire(Vector3 pos, float radius, float height, Quaternion rotation = default, float thickness = 1, Color color = default)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3 back = rotation * Vector3.back;
            
            DrawCircleWire(pos, Vector3.up, radius, thickness, color);
            DrawTriWire(new []
            {
                pos - right * radius,
                pos + up * height,
                pos + right * radius
            }, thickness, color);
            
            DrawTriWire(new []
            {
                pos - back * radius,
                pos + up * height,
                pos + back * radius
            }, thickness, color);
        }
        
        public static void DrawCone(Vector3 pos, float radius, float height, Quaternion rotation = default, Color color = default)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3 back = rotation * Vector3.back;
            
            DrawCircle(pos, rotation * Vector3.up, radius, color);
            DrawTri(new []
            {
                pos - right * radius,
                pos + up * height,
                pos + right * radius
            });
            DrawTri(new []
            {
                pos - back * radius,
                pos + up * height,
                pos + back * radius
            }, color);
            Vector3 diagonal1 = (right + back).normalized;
            Vector3 diagonal2 = (right - back).normalized;
            DrawTri(new []
            {
                pos - diagonal1 * radius,
                pos + up * height,
                pos + diagonal1 * radius
            }, color);
            DrawTri(new []
            {
                pos - diagonal2 * radius,
                pos + up * height,
                pos + diagonal2 * radius
            }, color);
        }

        #endregion
    }
}
