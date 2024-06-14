﻿using System;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public class Edge
	{
		public Vector2 begin;
		public Vector2 end;

		public Vector2[] Vertices => new[] { begin, end };


		public Edge(Vector2 a, Vector2 b)
		{
			begin = a;
			end = b;
		}

		// Mediana (Punto medio)
		public Vector2 Median => (begin + end) / 2;

		// Vector [begin -> end]
		public Vector2 Vector => end - begin;
		public Vector2 Dir => Vector.normalized;


		#region MEDIATRIZ

		public Vector2 MediatrizLeftDir => Vector2.Perpendicular(Dir); // (90º CCW) => [-y,x]
		public Vector2 MediatrizRightDir => -MediatrizLeftDir; // (90º CW) => [y,-x]

		#endregion


		#region PARALELAS

		public Edge ParallelRight(float distance) => new(
			begin + MediatrizRightDir * distance,
			end + MediatrizRightDir * distance
		);

		public Edge ParallelLeft(float distance) => new(
			begin + MediatrizLeftDir * distance,
			end + MediatrizLeftDir * distance
		);

		public Edge ParallelRight(Vector2 distance) => new(
			begin + MediatrizRightDir * distance,
			end + MediatrizRightDir * distance
		);

		public Edge ParallelLeft(Vector2 distance) => new(
			begin + MediatrizLeftDir * distance,
			end + MediatrizLeftDir * distance
		);

		#endregion


		#region CONVEXIDAD

		/// <summary>
		///     Comprueba si dos aristas son CONCAVAS (el vertice intermedio esta a la Izquierda)
		///     Presupone que se suceden (e1.end == e2.begin)
		/// </summary>
		public static bool IsConcave(Edge e1, Edge e2) =>
			GeometryUtils.IsLeft(e1.begin, e2.end, e1.end);

		/// <summary>
		///     Comprueba si dos aristas son CONVEXAS (el vertice intermedio esta a la Derecha)
		/// </summary>
		private static bool IsConvex(Edge e1, Edge e2) =>
			GeometryUtils.IsRight(e1.begin, e2.end, e1.end);

		#endregion


		#region INTERSECCIONES

		public Vector2? Intersection(Edge other) =>
			GeometryUtils.IntersectionSegmentSegment(begin, end, other.begin, other.end);

		public Vector2? Intersection_LineSegment(Edge other) =>
			GeometryUtils.IntersectionLineSegment(begin, end, other.begin, other.end);

		public Vector2? Intersection_LineLine(Edge other) =>
			GeometryUtils.IntersectionLineLine(begin, end, other.begin, other.end);

		#endregion


		#region SPLIT

		public Edge[] Split(float t) => Split(Vector2.Lerp(begin, end, t));
		public Edge[] Split(Vector2 point) => new[] { new Edge(begin, point), new Edge(point, end) };

		#endregion


		#region COMPARADORES

		// Ignora el la direccion
		public override bool Equals(object obj)
		{
			if (obj is Edge edge)
				return (edge.begin == begin && edge.end == end) || (edge.begin == end && edge.end == begin);

			return false;
		}

		public override int GetHashCode() => HashCode.Combine(begin, end);

		#endregion


		#region DEBUG

#if UNITY_EDITOR
		public void DrawGizmos(
			Matrix4x4 matrix, float thickness = 1, Color color = default, bool projectedOnTerrain = false
		)
		{
			Vector3[] verticesInWorld = matrix.MultiplyPoint3x4(Vertices).ToArray();
			GizmosExtensions.DrawLineThick(
				projectedOnTerrain ? Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld) : verticesInWorld,
				thickness,
				color
			);
		}

		public void DrawGizmos_Arrow(Matrix4x4 localToWorldM, float thickness = 1, Color color = default)
		{
			float capSize = .2f * localToWorldM.lossyScale.magnitude;
			if (capSize > Vector.magnitude / 2) capSize = Vector.magnitude / 2;
			Vector3 beginInWorld = localToWorldM.MultiplyPoint3x4(begin);
			Vector3 vector = localToWorldM.MultiplyVector(Vector);
			GizmosExtensions.DrawArrow(beginInWorld, vector, capSize, thickness, color);
		}

		public void DrawGizmos_ArrowWire(Matrix4x4 localToWorldM, float thickness = 1, Color color = default)
		{
			float capSize = .02f * localToWorldM.lossyScale.magnitude;
			if (capSize > Vector.magnitude / 2) capSize = Vector.magnitude / 2;
			Vector3 beginInWorld = localToWorldM.MultiplyPoint3x4(begin);
			Vector3 vector = localToWorldM.MultiplyVector(Vector);
			GizmosExtensions.DrawArrowWire(beginInWorld, vector, MediatrizLeftDir, capSize, thickness, color);
		}
#endif

		#endregion
	}
}
