using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry.Bounding_Box
{
	public class OBB_2D
	{
		public Vector2 min;
		public Vector2 max;
		public Vector2 up;

		public Vector2 Diagonal => max - min;

		// Angulo girado respecto a su AABB homólogo 
		public float Angle => Vector2.SignedAngle(Vector2.up, up);
		public Quaternion Rotation => Quaternion.AngleAxis(Angle, Vector3.forward);

		// AABB resultado de un giro
		public AABB_2D AABB_Rotated => new(min, max.Rotate(-Angle, min));

		// CCW =>  [BL, BR, TR, TL]
		public Vector2[] Corners => AABB_Rotated.Corners.Rotate(Angle).ToArray();

		// AABB que contiene al OBB
		public AABB_2D AABB_outer => new(Corners);

		public OBB_2D(Vector2 min, Vector2 max, Vector2 up)
		{
			this.min = min;
			this.max = max;
			this.up = up;
		}

		public OBB_2D(Vector2[] points, Vector2 up)
		{
			// Rotamos los puntos para que el up sea Vector2.up
			// Calculamos el AABB y lo rotamos de vuelta a su up original
			this.up = up;
			Vector2 centroid = points.Center();
			var aabb = new AABB_2D(points.Rotate(-Angle, centroid));
			min = aabb.min.Rotate(Angle, centroid);
			max = aabb.max.Rotate(Angle, centroid);
		}

		public OBB_2D(Polygon polygon, Vector2 up) : this(polygon.vertices, up)
		{
		}

		/// <summary>
		///     Contains a point in the OBB.
		///     Rotamos el OBB y el punto para reducir el cálculo en un AABB
		/// </summary>
		public bool Contains(Vector2 point) => AABB_Rotated.Contains(point.Rotate(-Angle, min));

		public void DrawGizmos(Matrix4x4 matrix, Color color, float thickness = 1f) =>
			GizmosExtensions.DrawQuadWire(
				matrix * Matrix4x4.TRS(min, Rotation, AABB_Rotated.Size),
				color: color,
				thickness: thickness
			);
	}
}
