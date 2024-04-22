using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugExtensions;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	[Serializable]
	public class Delaunay
	{
		public struct Edge
		{
			public Vector2 begin;
			public Vector2 end;
			
			public Vector2[] Vertices => new[] { begin, end };

			// Ignora el la direccion
			public override bool Equals(object obj)
			{
				if (obj is Edge edge)
					return (edge.begin == begin && edge.end == end) || (edge.begin == end && edge.end == begin);

				return false;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(begin, end);
			}

#if UNITY_EDITOR
			public void OnGizmosDraw(Vector3 worldPos, Vector2 size, float thickness = 1, Color color = default) =>
				GizmosExtensions.DrawLineThick(Vertices.Select(vertex => (vertex * size).ToVector3xz() + worldPos).ToArray(), thickness, color);
			#endif
		}

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
			
			public Triangle(Vector2 v1, Vector2 v2, Vector2 v3, Triangle t1 = null, Triangle t2 = null, Triangle t3 = null)
			{
				this.v1 = v1;
				this.v2 = v2;
				this.v3 = v3;

				neighbours = new[] { t1, t2, t3 };
				
				e1 = new Edge { begin = v1, end = v2};
				e2 = new Edge { begin = v2, end = v3};
				e3 = new Edge { begin = v3, end = v1};
			}

			public void SetNeighbour(Triangle t, int index)
			{
				neighbours[index] = t;
				
				// Set the neighbour in the other triangle
				Edge sharedEdge = Edges[index];
				for (var i = 0; i < 3; i++)
				{
					Edge edge = t.Edges[i];
					if (edge.Equals(sharedEdge))
						t.neighbours[i] = this;
				}
			}
			
			public static Triangle SuperTriangle =>
				new(new Vector2(-2, -1), new Vector2(2, -1), new Vector2(0, 3));

			#if UNITY_EDITOR
			public void OnGizmosDrawWire(Vector3 worldPos, Vector2 size, float thickness = 1, Color color = default)
			{
				GizmosExtensions.DrawLineThick(
					Vertices.Select(vertex => (vertex * size).ToVector3xz() + worldPos).ToArray(),
					thickness,
					color
				);
			}
			
			public void OnGizmosDraw(Vector3 worldPos, Vector2 size, Color color = default, bool projectedOnTerrain = false)
			{
				Vector3[] verticesInWorld;
				if (projectedOnTerrain)
				{
					Terrain terrain = Terrain.activeTerrain;
					verticesInWorld = Vertices
						.Select(vertex => terrain.Project((vertex * size).ToVector3xz() + worldPos)).ToArray();
				}
				else
				{
					verticesInWorld = Vertices
						.Select(vertex => (vertex * size).ToVector3xz() + worldPos).ToArray();
				}
				GizmosExtensions.DrawTri(verticesInWorld, color);
			}
			#endif
		}

		[HideInInspector] public Vector2[] vertices = Array.Empty<Vector2>();
		[HideInInspector] public List<Triangle> triangles = new();
		
		// Vertices almancenados para buscar al final todos los triangulos que lo tengan
		private Vector2[] superTriangleVertices = Array.Empty<Vector2>();
		
		// Algoritmo de Bowyer-Watson
		// - Por cada punto busca los triangulos cuyo círculo circunscrito contenga al punto
		// - Elimina los triangulos invalidos y crea nuevos triangulos con el punto
		// points deben estar Normalizados entre [0,1]
		public Triangle[] Triangulate(Vector2[] points)
		{
			Reset();
			
			foreach (Vector2 p in points)
				Triangulate(p);
			
			RemoveSuperTriangle();
			
			ended = true;
			
			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();

			return triangles.ToArray();
		}
		
		public void Run() => triangles = Triangulate(vertices).ToList();

		#region PROGRESIVE RUN
		
		public int iterations;
		public bool ended;
		private List<Triangle> removedTris = new();
		private List<Triangle> addedTris = new();
		private List<Edge> polygon = new();
		
		
		public IEnumerator AnimationCoroutine(float delay = 0.1f)
		{
			while (!ended)
			{
				Run_OnePoint();
				yield return new WaitForSecondsRealtime(delay);
			}

			yield return null;
		}
		
		public void Run_OnePoint()
		{
			if (iterations > vertices.Length) return;
			
			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();
			
			if (iterations == vertices.Length)
				RemoveSuperTriangle();
			else
				Triangulate(vertices[iterations]);
			iterations++;
			
			ended = iterations > vertices.Length;
		}

		public void Reset()
		{
			iterations = 0;
			ended = false;
			triangles = new List<Triangle>();
			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();
		}
		
		public void Triangulate(Vector2 point)
		{
			if (triangles.Count == 0) InitializeSuperQuad();
			
			polygon = new List<Edge>();
			var neighbours = new List<Triangle>();
			
			// Triangulos que se deben eliminar
			var badTris = triangles.Where(t => GeometryUtils.PointInCirle(point, t.v1, t.v2, t.v3)).ToList();

			// Ignoramos los ejes compartidos por los triangulos invalidos
			// Guardamos el Triangulo Vecino y la Arista
			foreach (Triangle t in badTris)
				for (int i = 0; i < 3; i++)
				{
					Edge e = t.Edges[i];
					Triangle neighbour = t.neighbours[i];
					if (neighbour != null && badTris.Contains(neighbour)) continue;
					
					neighbours.Add(neighbour);
					polygon.Add(e);
				}
				
			
			// var badTrisIndices = badTris.Select(t => triangles.IndexOf(t));
			// Debug.Log($"Bad Tris: {string.Join(", ", badTrisIndices)}");
			// Debug.Log($"Sorted: {String.Join(", ", polygon.Select(e => $"[{Vector2.SignedAngle(Vector2.right, e.begin - point)}, {Vector2.SignedAngle(Vector2.right, e.end - point)} ({triangles.IndexOf(e.tOpposite)})]")) }");
				
			// Rellenamos el poligono con nuevos triangulos validos por cada arista del poligono
			// Le asignamos t0 como vecino a la arista
			Triangle[] newTris = new Triangle[polygon.Count];
			for (int i = 0; i < polygon.Count; i++)
			{
				Edge e = polygon[i];
				Triangle neighbour = neighbours[i];
				newTris[i] = new Triangle(e.begin, e.end, point, null, null, null);
				if (neighbour != null)
					newTris[i].SetNeighbour(neighbour, 0); // SetNeighbour() es Reciproco
			}
				
			// Ordenamos los nuevo triangulos del poligono CCW
			newTris = newTris.OrderBy(t => Vector2.SignedAngle(Vector2.right, t.v1 - point)).ToArray();
			
			
			// Asignamos vecinos entre ellos. Como esta ordenado CCW t1 es el siguiente, y t2 el anterior
			for (var j = 0; j < newTris.Length; j++)
			{
				Triangle t = newTris[j];
				t.neighbours[1] = newTris[(j + 1) % newTris.Length];
				t.neighbours[2] = newTris[(j - 1 + newTris.Length) % newTris.Length];
			}
				
			// Eliminamos los triangulos invalidos dentro del poligono
			foreach (Triangle badTri in badTris) 
				triangles.Remove(badTri);
				
			// Añadimos los nuevos triangulos
			triangles.AddRange(newTris);
			removedTris = badTris;
			addedTris = newTris.ToList();
		}

		public void InitializeSuperQuad()
		{
			var t1 = new Triangle(new Vector2(2, -1), new Vector2(-1, 2), new Vector2(-1, -1));
			var t2 = new Triangle(new Vector2(-1, 2), new Vector2(2, -1), new Vector2(2,2));

			t1.neighbours[0] = t2;
			t2.neighbours[0] = t1;
			
			triangles.Add(t1);
			triangles.Add(t2);
			superTriangleVertices = t1.Vertices.Concat(t2.Vertices).ToArray();
		}
		
		public void InitializeSuperTriangle()
		{
			Triangle superTri = Triangle.SuperTriangle;
			triangles.Add(superTri);
			superTriangleVertices = superTri.Vertices;
		}

		// Elimina todos los Triangulos que contengan un vertice del SuperTriangulo
		// Y reasigna el vecino a de cualquier triangulo vecino que tenga como vecino al triangulo eliminado a null
		public void RemoveSuperTriangle()
		{
			var trisToRemove = triangles.Where(t => t.Vertices.Any(v => superTriangleVertices.Contains(v))).ToArray();
			foreach (Triangle t in trisToRemove)
			{
				// Eliminamos las referencias de los vecinos
				foreach (Triangle neighbour in t.neighbours)
				{
					if (neighbour == null) continue;
					for (var i = 0; i < neighbour.neighbours.Length; i++)
						if (neighbour.neighbours[i] == t) neighbour.neighbours[i] = null;
				}
				triangles.Remove(t);
			}
		}

		#endregion
		

		#if UNITY_EDITOR
		#region DEBUG

		public void OnDrawGizmos(Vector3 pos, Vector2 size, bool projectOnTerrain = false)
		{
			// VERTICES
			Gizmos.color = Color.grey;
			foreach (Vector2 vertex in vertices) 
				Gizmos.DrawSphere((pos + (vertex * size).ToVector3xz() ), .1f);
			
			// DELAUNAY TRIANGULATION
			var colors = Color.cyan.GetRainBowColors(triangles.Count, 0.02f);
			
			for (int i = 0; i < triangles.Count; i++)
			{
				Triangle tri = triangles[i];
					tri.OnGizmosDraw(pos, size, colors[i], projectOnTerrain);
				// if (ended)
				// else
				// 	tri.OnGizmosDrawWire(pos, size, 2, Color.white);
			}
			
			// ADDED TRIANGLES
			colors = Color.cyan.GetRainBowColors(addedTris.Count, 0.02f);
			foreach (Delaunay.Triangle t in addedTris) t.OnGizmosDraw(pos, size);
			for (var i = 0; i < addedTris.Count; i++) 
				addedTris[i].OnGizmosDraw(pos + Vector3.down, size, colors[i]);
			
			// DELETED TRIANGLES
			foreach (Triangle t in removedTris) t.OnGizmosDrawWire(pos, size, 2, Color.red);
			
			// POLYGON
			foreach (Edge e in polygon) e.OnGizmosDraw(pos, size, 2, Color.green);
		}

		#endregion
		#endif
	}
}
