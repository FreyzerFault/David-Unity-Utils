using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;

namespace DavidUtils.Geometry.Bounding_Box
{
	[Serializable]
	public struct AABB_2D
	{
		public enum Side { Left, Right, Top, Bottom }

		public Vector2 min;
		public Vector2 max;
		public Vector2 Center => (min + max) / 2;

		public float Width => max.x - min.x;
		public float Height => max.y - min.y;
		public Vector2 Extent => Size / 2;
		public Vector2 Size
		{
			get => new(Width, Height);
			set
			{
				Vector2 halfSize = value / 2;
				Vector2 center = Center;
				min = center - halfSize;
				max = center + halfSize;
			}
		}

		public Vector2 BL => min;
		public Vector2 BR => new(max.x, min.y);
		public Vector2 TL => new(min.x, max.y);
		public Vector2 TR => max;
		public Vector2[] Corners => new[] { BL, BR, TR, TL }; // CCW

		public bool IsNormalized => min == Vector2.zero && max == Vector2.one;

		public static AABB_2D NormalizedAABB => new(Vector2.zero, Vector2.one);

		public AABB_2D(Vector2 min, Vector2 max)
		{
			this.min = min;
			this.max = max;
		}

		public AABB_2D(IEnumerable<Vector2> pointsInsideBound)
		{
			IEnumerable<Vector2> pointsEnumerable = pointsInsideBound as Vector2[] ?? pointsInsideBound.ToArray();
			min = pointsEnumerable.MinPosition();
			max = pointsEnumerable.MaxPosition();
		}

		public AABB_2D(Polygon polygon) : this(polygon.Vertices)
		{
		}

		public AABB_2D(Bounds bounds3D, bool isXZplane = true)
			: this(
				isXZplane ? bounds3D.min.ToV2XZ() : bounds3D.min.ToV2XY(),
				isXZplane ? bounds3D.max.ToV2XZ() : bounds3D.max.ToV2XY()
			)
		{
		}

		#region To 3D

		public Bounds To3D(bool isXZplane = true)
		{
			Vector3 center = Center.ToV3();
			Vector3 size = new Vector3( 
				Size.x,
				isXZplane ? 1 : Size.y,
				isXZplane ? Size.y : 1
			);
			return new Bounds(center, size);
		}

		#endregion


		#region AABB CONVERSIONS

		/// <summary>
		///		Upscale the AABB to match the Aspect Ratio given
		///		It resize it to 1:1 and then apply the aspect ratio => w:h -> 1:1 -> w':h'
		/// </summary>
		public void UpscaleToAspectRatio(Vector2 aspectRatio) =>
			Size = Size.UpscaleToAspectRatio(aspectRatio);
		
		/// <summary>
		///		Upscale the AABB to match the Aspect Ratio of the Size given
		///		It resize it to 1:1 and then apply the aspect ratio => w:h -> 1:1 -> w':h'
		/// </summary>
		public void UpscaleToMatchAspectRatio(Vector2 targetSize) =>
			Size = Size.UpscaleToMathSizeAspectRatio(targetSize);

		/// <summary>
		///		Upscale the AABB to match the Aspect Ratio of the AABB given
		///		It resize it to 1:1 and then apply the aspect ratio => w:h -> 1:1 -> w':h'
		/// </summary>
		public void UpscaleToMatchAspectRatio(AABB_2D targetAABB) =>
			UpscaleToMatchAspectRatio(targetAABB.Size);

		#endregion
		

		#region SPACE CONVERSIONS

		public static Quaternion RotationToXZplane => Quaternion.AngleAxis(90, Vector3.right);
		public static Quaternion RotationToXYplane => Quaternion.AngleAxis(-90, Vector3.right);

		public Vector2 NormalizedToBoundsSpace(Vector2 point) => min + point * Size;

		public Vector2 BoundsToNormalizedSpace(Vector2 point) =>
			(point - min) * Vector2.Max(Vector2.one * 0.01f, Size).Inverse();

		/// <summary>
		///     Convierte un punto local [0,1] a un punto en el espacio de la Bounding Box
		///     Traslada el punto al Min. Y escala al tamaño de la Bounding Box
		/// </summary>
		public Matrix4x4 LocalToBoundsMatrix(bool isXZplane = true) =>
			Matrix4x4.TRS(min, isXZplane ? RotationToXZplane : Quaternion.identity, Size.ToV3XY().WithZ(1));

		public Matrix4x4 BoundsToLocalMatrix(bool isXZplane = true) =>
			Matrix4x4.Scale(
				Vector2.Max(Vector2.one * 0.1f, Size).Inverse().ToV3(isXZplane) + (isXZplane ? Vector3.up : Vector3.forward)
			)
			* Matrix4x4.Translate(-min.ToV3(isXZplane));

