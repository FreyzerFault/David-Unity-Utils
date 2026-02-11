using System;
using System.Linq;
using DavidUtils.Utils;
using UnityEngine;
using UnityEditor;

namespace DavidUtils.DevTools.GizmosAndHandles
{
	public static class GizmosExtensions
	{
		private const int DEFAULT_THICKNESS = 5;

#if UNITY_EDITOR
		
		#region ARROWS

		public enum ArrowCap { Line, Triangle, Cone, None }

		private static Vector3[] ArrowTipVertices(
			Vector3 tipPos, Vector3 dir, Vector3 normal, float capSize = 0.4f, float angle = 20
		) =>
			new[]
			{
				tipPos - Quaternion.AngleAxis(angle, normal) * dir * capSize,
				tipPos,
				tipPos - Quaternion.AngleAxis(-angle, normal) * dir * capSize
			};

		public static void DrawArrow(
			ArrowCap cap, Vector3 pos, Vector3 vector, Vector3 normal = default, float capSize = 0.4f,
			Color? color = null, float thickness = DEFAULT_THICKNESS
		)
		{
			Vector3 tipPos = pos + vector;

			// BODY
			DrawLineThick(pos, tipPos, thickness, color);

			// If headOrientation is same as direction of arrow, take another
			if (normal.normalized == vector.normalized)
				normal = Quaternion.AngleAxis(90, Vector3.up) * vector;

			switch (cap)
			{
				case ArrowCap.Line:
					DrawLineThick(ArrowTipVertices(tipPos, vector.normalized, normal, capSize), thickness, color);
					break;
				case ArrowCap.Triangle:
					DrawTri(ArrowTipVertices(tipPos, vector.normalized, normal, capSize), color);
					break;
				case ArrowCap.Cone:
					DrawCone(
						Matrix4x4.TRS(
							tipPos - vector.normalized * capSize,
							Quaternion.FromToRotation(Vector3.up, vector),
							new Vector3(capSize, capSize, capSize * 2)
						),
						color
					);
					break;
				case ArrowCap.None: break;
			}
		}

		#endregion

		#region QUAD

		public static Vector3[] QuadVertices(Matrix4x4 matrix, bool centered = false) => new[]
		{
			matrix.MultiplyPoint3x4(centered ? new Vector2(-.5f, -.5f) : new Vector2(0, 0)),
			matrix.MultiplyPoint3x4(centered ? new Vector2(.5f, -.5f) : new Vector2(1, 0)),
			matrix.MultiplyPoint3x4(centered ? new Vector2(.5f, .5f) : new Vector2(1, 1)),
			matrix.MultiplyPoint3x4(centered ? new Vector2(-.5f, .5f) : new Vector2(0, 1))
		};

		public static void DrawQuadWire(
			Matrix4x4 matrix, float thickness = 1, Color color = default, bool centered = false
		) =>
			DrawLineThick(QuadVertices(matrix, centered), thickness, color, true);

		public static void DrawQuad(
			Matrix4x4 matrix, Color color = default, Color? outlineColor = null, bool centered = false
		) =>
			Handles.DrawSolidRectangleWithOutline(QuadVertices(matrix, centered), color, outlineColor ?? color);

		public static void DrawQuad(Vector3[] vertices, Color color = default, Color? outlineColor = null) =>
			Handles.DrawSolidRectangleWithOutline(vertices, color, outlineColor ?? color);

		#endregion

		#region GRID

		public static void DrawGrid(
			float cellRows, float cellCols, Matrix4x4 matrix, float thickness = 1, Color color = default
		)
		{
			// rowSize = 1 / rows; colSize = 1 / cols
			Vector2 cellSize = Vector2.one / new Vector2(cellRows, cellCols);

			for (var y = 0; y < cellRows; y++)
			for (var x = 0; x < cellRows; x++)
			{
				Matrix4x4 cellMatrix = matrix * Matrix4x4.TRS(
					new Vector2(x, y) * cellSize,
					Quaternion.identity,
					cellSize
				);
				DrawQuadWire(cellMatrix, thickness, color);
			}
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
			Vector3 a, Vector3 b, float thickness = DEFAULT_THICKNESS, Color? color = null
		)
		{
			if (color.HasValue) Handles.color = color.Value;
			Handles.DrawLine(a, b, thickness);
		}

