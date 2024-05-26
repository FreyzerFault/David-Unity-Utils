using System;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.TerrainExtensions;
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

		// Comprueba si dos aristas son CONCAVAS (el vertice intermedio esta a la Izquierda)
		// Presupone que se suceden (e1.end == e2.begin)
		public static bool IsConcave(Edge e1, Edge e2) =>
			GeometryUtils.IsLeft(e1.begin, e2.end, e1.end);

		private static bool IsConvex(Edge e1, Edge e2) =>
			GeometryUtils.IsRight(e1.begin, e2.end, e1.end);

		public Vector2 Median => (begin + end) / 2;
		public Vector2 Dir => (end - begin).normalized;

		// Dir Derecha => (90º CCW) => [y,-x]
		public Vector2 MediatrizRight => new(Dir.y, -Dir.x);

		// Dir IZQUIERDA => (90º CW) => [-y,x]
		public Vector2 MediatrizLeft => new(-Dir.y, Dir.x);


		// Ignora el la direccion
		public override bool Equals(object obj)
		{
			if (obj is Edge edge)
				return (edge.begin == begin && edge.end == end) || (edge.begin == end && edge.end == begin);

			return false;
		}

		public override int GetHashCode() => HashCode.Combine(begin, end);

#if UNITY_EDITOR
		public void OnGizmosDraw(
			Matrix4x4 matrix, float thickness = 1, Color color = default, bool projectedOnTerrain = false
		)
		{
			Vector3[] verticesInWorld =
				Vertices.Select(vertex => matrix.MultiplyPoint3x4(vertex.ToV3xz())).ToArray();
			GizmosExtensions.DrawLineThick(
				projectedOnTerrain ? Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld) : verticesInWorld,
				thickness,
				color
			);
		}
#endif
	}
}
