using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.MouseInput;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public struct Bounds2D
	{
		public enum Side { Left, Right, Top, Bottom }

		public Vector2 min;
		public Vector2 max;
		public Vector2 Center => (min + max) / 2;

		public float Width => max.x - min.x;
		public float Height => max.y - min.y;
		public Vector2 Size => new(Width, Height);
		public Vector2 Extent => Size / 2;

		public Vector2 BL => min;
		public Vector2 BR => new(max.x, min.y);
		public Vector2 TL => new(min.x, max.y);
		public Vector2 TR => max;
		public Vector2[] Corners => new[] { BL, BR, TR, TL }; // CCW

		public bool IsNormalized => min == Vector2.zero && max == Vector2.one;

		public static Bounds2D NormalizedBounds => new(Vector2.zero, Vector2.one);

		public Bounds2D(Vector2 min, Vector2 max)
		{
			this.min = min;
			this.max = max;
		}

		public Bounds2D(Vector2[] pointsInsideBound)
		{
			min = pointsInsideBound.MinPosition();
			max = pointsInsideBound.MaxPosition();
		}

		public Vector2 TransformNormalized(Vector2 normalized) => min + normalized * Size;

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
		public Vector2[] CropPolygon(Vector2[] polygonVertices)
		{
			List<Vector2> croppedPolygon = new();
			for (var i = 0; i < polygonVertices.Length; i++)
			{
				Vector2 vertex = polygonVertices[i];

				// Si está dentro, conservamos el vertice
				if (Contains(vertex))
				{
					croppedPolygon.Add(vertex);
					continue;
				}

				// Si esta fuera de la Bounding Box, buscamos la interseccion de sus aristas con la BB
				Vector2 prev = polygonVertices[(i - 1 + polygonVertices.Length) % polygonVertices.Length];
				Vector2 next = polygonVertices[(i + 1) % polygonVertices.Length];
				Vector2[] i1 = Intersections_Segment(prev, vertex).ToArray();
				Vector2[] i2 = Intersections_Segment(vertex, next).ToArray();

				// Añadimos las intersecciones en vez del vertice si las hay
				croppedPolygon.AddRange(i1);
				croppedPolygon.AddRange(i2);
			}

			return croppedPolygon.ToArray();
		}

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
		public static implicit operator Bounds2D(Bounds bounds) =>
			new(bounds.min.ToV2xz(), bounds.max.ToV2xz());

		public static implicit operator Bounds(Bounds2D bounds) =>
			new(bounds.Center.ToV3xz(), bounds.Extent.ToV3xz());

		public Bounds2D ApplyTransform_XY(Matrix4x4 matrix) =>
			new(
				matrix.MultiplyPoint3x4(min.ToV3xy()).ToV2xy(),
				matrix.MultiplyPoint3x4(max.ToV3xy()).ToV2xy()
			);

		public Bounds2D ApplyTransform_XZ(Matrix4x4 matrix) =>
			new(
				matrix.MultiplyPoint3x4(min.ToV3xz()).ToV2xz(),
				matrix.MultiplyPoint3x4(max.ToV3xz()).ToV2xz()
			);

		#endregion
	}
}
