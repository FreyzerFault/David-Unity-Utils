using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DavidUtils.DebugUtils
{
	public static class GizmosExtensions
	{
		private const int DEFAULT_THICKNESS = 5;

#if UNITY_EDITOR

		#region ARROWS

		public static void DrawArrowWire(
			Vector3 pos, Vector3 direction, Vector3 headOrientation = default, float length = 1, float capSize = 0.4f,
			float thickness = DEFAULT_THICKNESS, Color color = default
		)
		{
			Vector3 tipPos = pos + direction * length;

			// If headOrientation is same as direction of arrow, take another
			if (headOrientation == direction) headOrientation = Quaternion.AngleAxis(90, Vector3.up) * direction;

			// Axis rotation of head tips
			Vector3 tangent = Vector3.Cross(direction, headOrientation);

			Vector3[] vertices =
			{
				pos,
				tipPos,
				tipPos,
				tipPos - Quaternion.AngleAxis(30, tangent) * direction * capSize,
				tipPos,
				tipPos - Quaternion.AngleAxis(-30, tangent) * direction * capSize
			};
			DrawLineThick(vertices, thickness, color);
		}

		public static void DrawArrow(
			Vector3 pos, Vector3 direction, Vector3 headOrientation = default, float length = 1, float capSize = 0.4f,
			Color color = default
		)
		{
			Vector3 tipPos = pos + direction * length;

			DrawLineThick(pos, tipPos - direction * capSize, 10, color);
			DrawCone(
				tipPos - direction * capSize,
				capSize / 2,
				capSize,
				Quaternion.FromToRotation(Vector3.up, direction),
				color
			);
		}

		#endregion

		#region QUAD

		public static Vector3[] QuadVertices(Matrix4x4 matrix) => new[]
		{
			matrix.MultiplyPoint3x4(new Vector2(0, 0).ToVector3xz()),
			matrix.MultiplyPoint3x4(new Vector2(1, 0).ToVector3xz()),
			matrix.MultiplyPoint3x4(new Vector2(1, 1).ToVector3xz()),
			matrix.MultiplyPoint3x4(new Vector2(0, 1).ToVector3xz())
		};

		public static void DrawQuadWire(Matrix4x4 matrix, float thickness = 1, Color color = default) =>
			DrawLineThick(QuadVertices(matrix), thickness, color, true);

		public static void DrawQuad(Matrix4x4 matrix, Color color = default, Color outlineColor = default) =>
			Handles.DrawSolidRectangleWithOutline(QuadVertices(matrix), color, outlineColor);

		public static void DrawQuad(Vector3[] vertices, Color color = default, Color outlineColor = default) =>
			Handles.DrawSolidRectangleWithOutline(vertices, color, outlineColor);

		#endregion

		#region GRID

		public static void DrawGrid(
			float cellRows, float cellCols, Matrix4x4 matrix, float thickness = 1, Color color = default
		)
		{
			Vector2 cellSize = Vector2.one / new Vector2(cellRows, cellCols);

			for (var y = 0; y < cellRows; y++)
			for (var x = 0; x < cellRows; x++)
				DrawQuadWire(
					matrix * Matrix4x4.TRS(
						new Vector3(x * cellSize.x, 0, y * cellSize.y),
						Quaternion.identity,
						cellSize.ToVector3xz()
					),
					thickness,
					color
				);
		}

		#endregion

		#region BEZIER CURVES

		public static void DrawBezier(Vector3 begin, Vector3 end, Vector3 p3, Vector3 p4, int segments = 20)
		{
			var bezierPoints = new Vector3[segments + 1];
			for (var i = 0; i <= segments; i++)
			{
				float t = i / (float)segments;
				bezierPoints[i] = Mathf.Pow(1 - t, 3) * begin + 3 * Mathf.Pow(1 - t, 2) * t * end
				                                              + 3 * (1 - t) * Mathf.Pow(t, 2) * p3
				                                              + Mathf.Pow(t, 3) * p4;
			}

			Gizmos.DrawLineStrip(bezierPoints, false);
		}

		public static void DrawBezierThick(
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float thickness = 1, Color color = default
		) =>
			Handles.DrawBezier(p1, p2, p3, p4, color, null, thickness);

		#endregion

		#region LINES

		public static void DrawLineThick(
			Vector3 a, Vector3 b, float thickness = DEFAULT_THICKNESS, Color color = default
		)
		{
			using (new Handles.DrawingScope(color))
			{
				Handles.DrawLine(a, b, thickness);
			}
		}

		public static void DrawLineThick(
			Vector3[] points, float thickness = DEFAULT_THICKNESS, Color color = default, bool loop = false
		)
		{
			if (points.Length == 0) return;
			if (loop) points = points.Append(points[0]).ToArray();
			var colors = new Color[points.Length];
			Array.Fill(colors, color);
			Handles.DrawAAPolyLine(thickness, colors, points);
		}

		#endregion

		#region POLYGONS

		public static void DrawPolygonWire(
			Vector3[] vertices, float thickness = DEFAULT_THICKNESS, Color color = default
		) =>
			DrawLineThick(vertices.Length == 0 ? vertices : vertices.Append(vertices[0]).ToArray(), thickness, color);


		public static void DrawPolygon(Vector3[] vertices, Color color = default)
		{
			Vector3 mainVertex = vertices[0];
			for (int i = 1, j = vertices.Length - 1; i < vertices.Length; j = i++)
			{
				Vector3 a = vertices[j], b = vertices[i];
				DrawTri(new[] { mainVertex, a, b }, color);
			}
		}

		#endregion

		#region TRIANGLE

		public static void DrawTriWire(Vector3[] vertices, float thickness = 1, Color color = default) =>
			DrawLineThick(vertices, thickness, color, true);

		public static void DrawTri(Vector3[] vertices, Color color = default) =>
			Handles.DrawSolidRectangleWithOutline(vertices.Append(vertices[2]).ToArray(), color, color);

		#endregion

		#region CIRCLES

		public static void DrawCircleWire(
			Vector3 pos, Vector3 normal, float radius, float thickness = 1, Color color = default
		)
		{
			using (new Handles.DrawingScope(color))
			{
				Handles.DrawWireDisc(pos, normal, radius, thickness);
			}
		}

		public static void DrawCircle(Vector3 pos, Vector3 normal, float radius, Color color = default)
		{
			using (new Handles.DrawingScope(color))
			{
				Handles.DrawSolidDisc(pos, normal, radius);
			}
		}

		#endregion

		#region CILINDER

		public static void DrawCilinderWire(
			float radius, float height, Matrix4x4 matrix = default, int sections = 1, float thickness = 1,
			Color color = default
		)
		{
			var center = new Vector3(0, 0, 0);
			var size = new Vector3(radius * 2, height, radius * 2);

			// BASE
			Vector3 baseCenter = center - Vector3.up * height / 2;
			float sectionHeight = height / sections;
			for (var i = 0; i <= sections; i++)
				DrawCircleWire(
					matrix.MultiplyPoint3x4(baseCenter + Vector3.up * i * sectionHeight),
					matrix * Vector3.up,
					matrix.lossyScale.x * radius,
					thickness / 2,
					color
				);

			DrawQuadWire(matrix * Matrix4x4.Scale(size), thickness, color);
			DrawQuadWire(matrix * Matrix4x4.Rotate(Quaternion.AngleAxis(90, Vector3.up)), thickness, color);
		}

		public static void DrawCilinder(
			float radius, float height, Matrix4x4 matrix = default, int sections = 1, Color color = default
		)
		{
			var center = new Vector3(0, 0, 0);
			var size = new Vector3(radius * 2, height, radius * 2);

			// BASE
			Vector3 baseCenter = center - Vector3.up * height / 2;
			float sectionHeight = height / sections;
			for (var i = 0; i <= sections; i++)
				DrawCircle(
					matrix.MultiplyPoint3x4(baseCenter + Vector3.up * i * sectionHeight),
					matrix * Vector3.up,
					matrix.lossyScale.x * radius,
					color
				);

			// SIDE
			DrawQuad(matrix * Matrix4x4.Scale(size), color);
			DrawQuad(matrix * Matrix4x4.Rotate(Quaternion.AngleAxis(90, Vector3.up)), color);
		}

		#endregion

		#region CONE

		public static void DrawConeWire(
			Vector3 pos, float radius, float height, Quaternion rotation = default, float thickness = 1,
			Color color = default
		)
		{
			Vector3 up = rotation * Vector3.up;
			Vector3 right = rotation * Vector3.right;
			Vector3 back = rotation * Vector3.back;

			DrawCircleWire(pos, Vector3.up, radius, thickness, color);
			DrawTriWire(
				new[]
				{
					pos - right * radius,
					pos + up * height,
					pos + right * radius
				},
				thickness,
				color
			);

			DrawTriWire(
				new[]
				{
					pos - back * radius,
					pos + up * height,
					pos + back * radius
				},
				thickness,
				color
			);
		}

		public static void DrawCone(
			Vector3 pos, float radius, float height, Quaternion rotation = default, Color color = default
		)
		{
			Vector3 up = rotation * Vector3.up;
			Vector3 right = rotation * Vector3.right;
			Vector3 back = rotation * Vector3.back;

			DrawCircle(pos, rotation * Vector3.up, radius, color);
			DrawTri(
				new[]
				{
					pos - right * radius,
					pos + up * height,
					pos + right * radius
				}
			);
			DrawTri(
				new[]
				{
					pos - back * radius,
					pos + up * height,
					pos + back * radius
				},
				color
			);
			Vector3 diagonal1 = (right + back).normalized;
			Vector3 diagonal2 = (right - back).normalized;
			DrawTri(
				new[]
				{
					pos - diagonal1 * radius,
					pos + up * height,
					pos + diagonal1 * radius
				},
				color
			);
			DrawTri(
				new[]
				{
					pos - diagonal2 * radius,
					pos + up * height,
					pos + diagonal2 * radius
				},
				color
			);
		}

		#endregion

		#region BEZIER

		public static void DrawBezier(
			Vector3 begin, Vector3 end, Vector3 p3, Vector3 p4, float thickness = 1, Color color = default
		)
		{
			Vector3 tangent1 = end - begin;
			Vector3 tangent2 = p4 - p3;
			Texture2D tex = EditorGUIUtility.whiteTexture;
			Handles.DrawBezier(begin, p4, tangent1, tangent2, color, tex, thickness);
		}

		public static void DrawBezierByTangents(
			Vector3 begin, Vector3 end, Vector3 tangentBegin, Vector3 tangentEnd, float thickness = 1,
			Color color = default
		)
		{
			Texture2D tex = EditorGUIUtility.whiteTexture;
			Handles.DrawBezier(begin, end, tangentBegin, tangentEnd, color, tex, thickness);
		}

		#endregion


		#region LABELS

		public static void DrawLabel(
			Vector3 position,
			string text,
			Color? textColor = null,
			int fontSize = 12,
			FontStyle fontStyle = FontStyle.Bold
		) => Handles.Label(
			position,
			text,
			new GUIStyle
			{
				fontSize = 12,
				fontStyle = FontStyle.Bold,
				normal = { textColor = Color.white }
			}
		);

		#endregion


		#region TERRAIN VARIANTS

		public static void DrawLineThick_OnTerrain(
			Vector3 a, Vector3 b, float thickness = DEFAULT_THICKNESS, Color color = default, Terrain terrain = null
		)
		{
			terrain ??= Terrain.activeTerrain;
			DrawLineThick(terrain.ProjectPathToTerrain(new[] { a, b }), thickness, color);
		}

		public static void DrawLineThick_OnTerrain(
			Vector3[] vertices, float thickness = DEFAULT_THICKNESS, Color color = default, Terrain terrain = null
		)
		{
			terrain ??= Terrain.activeTerrain;
			DrawLineThick(terrain.ProjectPathToTerrain(vertices), thickness, color);
		}

		public static void DrawPolygon_OnTerrain(Vector3[] vertices, Color color = default, Terrain terrain = null)
		{
			terrain ??= Terrain.activeTerrain;
			DrawPolygon(terrain.ProjectPathToTerrain(vertices), color);
		}

		public static void DrawPolygonWire_OnTerrain(
			Vector3[] vertices, float thickness = DEFAULT_THICKNESS, Color color = default, Terrain terrain = null
		)
		{
			terrain ??= Terrain.activeTerrain;
			DrawPolygonWire(terrain.ProjectPathToTerrain(vertices), thickness, color);
		}

		public static void DrawQuadWire_OnTerrain(
			Matrix4x4 matrix, float thickness = 1, Color color = default, Terrain terrain = null
		) => DrawPolygonWire_OnTerrain(QuadVertices(matrix), thickness, color, terrain);

		public static void DrawQuad_OnTerrain(
			Matrix4x4 matrix, Color color = default, Terrain terrain = null
		) => DrawPolygon_OnTerrain(QuadVertices(matrix), color, terrain);


		public static void DrawGrid_OnTerrain(
			float cellRows, float cellCols, Matrix4x4 matrix, float thickness = 1,
			Color color = default, Terrain terrain = null
		)
		{
			Vector2 cellSize = Vector2.one / new Vector2(cellRows, cellCols);
			Matrix4x4 cellScaleMatrix = Matrix4x4.Scale(cellSize.ToVector3xz().WithY(1));

			for (var y = 0; y < cellRows; y++)
			for (var x = 0; x < cellRows; x++)
				DrawQuadWire_OnTerrain(
					matrix
					* Matrix4x4.Translate(new Vector3(cellSize.x * x, 0, cellSize.y * y))
					* cellScaleMatrix,
					thickness,
					color,
					terrain
				);
		}

		#endregion

#endif
	}
}