		public static void DrawLineThick(
			Vector3[] points, float thickness = DEFAULT_THICKNESS, Color? color = null, bool loop = false
		)
		{
			if (points.Length == 0) return;
			if (loop) points = points.Append(points[0]).ToArray();
			var colors = new Color[points.Length + (loop ? 1 : 0)];
			Array.Fill(colors, color ?? Color.white);
			Handles.DrawAAPolyLine(thickness, colors, points);
		}

		#endregion

		#region POLYGONS

		public static void DrawPolygonWire(
			Vector3[] vertices, float thickness = DEFAULT_THICKNESS, Color? color = null
		) =>
			DrawLineThick(vertices.Length == 0 ? vertices : vertices.Append(vertices[0]).ToArray(), thickness, color);


		public static void DrawPolygon(
			Vector3[] vertices, Color? color = null, Color? outlineColor = null,
			float outlineThickness = DEFAULT_THICKNESS
		)
		{
			if (color.HasValue) Handles.color = color.Value;
			Handles.DrawAAConvexPolygon(vertices);
			if (outlineColor.HasValue) DrawPolygonWire(vertices, outlineThickness, outlineColor.Value);
		}

		#endregion

		#region TRIANGLE

		public static void DrawTriWire(Vector3[] vertices, float thickness = 1, Color? color = null) =>
			DrawLineThick(vertices, thickness, color, true);

		public static void DrawTri(Vector3[] vertices, Color? color = null, Color? outlineColor = null) =>
			Handles.DrawSolidRectangleWithOutline(
				vertices.Append(vertices[2]).ToArray(),
				color ?? Color.white,
				outlineColor ?? color ?? Color.white
			);

		#endregion

		#region CIRCLES

		public static void DrawCircleWire(
			Vector3 pos, Vector3 normal, float radius, float thickness = 1, Color? color = null
		)
		{
			if (color.HasValue) Handles.color = color.Value;
			Handles.DrawWireDisc(pos, normal, radius, thickness);
		}

		public static void DrawCircleWire(
			Matrix4x4 localToWorldM, float thickness = DEFAULT_THICKNESS, Color? color = null
		)
		{
			Vector3 pos = localToWorldM.MultiplyPoint3x4(Vector3.zero);
			Vector3 normal = localToWorldM.MultiplyVector(Vector3.back);
			float radius = localToWorldM.lossyScale.x / 2;
			DrawCircleWire(pos, normal, radius, thickness, color);
		}

		public static void DrawCircle(
			Vector3 pos, Vector3 normal, float radius, Color? color = null, Color? outlineColor = null
		)
		{
			if (color.HasValue) Handles.color = color.Value;
			Handles.DrawSolidDisc(pos, normal, radius);
			if (outlineColor.HasValue) DrawCircleWire(pos, normal, radius, 1, outlineColor.Value);
		}

		public static void DrawCircle(
			Matrix4x4 localToWorldM, Color? color = null, Color? outlineColor = null
		)
		{
			Vector3 pos = localToWorldM.MultiplyPoint3x4(Vector3.zero);
			Vector3 normal = localToWorldM.MultiplyVector(Vector3.back);
			float radius = localToWorldM.lossyScale.x / 2;
			DrawCircle(pos, normal, radius, color, outlineColor);
		}

		#endregion

		#region CUBE

		public static void DrawCube(Matrix4x4 matrix, Color? color = null, bool centered = true)
		{
			if (color.HasValue) Handles.color = color.Value;
			Gizmos.DrawCube(
				matrix.MultiplyPoint3x4(centered ? Vector3.zero : Vector3.one / 2),
				matrix.MultiplyPoint3x4(Vector3.one / 2)
			);
		}

		public static void DrawCubeWire(
			Matrix4x4 matrix, float thickness = DEFAULT_THICKNESS, Color? color = null, bool centered = true
		)
		{
			if (color.HasValue) Gizmos.color = color.Value;
			Vector3 pos = matrix.MultiplyPoint3x4(Vector3.zero),
				extent = matrix.MultiplyVector(Vector3.one / 2);
			Gizmos.DrawWireCube(centered ? pos + extent : pos, extent * 2);
		}