		#endregion


		#region TEST INSIDE

		public readonly bool Contains(Vector2 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
		public bool OutOfBounds(Vector2 p) => !Contains(p);

		#endregion


		#region CORNERS

		/// <summary>
		///     Esquina de la Bounding Box que comparte ambos lados
		/// </summary>
		/// <returns>Posicion de la Esquina</returns>
		public Vector2 GetCorner(Side side0, Side side1) => side0 switch
		{
			Side.Left => side1 == Side.Bottom ? BL : TL,
			Side.Right => side1 == Side.Bottom ? BR : TR,
			Side.Top => side1 == Side.Left ? TL : TR,
			Side.Bottom => side1 == Side.Left ? BL : BR,
			_ => Vector2.zero
		};

		#endregion


		#region COLLISIONS

		/// <summary>
		///     Test Punto P pertenece a un borde (x o y == 0 o 1)
		///     Side puede ser Left, Right, Top o Bottom
		///     Ejemplo: Side = Right => Segmento br,tr
		/// </summary>
		public bool PointOnBorder(Vector2 p, out Side? side)
		{
			side = null;

			// Normalizamos el punto a [0,1] si el AABB no es [0,0 - 1,1]
			if (!IsNormalized) p = BoundsToLocalMatrix().MultiplyPoint3x4(p);
		
			float epsilon = GeometryUtils.Epsilon;

			if (Mathf.Abs(p.x) < epsilon) side = Side.Left;
			else if (Mathf.Abs(p.x - 1) < epsilon) side = Side.Right;
			else if (Mathf.Abs(p.y) < epsilon) side = Side.Bottom;
			else if (Mathf.Abs(p.y - 1) < epsilon) side = Side.Top;

			return side != null;
		}

		#endregion


		#region INTERSECTIONS

		/// <summary>
		///     Interseccion del Segmento AB con los 4 lados del rectangulo
		///     Si hay más de una (2 maximo) se ordenan de A a B
		/// </summary>
		/// <returns>Intersecciones con los bordes ordenadas de A a B (maximo 2)</returns>
		public IEnumerable<Vector2> Intersections_Segment(Vector2 a, Vector2 b)
		{
			Vector2? ib = GeometryUtils.IntersectionSegmentSegment(a, b, BL, BR);
			Vector2? ir = GeometryUtils.IntersectionSegmentSegment(a, b, BR, TR);
			Vector2? it = GeometryUtils.IntersectionSegmentSegment(a, b, TL, TR);
			Vector2? il = GeometryUtils.IntersectionSegmentSegment(a, b, BL, TL);

			Vector2?[] intersections = { ib, ir, it, il };
			return intersections
				.Where(i => i.HasValue)
				.Select(i => i.Value)
				.OrderBy(i => Vector2.Distance(i, a));
		}

		/// <summary>
		///     Interseccion del Rayo A -> dir con los 4 lados del rectangulo
		///     Si hay más de una (2 maximo) se ordenan de A a B
		/// </summary>
		/// <returns>Intersecciones con los bordes ordenadas de A a B (maximo 2)</returns>
		public IEnumerable<Vector2> Intersections_Ray(Vector2 p, Vector2 dir)
		{
			Vector2? ib = GeometryUtils.IntersectionRaySegment(p, dir, BL, BR);
			Vector2? ir = GeometryUtils.IntersectionRaySegment(p, dir, BR, TR);
			Vector2? it = GeometryUtils.IntersectionRaySegment(p, dir, TR, TL);
			Vector2? il = GeometryUtils.IntersectionRaySegment(p, dir, TL, BL);

			Vector2?[] intersections = { ib, ir, it, il };
			return intersections
				.Where(i => i.HasValue)
				.Select(i => i.Value)
				.OrderBy(i => (i - p).sqrMagnitude);
		}


		public IEnumerable<Vector2> Intersections_Line(Vector2 a, Vector2 b)
		{
			Vector2? ib = GeometryUtils.IntersectionLineSegment(a, b, BL, BR);
			Vector2? ir = GeometryUtils.IntersectionLineSegment(a, b, BR, TR);
			Vector2? it = GeometryUtils.IntersectionLineSegment(a, b, TL, TR);
			Vector2? il = GeometryUtils.IntersectionLineSegment(a, b, BL, TL);

			Vector2?[] intersections = { ib, ir, it, il };
			return intersections
				.Where(i => i.HasValue)
				.Select(i => i.Value)
				.OrderBy(i => Vector2.Distance(i, a));
		}

		/// <summary>
		///     Convert a Polygon to stay inside the Bounding Box
		///     Add the intersections of the polygon edges with the BB
		///     Remove outer vertices
		/// </summary>
		public Polygon CropPolygon(Polygon polygon)
		{
			List<Vector2> croppedVertices = new();

			// Añadimos las esquinas que esten dentro del poligono
			Vector2[] cornersInside = Corners.Where(polygon.Contains_RayCast).ToArray();
			croppedVertices.AddRange(cornersInside);

			// Cortamos las aristas del poligono con la Bounding Box
			for (var i = 0; i < polygon.VertexCount; i++)
			{
				Vector2 vertex = polygon.Vertices[i];

				// Si está dentro, conservamos el vertice
				if (Contains(vertex)) croppedVertices.Add(vertex);

				// Si esta fuera de la Bounding Box, buscamos la interseccion de sus aristas con la BB
				Vector2 next = polygon.Vertices[(i + 1) % polygon.VertexCount];
				Vector2[] intersections = Intersections_Segment(vertex, next).ToArray();

				// Añadimos las intersecciones en vez del vertice si las hay
				croppedVertices.AddRange(intersections.Where(inters => croppedVertices.All(v => v != inters)));
			}

			return new Polygon(croppedVertices.SortByAngle(croppedVertices.Center()).ToArray());
		}

		public Vector2[] CropPolygon(Vector2[] polygonVertices) => CropPolygon(new Polygon(polygonVertices)).Vertices;

		#endregion

		public Vector2 Normalize(Vector2 p) => (p - min) / Size;

		#region MOUSE PICKING

		public bool MouseInBounds_XY() => Contains(MouseInputUtils.MouseWorldPosition.ToV2XY());
		public bool MouseInBounds_XZ() => Contains(MouseInputUtils.MouseWorldPosition.ToV2XZ());
		public Vector2 NormalizeMousePosition_XY() => Normalize(MouseInputUtils.MouseWorldPosition.ToV2XY());
		public Vector2 NormalizeMousePosition_XZ() => Normalize(MouseInputUtils.MouseWorldPosition.ToV2XZ());

		// Posicion del Mouse en el Editor de Escena
		#if UNITY_EDITOR
		public Vector2 NormalizeMousePosition_InScene_XY() => Normalize(MouseInputUtils.MouseWorldPosition_InScene_XY);
		public Vector2 NormalizeMousePosition_InScene_XZ() => Normalize(MouseInputUtils.MouseWorldPosition_InScene_XZ);
		#endif

		#endregion


		#region OPERATORS

		// CONVERSION OPERATOR Bounds <--> Bounds2D
		public static implicit operator AABB_2D(Bounds bounds) =>
			new(bounds.min.ToV2XZ(), bounds.max.ToV2XZ());

		public static implicit operator Bounds(AABB_2D aabb) =>
			new(aabb.Center.ToV3XZ(), aabb.Extent.ToV3XZ());

		public AABB_2D ApplyTransform_XY(Matrix4x4 matrix) =>
			new(
				matrix.MultiplyPoint3x4(min.ToV3XY()).ToV2XY(),
				matrix.MultiplyPoint3x4(max.ToV3XY()).ToV2XY()
			);

		public AABB_2D ApplyTransform_XZ(Matrix4x4 matrix) =>
			new(
				matrix.MultiplyPoint3x4(min.ToV3XZ()).ToV2XZ(),
				matrix.MultiplyPoint3x4(max.ToV3XZ()).ToV2XZ()
			);

		#endregion


		#region DEBUG
		
		#if UNITY_EDITOR

		public void DrawGizmos(Matrix4x4 matrix, float thickness = 1, Color color = default) =>
			GizmosExtensions.DrawQuadWire(
				matrix * Matrix4x4.TRS(min, Quaternion.identity, Size),
				thickness,
				color
			);
		
		#endif

		#endregion


		public override string ToString() => $"AABB_2D: [Min {min}, Max {max}] | Size {Size}";
	}

	#region EXTENSIONS

	public static class Bounds2DExtensions
	{
		public static AABB_2D GetBoundingBox(this Vector2[] points) => new(points);

		/// <summary>
		///     Devuelve un punto aleatorio dentro de los limites de la Bounding Box.
		///     Respetando un offset para que objetos extensos no sobresalgan.
		/// </summary>
		public static Vector2 GetRandomPointInBounds(this AABB_2D aabb, Vector2 offsetPadding) =>
			VectorExtensions.GetRandomPos(aabb.min + offsetPadding, aabb.max - offsetPadding);
	}

	#endregion
}
