﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
	public static class VectorExtensions
	{
		#region RANDOM GENERATION
		
		// IN RANGE
		public static Vector2 GetRandomPos(Vector2 min, Vector2 max) =>
			new(
				Random.Range(min.x, max.x),
				Random.Range(min.y, max.y)
			);
		
		public static Vector3 GetRandomPos(Vector3 min, Vector3 max) =>
			new(
				Random.Range(min.x, max.x),
				Random.Range(min.y, max.y),
				Random.Range(min.z, max.z)
			);

		public static IEnumerable<Vector2> RandomPositions(int count, Vector2 min, Vector2 max) =>
			Enumerable.Range(0, count).Select(_ => GetRandomPos(min, max));
		
		public static IEnumerable<Vector3> RandomPositions(int count, Vector3 min, Vector3 max) =>
			Enumerable.Range(0, count).Select(_ => GetRandomPos(min, max));
		
		// INSIDE CIRCLE / SPHERE
		public static Vector2 RandomPositionInsideCircle(float radius = 1) => Random.insideUnitCircle * radius;
		public static Vector3 RandomPositionInsideSphere(float radius = 1) => Random.insideUnitSphere * radius;
		
		public static IEnumerable<Vector2> RandomPositionsInsideCircle(int count, float radius = 1) =>
			radius.ToFilledArray(count).Select(RandomPositionInsideCircle);
		
		public static IEnumerable<Vector3> RandomPositionsInsideSphere(int count, float radius = 1) =>
			radius.ToFilledArray(count).Select(RandomPositionInsideSphere);
		


		#endregion

		#region NORMALIZATION

		// Normalize points in [0,1] taking the min and max from the points
		public static Vector2[] Normalize(this Vector2[] points) =>
			points.Normalize(points.MinPosition(), points.MaxPosition());

		public static Vector3[] Normalize(this Vector3[] points) =>
			points.Normalize(points.MinPosition(), points.MaxPosition());

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


		public static Vector2 Abs(this Vector2 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y));
		public static Vector3 Abs(this Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

		#endregion


		#region CONVERSION

		public static Vector2Int ToVector2Int(this Vector2 v) => new((int)v.x, (int)v.y);
		public static Vector2 ToVector2(this Vector2Int v) => new(v.x, v.y);
		
		public static Vector3Int ToVector3Int(this Vector3 v) => new((int)v.x, (int)v.y, (int)v.z);
		public static Vector3 ToVector3(this Vector3Int v) => new(v.x, v.y, v.z);
		
		public static Point ToPoint(this Vector2 v) => new((int)v.x, (int)v.y);
		public static Point ToPoint(this Vector2Int v) => new(v.x, v.y);
		public static Size ToSize(this Vector2 v) => new((int)v.x, (int)v.y);
		public static Size ToSize(this Vector2Int v) => new(v.x, v.y);

		#endregion

		
		#region 3D to 2D

		public static Vector2 ToV2(this Vector3 v, bool XZplane = false) => XZplane ? v.ToV2xz() : v.ToV2xy();
		public static Vector2 ToV2xz(this Vector3 v) => new(v.x, v.z);
		public static Vector2 ToV2xy(this Vector3 v) => new(v.x, v.y);

		public static IEnumerable<Vector2> ToV2(this IEnumerable<Vector3> v, bool XZplane = false) =>
			XZplane ? v?.ToV2xz() : v?.ToV2xy();

		public static IEnumerable<Vector2> ToV2xz(this IEnumerable<Vector3> v) => v.Select(ToV2xz);
		public static IEnumerable<Vector2> ToV2xy(this IEnumerable<Vector3> v) => v.Select(ToV2xy);

		#endregion


		#region 2D to 3D

		public static Vector3 ToV3(this Vector2 v, bool XZplane = false) => XZplane ? v.ToV3xz() : v.ToV3xy();
		public static Vector3 ToV3xz(this Vector2 v) => new(v.x, 0, v.y);
		public static Vector3 ToV3xy(this Vector2 v) => new(v.x, v.y, 0);

		public static IEnumerable<Vector3> ToV3(this IEnumerable<Vector2> v, bool XZplane = false) =>
			XZplane ? v?.ToV3xz() : v?.ToV3xy();

		public static IEnumerable<Vector3> ToV3xz(this IEnumerable<Vector2> v) => v.Select(ToV3xz);
		public static IEnumerable<Vector3> ToV3xy(this IEnumerable<Vector2> v) => v.Select(ToV3xy);


		public static Vector3 WithX(this Vector3 v, float x) => v = new Vector3(x, v.y, v.z);
		public static Vector3 WithY(this Vector3 v, float y) => v = new Vector3(v.x, y, v.z);
		public static Vector3 WithZ(this Vector3 v, float z) => v = new Vector3(v.x, v.y, z);

		#endregion


		#region to 4D for Matrix Transformations

		public static Vector4 ToVector4xy(this Vector2 v) => new(v.x, v.y, 0, 0);
		public static Vector4 ToVector4xz(this Vector2 v) => new(v.x, 0, v.y, 0);
		public static Vector4 ToVector4(this Vector3 v) => new(v.x, v.y, v.z, 0);

		public static Vector2 ToV2xy(this Vector4 v) => new(v.x, v.y);
		public static Vector2 ToV2xz(this Vector4 v) => new(v.x, v.z);

		#endregion


		#region MATRIX

		public static void ApplyLocalMatrix(this Transform transform, Matrix4x4 matrix)
		{
			transform.localPosition = matrix.GetPosition();
			transform.localRotation = matrix.rotation;
			transform.localScale = matrix.lossyScale;
		}

		public static void ApplyWorldMatrix(this Transform transform, Matrix4x4 matrix)
		{
			transform.SetPositionAndRotation(matrix.GetPosition(), matrix.rotation);
			transform.SetGlobalScale(matrix.lossyScale);
		}

		// Apply to Multiple Points
		public static IEnumerable<Vector3> MultiplyPoint3x4(this Matrix4x4 matrix, IEnumerable<Vector3> points) =>
			points.Select(p => matrix.MultiplyPoint3x4(p));

		public static IEnumerable<Vector3> MultiplyPoint3x4(this Matrix4x4 matrix, IEnumerable<Vector2> points) =>
			points.Select(p => matrix.MultiplyPoint3x4(p));

		#endregion


		#region ROTATION

		public static Vector3 Rotate(this Vector3 v, Quaternion q) => q * v;
		public static Vector2 Rotate(this Vector2 v, Quaternion q) => q * v;

		public static Vector3 Rotate(this Vector3 v, float angle, Vector3 axis) =>
			Quaternion.AngleAxis(angle, axis) * v;

		public static Vector2 Rotate(this Vector2 v, float angle) =>
			Quaternion.AngleAxis(angle, Vector3.forward) * v;

		public static Vector2 Rotate(this Vector2 v, float angle, Vector2 center) =>
			Quaternion.AngleAxis(angle, Vector3.forward) * (v - center) + center.ToV3xy();

		// ARRAY
		public static IEnumerable<Vector3> Rotate(this IEnumerable<Vector3> points, Quaternion q) =>
			points.Select(p => p.Rotate(q));

		public static IEnumerable<Vector2> Rotate(this IEnumerable<Vector2> points, Quaternion q) =>
			points.Select(p => p.Rotate(q));

		public static IEnumerable<Vector3> Rotate(this IEnumerable<Vector3> points, float angle, Vector3 axis) =>
			points.Select(p => p.Rotate(angle, axis));

		public static IEnumerable<Vector2> Rotate(this IEnumerable<Vector2> points, float angle) =>
			points.Select(p => p.Rotate(angle));

		public static IEnumerable<Vector2> Rotate(this IEnumerable<Vector2> points, float angle, Vector2 center) =>
			points.Select(p => p.Rotate(angle, center));

		#endregion


		#region SCALE

		/// <summary>
		///     Sets the Global Scale of the source Transform.
		/// </summary>
		public static Transform SetGlobalScale(this Transform transform, Vector3 targetGlobalScale)
		{
			transform.localScale =
				targetGlobalScale.ScaleBy(transform.lossyScale.Inverse()).ScaleBy(transform.localScale);
			return transform;
		}

		/// <summary>
		///     Immutably returns the result of the source vector multiplied with
		///     another vector component-wise.
		/// </summary>
		public static Vector2 ScaleBy(this Vector2 pos, Vector2 scaleFactor) => Vector2.Scale(pos, scaleFactor);

		/// <summary>
		///     Immutably returns the result of the source vector multiplied with
		///     another vector component-wise.
		/// </summary>
		public static Vector3 ScaleBy(this Vector3 pos, Vector3 scaleFactor) => Vector3.Scale(pos, scaleFactor);

		/// <summary>
		///     Safe Inversion (1 / vector)
		///     Si un componente es 0 lo mantiene a 0
		/// </summary>
		public static Vector2 Inverse(this Vector2 v) => new(
			Mathf.Abs(v.x) < Mathf.Epsilon ? 0 : 1 / v.x,
			Mathf.Abs(v.y) < Mathf.Epsilon ? 0 : 1 / v.y
		);

		public static Vector3 Inverse(this Vector3 v) => new(
			Mathf.Abs(v.x) < Mathf.Epsilon ? 0 : 1 / v.x,
			Mathf.Abs(v.y) < Mathf.Epsilon ? 0 : 1 / v.y,
			Mathf.Abs(v.z) < Mathf.Epsilon ? 0 : 1 / v.z
		);

		#endregion


		#region POW

		/// <summary>
		///     Raise each component of the source Vector2 to the specified power.
		/// </summary>
		public static Vector2 Pow(this Vector2 pos, float exponent)
			=> new(Mathf.Pow(pos.x, exponent), Mathf.Pow(pos.y, exponent));

		/// <summary>
		///     Raise each component of the source Vector3 to the specified power.
		/// </summary>
		public static Vector3 Pow(this Vector3 pos, float exponent)
			=> new(Mathf.Pow(pos.x, exponent), Mathf.Pow(pos.y, exponent), Mathf.Pow(pos.z, exponent));

		#endregion


		#region SORTING

		// Ordena los puntos por angulo respecto a un centroide
		public static Vector2[] SortByAngle(this IEnumerable<Vector2> points, Vector2 centroid) =>
			points.OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x)).ToArray();

		// Ordena los puntos por angulo respecto a un centroide, con un eje de giro
		// Utiliza el primer punto como referencia de ángulo 0, por lo que siempre sera el primero
		public static Vector3[] SortByAngle(this IEnumerable<Vector3> points, Vector3 centroid, Vector3 axis)
		{
			points = points as Vector3[] ?? points?.ToArray();
			if (points.IsNullOrEmpty()) return null;
			Vector3 refPoint = points.First();
			return points.OrderBy(p => Vector3.SignedAngle(refPoint - centroid, p - centroid, axis)).ToArray();
		}

		#endregion


		#region BOUNDING BOX

		public static Bounds GetBoundingBox(this IEnumerable<Vector3> points)
		{
			points = points as Vector3[] ?? points?.ToArray();
			if (points.IsNullOrEmpty()) return new Bounds();
			Vector3 min = points.MinPosition(), max = points.MaxPosition();
			Vector3 size = max - min;
			return new Bounds(min + size / 2, size);
		}

		public static Bounds GetBoundingBox(this IEnumerable<Vector2> points) =>
			points.ToV3().ToArray().GetBoundingBox();


		// MAX / MIN from a collection of points => Can build AABB
		public static Vector2 MinPosition(this IEnumerable<Vector2> points) =>
			points.Aggregate(Vector2.positiveInfinity, Vector2.Min);

		public static Vector2 MaxPosition(this IEnumerable<Vector2> points) =>
			points.Aggregate(Vector2.negativeInfinity, Vector2.Max);

		public static Vector3 MinPosition(this IEnumerable<Vector3> points) =>
			points.Aggregate(Vector3.positiveInfinity, Vector3.Min);

		public static Vector3 MaxPosition(this IEnumerable<Vector3> points) =>
			points.Aggregate(Vector3.negativeInfinity, Vector3.Max);


		// CENTROIDE (Punto Medio)
		public static Vector2 Center(this IEnumerable<Vector2> points)
		{
			IEnumerable<Vector2> pointsEnumerable = points as Vector2[] ?? points?.ToArray();
			if (pointsEnumerable.IsNullOrEmpty()) return default;
			return pointsEnumerable.Aggregate(Vector2.zero, (sum, p) => sum + p) / pointsEnumerable.Count();
		}

		public static Vector3 Center(this IEnumerable<Vector3> points)
		{
			IEnumerable<Vector3> pointsEnumerable = points as Vector3[] ?? points?.ToArray();
			if (pointsEnumerable.IsNullOrEmpty()) return default;
			return pointsEnumerable.Aggregate(Vector3.zero, (sum, p) => sum + p) / pointsEnumerable.Count();
		}

		public static Matrix4x4 LocalToBoundsMatrix(this Bounds bounds) => 
			Matrix4x4.TRS(bounds.min, Quaternion.identity, bounds.size);

		#endregion
	}
}
