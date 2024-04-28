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
		
		public static bool IsNormalized(this Vector2 p) => p.x is >= 0 and <= 1 && p.y is >= 0 and <= 1;

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
	}
}
