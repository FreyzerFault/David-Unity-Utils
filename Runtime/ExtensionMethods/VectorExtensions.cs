using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
	public static class VectorExtensions
	{
		#region RANDOM GENERATION

		public static Vector3 GetRandomPos(Vector3 min, Vector3 max) =>
			new(
				Random.Range(min.x, max.x),
				Random.Range(min.y, max.y),
				Random.Range(min.z, max.z)
			);

		#endregion

		#region NORMALIZATION

		// Normalize points in [0,1] taking the min and max from the points
		public static Vector2[] Normalize(this Vector2[] points) =>
			points.Normalize(points.Min(), points.Max());

		public static Vector3[] Normalize(this Vector3[] points) =>
			points.Normalize(points.Min(), points.Max());

		// Taking min and max as parameters
		public static Vector2[] Normalize(this Vector2[] points, Vector2 min, Vector2 max) =>
			points.Select(p => p.Normalize(min, max)).ToArray();

		public static Vector3[] Normalize(this Vector3[] points, Vector3 min, Vector3 max) =>
			points.Select(p => p.Normalize(min, max)).ToArray();

		// Normalize a Point in [0,1]
		public static Vector2 Normalize(this Vector2 p, Vector2 min, Vector2 max) => new(
			Mathf.InverseLerp(min.x, max.x, p.x),
			Mathf.InverseLerp(min.y, max.y, p.y)
		);

		public static Vector3 Normalize(this Vector3 p, Vector3 min, Vector3 max) => new(
			Mathf.InverseLerp(min.x, max.x, p.x),
			Mathf.InverseLerp(min.y, max.y, p.y),
			Mathf.InverseLerp(min.z, max.z, p.z)
		);

		public static bool IsIn01(this Vector2 p) => p.x is >= 0 and <= 1 && p.y is >= 0 and <= 1;

		public static Vector2 Clamp01(this Vector2 p) => new(Mathf.Clamp01(p.x), Mathf.Clamp01(p.y));

		public static Vector3 Clamp01(this Vector3 p) =>
			new(Mathf.Clamp01(p.x), Mathf.Clamp01(p.y), Mathf.Clamp01(p.z));

		public static Vector2 Clamp(this Vector2 p, Vector2 min, Vector2 max) => new(
			Mathf.Clamp(p.x, min.x, max.x),
			Mathf.Clamp(p.y, min.y, max.y)
		);

		public static Vector3 Clamp(this Vector3 p, Vector3 min, Vector3 max) => new(
			Mathf.Clamp(p.x, min.x, max.x),
			Mathf.Clamp(p.y, min.y, max.y),
			Mathf.Clamp(p.z, min.z, max.z)
		);

		#endregion

		#region 3D to 2D

		public static Vector2 ToVector2xz(this Vector3 v) => new(v.x, v.z);
		public static Vector2 ToVector2xy(this Vector3 v) => new(v.x, v.y);

		public static Vector3 WithX(this Vector3 v, float x) => v = new Vector3(x, v.y, v.z);
		public static Vector3 WithY(this Vector3 v, float y) => v = new Vector3(v.x, y, v.z);
		public static Vector3 WithZ(this Vector3 v, float z) => v = new Vector3(v.x, v.y, z);

		#endregion

		#region 2D to 3D

		public static Vector3 ToVector3xz(this Vector2 v) => new(v.x, 0, v.y);
		public static Vector3 ToVector3xy(this Vector2 v) => new(v.x, v.y, 0);

		#endregion

		#region to 4D for Matrix Transformations

		public static Vector4 ToVector4xy(this Vector2 v) => new(v.x, v.y, 0, 0);
		public static Vector4 ToVector4xz(this Vector2 v) => new(v.x, 0, v.y, 0);
		public static Vector4 ToVector4(this Vector3 v) => new(v.x, v.y, v.z, 0);

		public static Vector2 ToVector2xy(this Vector4 v) => new(v.x, v.y);
		public static Vector2 ToVector2xz(this Vector4 v) => new(v.x, v.z);

		#endregion

		#region SORTING

		// Ordena los puntos por angulo respecto a un centroide
		public static Vector2[] SortByAngle(this IEnumerable<Vector2> points, Vector2 centroid) =>
			points.OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x)).ToArray();

		// Ordena los puntos por angulo respecto a un centroide, con un eje de giro
		// Utiliza el primer punto como referencia de ángulo 0, por lo que siempre sera el primero
		public static Vector3[] SortByAngle(this IEnumerable<Vector3> points, Vector3 centroid, Vector3 axis)
		{
			Vector3 refPoint = points.First();
			return points.OrderBy(p => Vector3.SignedAngle(refPoint - centroid, p - centroid, axis)).ToArray();
		}

		#endregion
	}
}