		#endregion

		#region CILINDER

		public static void DrawCilinderWire(
			float radius, float height, Matrix4x4 matrix = default, int sections = 1, float thickness = 1,
			Color color = default
		)
		{
			Vector3 center = new(0, 0, 0);
			Vector3 size = new(radius * 2, height, radius * 2);

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
			Vector3 center = new(0, 0, 0);
			Vector3 size = new(radius * 2, height, radius * 2);

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
			Color? color = null
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

		public static void DrawCone(Matrix4x4 localToWorldM, Color? color = null)
		{
			if (color.HasValue) Handles.color = color.Value;

			Vector3 baseCenter = Vector3.zero;

			var radius = .5f;
			float height = 1;

			Vector3[] tri =
			{
				baseCenter - Vector3.right * radius,
				baseCenter + Vector3.up * height,
				baseCenter + Vector3.right * radius
			};

			Vector3 baseWorld = localToWorldM.MultiplyPoint3x4(Vector3.zero);

			// CIRCLE
			Quaternion upRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
			Matrix4x4 circleMatrix = localToWorldM * Matrix4x4.Rotate(upRotation);
			DrawCircleWire(circleMatrix, 3, color?.Invert());
			DrawCircle(circleMatrix, color);

			// TRIANGLE BILLBOARD
			Vector3 triUp = localToWorldM.MultiplyVector(Vector3.up);
			Vector3 triForward = localToWorldM.MultiplyVector(Vector3.forward);
			Camera cam = SceneView.lastActiveSceneView.camera;
			Matrix4x4 triMatrix = BillboardMatrix(baseWorld, triUp, triForward, cam) * localToWorldM;

			Vector3[] worldTri = triMatrix.MultiplyPoint3X4(tri).ToArray();
			DrawTri(worldTri, color);
			DrawLineThick(worldTri, 3, color?.Invert());
		}

		public static void DrawCone(Vector3 baseCenter, float radius, float height, Vector3 up, Color? color = null) =>
			DrawCone(
				Matrix4x4.TRS(
					baseCenter,
					Quaternion.FromToRotation(Vector3.up, up),
					new Vector3(radius, height, radius)
				),
				color
			);

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

		public static void DrawPolygon_OnTerrain(
			Vector3[] vertices, Color color = default, Color? outlineColor = null, Terrain terrain = null
		)
		{
			terrain ??= Terrain.activeTerrain;
			DrawPolygon(terrain.ProjectPathToTerrain(vertices), color, outlineColor);
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
			Matrix4x4 matrix, Color color = default, Color? outlineColor = null, Terrain terrain = null
		) => DrawPolygon_OnTerrain(QuadVertices(matrix), color, outlineColor, terrain);


		public static void DrawGrid_OnTerrain(
			float cellRows, float cellCols, Matrix4x4 matrix, float thickness = 1,
			Color color = default, Terrain terrain = null
		)
		{
			Vector2 cellSize = Vector2.one / new Vector2(cellRows, cellCols);
			Matrix4x4 cellScaleMatrix = Matrix4x4.Scale(cellSize.ToV3XZ().WithY(1));

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


		#region BILLBOARD

		/// <summary>
		///     BILLBOARD Matrix
		///     Al aplicarla como ultima transformacion orientara su forward a la camara, manteniendo su up
		/// </summary>
		public static Matrix4x4 BillboardMatrix(Vector3 position, Vector3 up, Vector3 forward, Camera camera)
		{
			Vector3 toCam = position - camera.transform.position;
			Vector3 camProjection = Vector3.ProjectOnPlane(toCam, up).normalized;
			float angle = Vector3.SignedAngle(forward, camProjection, up);
			return
				Matrix4x4.Translate(position)
				* Matrix4x4.Rotate(Quaternion.AngleAxis(angle, up))
				* Matrix4x4.Translate(-position);
		}

		#endregion

#endif
	}
}
