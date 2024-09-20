using System;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public class Triangle
	{
		// CCW
		public Vector2 v1, v2, v3;
		public Vector2[] Vertices => new[] { v1, v2, v3 };

		public Edge e1;
		public Edge e2;
		public Edge e3;
		public Edge[] Edges => new[] { e1, e2, e3 };

		// NEIGHBOURS (t1 share e1, etc)
		public Triangle[] neighbours;
		public Triangle T1 => neighbours[0];
		public Triangle T2 => neighbours[1];
		public Triangle T3 => neighbours[2];

		public bool IsBorder => neighbours.Length != 3 || neighbours.Any(n => n == null);

		public Edge[] BorderEdges => Edges.Where((e, i) => neighbours[i] == null).ToArray();

		public Triangle(Vector2[] vertices, Triangle[] neighbours = null)
		{
			v1 = vertices[0];
			v2 = vertices[1];
			v3 = vertices[2];

			SetAllNeightbours(neighbours ?? Array.Empty<Triangle>());

			e1 = new Edge(v1, v2);
			e2 = new Edge(v2, v3);
			e3 = new Edge(v3, v1);
		}

		public Triangle(
			Vector2 v1, Vector2 v2, Vector2 v3, Triangle t1 = null, Triangle t2 = null, Triangle t3 = null
		) : this(new[] { v1, v2, v3 }, new[] { t1, t2, t3 })
		{
		}

		public Triangle(
			Vector2[] vertices, Triangle t1, Triangle t2, Triangle t3
		) : this(vertices, new[] { t1, t2, t3 })
		{
		}


		public void MoveVertex(Vector2 vertex, Vector2 newVertex)
		{
			if (v1 == vertex)
			{
				v1 = newVertex;
				e3.end = newVertex;
				e1.begin = newVertex;
			}
			else if (v2 == vertex)
			{
				v2 = newVertex;
				e1.end = newVertex;
				e2.begin = newVertex;
			}
			else if (v3 == vertex)
			{
				v3 = newVertex;
				e2.end = newVertex;
				e3.begin = newVertex;
			}
		}

		/// <summary>
		///     Asigna vecinos de forma recíproca (Al vecino se le asigna este Triangulo como vecino también)
		/// </summary>
		public void SetAllNeightbours(Triangle[] newNeighbours)
		{
			neighbours = newNeighbours;
			for (var i = 0; i < newNeighbours.Length; i++)
				SetNeighbour(newNeighbours[i], i);
		}

		/// <summary>
		///     Asigna un vecino de forma recíproca cuya arista que los une es Edges[index]
		///     (Al vecino se le asigna este Triangulo como vecino también)
		/// </summary>
		public void SetNeighbour(Triangle t, int index)
		{
			neighbours[index] = t;
			if (t == null) return;

			// Set the neighbour in the other triangle
			Edge sharedEdge = Edges[index];
			for (var i = 0; i < 3; i++)
			{
				Edge edge = t.Edges[i];
				if (edge.Equals(sharedEdge))
					t.neighbours[i] = this;
			}
		}

		public Triangle GetNeighbour(Edge edge) => neighbours[Array.IndexOf(Edges, edge)];

		/// <summary>
		///     Interseccion de MEDIATRIZES para encontrar el circuncentro, vértice buscado en Voronoi
		/// </summary>
		public Vector2 GetCircumcenter()
		{
			Vector2? c = GeometryUtils.CircleCenter(v1, v2, v3);
			if (c.HasValue) return c.Value;

			// Son colineares
			return (v1 + v2 + v3) / 3;
		}

		/// <summary>
		///     Busca el vertice opuesto al eje dado
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="side">Índice del vértice opuesto</param>
		public Vector2 GetOppositeVertex(Edge edge, out int side)
		{
			for (var i = 0; i < 3; i++)
			{
				Vector2 vertex = Vertices[i];
				if (vertex == edge.begin || vertex == edge.end) continue;

				side = i;
				return vertex;
			}

			side = 0;
			return v1;
		}

		// Super Triangulo para Delaunay
		public static Triangle SuperTriangle =>
			new(new Vector2(-2, -1), new Vector2(2, -1), new Vector2(0, 3));


		#region TEST

		public bool IsCCW() => GeometryUtils.IsLeft(v1, v2, v3);
		public bool IsCW() => GeometryUtils.IsRight(v1, v2, v3);

		
		/// <summary>
		///		TEST Point is inside Polygon
		///		Uses RayCasting
		/// </summary>
		public bool Contains_RayCast(Vector2 point)
		{
			var contains = false;
			Edges.ForEach(
				e =>
				{
					Vector2 a = e.begin, b = e.end;
					if (b.y > point.y != a.y > point.y && point.x < (a.x - b.x) * (point.y - b.y) / (a.y - b.y) + b.x)
						contains = !contains;
				}
			);
			return contains;
		}
		
		/// <summary>
		///		TEST Point is inside Polygon
		///		Using Cross Product
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public bool Contains_CrossProd(Vector2 point) =>
			IsCCW() 
				? Edges.All(e => GeometryUtils.IsLeft(e.begin, e.end, point))
				: Edges.All(e => GeometryUtils.IsRight(e.begin, e.end, point));


		public bool IsOnEdge(Vector2 point) =>
			Edges.Any(e => GeometryUtils.IsColinear(e.begin, e.end, point));
		
		#endregion
		
		
		public override string ToString() => $"Tri [{v1}, {v2}, {v3}]";

		
#if UNITY_EDITOR

		#region DEBUG
		
		public void GizmosDrawWire(
			Matrix4x4 localToWorldMatrix, float thickness = 1, Color color = default, bool projectedOnTerrain = false
		)
		{
			Vector3[] verticesInWorld = localToWorldMatrix.MultiplyPoint3x4(Vertices).ToArray();

			if (projectedOnTerrain && Terrain.activeTerrain != null)
				GizmosExtensions.DrawPolygonWire(
					Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld),
					thickness,
					color
				);
			else
				GizmosExtensions.DrawTriWire(verticesInWorld, thickness, color);
		}

		public void GizmosDraw(Matrix4x4 matrix, Color color = default, bool projectedOnTerrain = false)
		{
			Vector3[] verticesInWorld = matrix.MultiplyPoint3x4(Vertices).ToArray();

			if (projectedOnTerrain && Terrain.activeTerrain != null)
				GizmosExtensions.DrawPolygon(
					Terrain.activeTerrain.ProjectPathToTerrain(verticesInWorld),
					color
				);
			else
				GizmosExtensions.DrawTri(verticesInWorld, color);
		}

		public void GizmosDrawCircumcenter(Matrix4x4 localToWorldMatrix)
		{
			Vector2 c = GetCircumcenter();
			Gizmos.color = c.IsIn01() ? Color.green : Color.red;
			Gizmos.DrawSphere(localToWorldMatrix.MultiplyPoint3x4(c), localToWorldMatrix.lossyScale.x * .005f);
		}

		#endregion

#endif
	}
}
