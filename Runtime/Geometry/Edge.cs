using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	
	[Serializable]
	public class Edge
	{
		// Para vertices superpuestos
		public const float VertexEpsilon = 0.0001f;
		
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

		public bool IsValid => !GeometryUtils.Equals(begin, end);
		

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


		#region SHORTEN

		public Edge Shorten(float padding) =>
			new(begin + Dir * padding, end - Dir * padding);

		public Edge ShortenProportional(float t) =>
			new(Vector2.Lerp(begin, end, t / 2), Vector2.Lerp(end, begin, t / 2));

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

		public bool Intersection(Edge other, out Vector2 intersection) =>
			GeometryUtils.IntersectionSegmentSegment(begin, end, other.begin, other.end, out intersection);

		public bool Intersection_LineSegment(Edge other, out Vector2 intersection) =>
			GeometryUtils.IntersectionLineSegment(begin, end, other.begin, other.end, out intersection);

		public bool Intersection_LineLine(Edge other, out Vector2 intersection) =>
			GeometryUtils.IntersectionLineLine(begin, end, other.begin, other.end, out intersection);

		#endregion
		

		#region SPLIT

		public Edge[] Split(float t) => Split(Vector2.Lerp(begin, end, t));
		public Edge[] Split(Vector2 point) => new[] { new Edge(begin, point), new Edge(point, end) };

		#endregion


		#region LOOP

		/// <summary>
		///     Busca un BUCLE en una lista de aristas desordenada
		/// </summary>
		public static List<Edge[]> SearchLoop(Edge[] edges)
		{
			if (edges.IsNullOrEmpty()) return null;

			List<Edge[]> loopList = new();
			List<Edge> loop = new();
			// Buscamos un bucle posible por cada arista, n a n
			foreach (Edge edge in edges)
			{
				// Si ya hay un bucle con esta arista no hace falta comprobarla
				if (loopList.Any(l => l.Contains(edge))) continue;

				loop.Clear();
				loop.Add(edge);
				Edge nextEdge = edge;
				do
				{
					Edge currentEdge = nextEdge;
					nextEdge = null;

					// Buscamos una arista que comience donde termina la actual
					int nextIndex = edges.FirstIndex(e => e.begin == currentEdge.end);
					if (nextIndex == -1) continue;
					nextEdge = edges[nextIndex];

					// Comprobamos que no este ya en el bucle
					// Si ya esta, podría entrar en un bucle interno y nunca volver al eje inicial
					if (loop.Contains(nextEdge)) break;

					loop.Add(nextEdge);

					// Si completa el bucle, lo añadimos a la lista de bucles
					if (nextEdge.end == edge.begin) loopList.Add(loop.ToArray());

					// Si no hay siguiente arista, no hay bucle para esta arista
				} while (nextEdge != null && nextEdge.end != edge.begin);
			}

			return loopList;
		}

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


		#region TEST
		
		public bool Collinear(Edge other) => GeometryUtils.CollinearVectors(begin, end, other.begin, other.end);
		
		public bool PointCollinear(Vector2 p) => GeometryUtils.CollinearPointInLine(begin, end, p);
		public bool PointOnEdge(Vector2 point) => GeometryUtils.PointOnSegment(begin, end, point);

		public float DistanceTo(Vector2 point) =>
			PointOnEdge(GeometryUtils.Projection(point, begin, end))
				? GeometryUtils.DistanceToLine(begin, end, point)
				: Mathf.Min((point - begin).magnitude, (point - end).magnitude);

		#endregion


		#region DEBUG

#if UNITY_EDITOR
		public void DrawGizmos(
			Matrix4x4 matrix, float thickness = 1, Color color = default, bool projectedOnTerrain = false
		)
		{
			Vector3[] verticesInWorld = matrix.MultiplyPoint3x4(Vertices).ToArray();
			GizmosExtensions.DrawLineThick(
				projectedOnTerrain && Terrain.activeTerrain != null
					? Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld) 
					: verticesInWorld,
				thickness,
				color
			);
		}

		public void DrawGizmos_Arrow(
			GizmosExtensions.ArrowCap cap, Matrix4x4 localToWorldM, Color? color = null, float thickness = 1
		)
		{
			float capSize = .2f * localToWorldM.lossyScale.magnitude;
			if (capSize > Vector.magnitude / 2) capSize = Vector.magnitude / 2;
			Vector3 beginInWorld = localToWorldM.MultiplyPoint3x4(begin);
			Vector3 vector = localToWorldM.MultiplyVector(Vector);
			Vector3 up = localToWorldM.MultiplyVector(Vector3.back);
			GizmosExtensions.DrawArrow(cap, beginInWorld, vector, up, capSize, color, thickness);
		}
#endif

		#endregion
	}

	public static class EdgeExtensions
	{
		
		#region INVALID EDGES
		
		/// <summary>
		///     Quita las aristas invalidas
		///		(colineales, generando area nula)
		///		y con longitud 0 (begin == end)
		/// </summary>
		public static Edge[] RemoveInvalidEdges(this Edge[] edges)
		{
			if (edges.IsNullOrEmpty()) return edges; // Empty
			
			List<Edge> validEdges = new List<Edge>(edges.Length);

			foreach (Edge edge in edges)
			{
				if (!edge.IsValid) continue; // Invalid Edge IGNORED

				if (validEdges.Count > 0)
				{
					// Check if last edge added is colinear with this one
					Edge lastEdge = validEdges[^1];
					if (lastEdge.Collinear(edge))
					{
						// If colinear, create a new edge from begin to end
						Edge newEdge = new(lastEdge.begin, edge.end);
						
						// If new edge is valid, replace last edge with new edge
						if (newEdge.IsValid) 
							validEdges[^1] = newEdge;
						
						continue;
					}
				}

				// All GOOD: No invalid Edge, no Collinear edge
				validEdges.Add(edge);
			}

			return validEdges.ToArray();
		}

		/// <summary>
		///		Check if Next Edge ends in the beginning of the current edge
		///		Creating a null Area
		///		U-Turn (Pokemon Move)
		/// </summary>
		/// <returns>
		///		TRUE => Next Edge ends in the beginning of the current edge
		/// </returns>
		public static bool IsU_Turn(this Edge a, Edge b) 
			=> Vector2.Distance(a.end, b.begin) < Edge.VertexEpsilon;

		#endregion
	}
}
