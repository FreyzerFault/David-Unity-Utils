using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugUtils;
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

		public AABB_2D(Polygon polygon) : this(polygon.vertices)
		{
		}

		public AABB_2D(Bounds bounds3D, bool XZplane = true)
			: this(
				XZplane ? bounds3D.min.ToV2xz() : bounds3D.min.ToV2xy(),
				XZplane ? bounds3D.max.ToV2xz() : bounds3D.max.ToV2xy()
			)
		{
		}

		#region To 3D

		public Bounds To3D(bool XZplane = true, float missingCoord = 1f) => new(
			Center.ToV3(XZplane),
			Size.ToV3(XZplane) + (XZplane ? Vector3.up : Vector3.forward) * missingCoord
		);

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
		public Matrix4x4 LocalToBoundsMatrix(bool XZplane = true) =>
			Matrix4x4.TRS(min, XZplane ? RotationToXZplane : Quaternion.identity, Size.ToV3xy().WithZ(1));

		public Matrix4x4 BoundsToLocalMatrix(bool XZplane = true) =>
			Matrix4x4.Scale(
				Vector2.Max(Vector2.one * 0.1f, Size).Inverse().ToV3(XZplane) + (XZplane ? Vector3.up : Vector3.forward)
			)
			* Matrix4x4.Translate(-min.ToV3(XZplane));

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
		///     Test del Punto P dentro de un Segmento (cualquiera de los 4 lados).
		///     Side puede ser Left, Right, Top o Bottom
		///     Ejemplo: Side = Right => Segmento br,tr
		/// </summary>
		public bool PointOnBorder(Vector2 p, out Side? side)
		{
			side = null;
			bool inBottom = GeometryUtils.PointOnSegment(p, BL, BR);
			bool inRight = GeometryUtils.PointOnSegment(p, BR, TR);
			bool inTop = GeometryUtils.PointOnSegment(p, TR, TL);
			bool inLeft = GeometryUtils.PointOnSegment(p, TL, BL);
			if (inBottom) side = Side.Bottom;
			if (inRight) side = Side.Right;
			if (inTop) side = Side.Top;
			if (inLeft) side = Side.Left;
			return inBottom || inRight || inTop || inLeft;
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
			Vector2[] cornersInside = Corners.Where(c => polygon.Contains_RayCast(c)).ToArray();
			croppedVertices.AddRange(cornersInside);

			// Cortamos las aristas del poligono con la Bounding Box
			for (var i = 0; i < polygon.VextexCount; i++)
			{
				Vector2 vertex = polygon.vertices[i];

				// Si está dentro, conservamos el vertice
				if (Contains(vertex)) croppedVertices.Add(vertex);

				// Si esta fuera de la Bounding Box, buscamos la interseccion de sus aristas con la BB
				Vector2 next = polygon.vertices[(i + 1) % polygon.VextexCount];
				Vector2[] i2 = Intersections_Segment(vertex, next).ToArray();

				// Añadimos las intersecciones en vez del vertice si las hay
				croppedVertices.AddRange(i2);
			}

			polygon = new Polygon(croppedVertices);
			polygon.vertices = polygon.vertices.SortByAngle(polygon.centroid);

			return polygon;
		}

		public Vector2[] CropPolygon(Vector2[] polygonVertices) => CropPolygon(new Polygon(polygonVertices)).vertices;

		#endregion

		public Vector2 Normalize(Vector2 p) => (p - min) / Size;

		#region MOUSE PICKING

		public bool MouseInBounds_XY() => Contains(MouseInputUtils.MouseWorldPosition.ToV2xy());
		public bool MouseInBounds_XZ() => Contains(MouseInputUtils.MouseWorldPosition.ToV2xz());
		public Vector2 NormalizeMousePosition_XY() => Normalize(MouseInputUtils.MouseWorldPosition.ToV2xy());
		public Vector2 NormalizeMousePosition_XZ() => Normalize(MouseInputUtils.MouseWorldPosition.ToV2xz());

		public Vector2 NormalizeMousePosition_InScene_XY() => Normalize(MouseInputUtils.MouseWorldPosition_InScene_XY);
		public Vector2 NormalizeMousePosition_InScene_XZ() => Normalize(MouseInputUtils.MouseWorldPosition_InScene_XZ);

		#endregion


		#region OPERATORS

		// CONVERSION OPERATOR Bounds <--> Bounds2D
		public static implicit operator AABB_2D(Bounds bounds) =>
			new(bounds.min.ToV2xz(), bounds.max.ToV2xz());

		public static implicit operator Bounds(AABB_2D aabb) =>
			new(aabb.Center.ToV3xz(), aabb.Extent.ToV3xz());

		public AABB_2D ApplyTransform_XY(Matrix4x4 matrix) =>
			new(
				matrix.MultiplyPoint3x4(min.ToV3xy()).ToV2xy(),
				matrix.MultiplyPoint3x4(max.ToV3xy()).ToV2xy()
			);

		public AABB_2D ApplyTransform_XZ(Matrix4x4 matrix) =>
			new(
				matrix.MultiplyPoint3x4(min.ToV3xz()).ToV2xz(),
				matrix.MultiplyPoint3x4(max.ToV3xz()).ToV2xz()
			);

		#endregion


		#region GIZMOS

		public void DrawGizmos(Matrix4x4 matrix, float thickness = 1, Color color = default) =>
			GizmosExtensions.DrawQuadWire(
				matrix * Matrix4x4.TRS(min, Quaternion.identity, Size),
				thickness,
				color
			);

		#endregion
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
