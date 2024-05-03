using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;
#if UNITY_EDITOR
using DavidUtils.DebugUtils;
#endif

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

			public Edge(Vector2 a, Vector2 b)
			{
				begin = a;
				end = b;
			}

			// Ignora el la direccion
			public override bool Equals(object obj)
			{
				if (obj is Edge edge)
					return (edge.begin == begin && edge.end == end) || (edge.begin == end && edge.end == begin);

				return false;
			}

			public override int GetHashCode() => HashCode.Combine(begin, end);

#if UNITY_EDITOR
			public void OnGizmosDraw(Matrix4x4 matrix, float thickness = 1, Color color = default) =>
				GizmosExtensions.DrawLineThick(
					Vertices.Select(vertex => matrix.MultiplyPoint3x4(vertex.ToVector3xz())).ToArray(),
					thickness,
					color
				);
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

			public Edge[] BorderEdges => Edges.Where((e, i) => neighbours[i] == null).ToArray();

			public Triangle(
				Vector2 v1, Vector2 v2, Vector2 v3, Triangle t1 = null, Triangle t2 = null, Triangle t3 = null
			)
			{
				this.v1 = v1;
				this.v2 = v2;
				this.v3 = v3;

				neighbours = new[] { t1, t2, t3 };

				e1 = new Edge { begin = v1, end = v2 };
				e2 = new Edge { begin = v2, end = v3 };
				e3 = new Edge { begin = v3, end = v1 };
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

			// MEDIATRIZ para encontrar el circuncentro, vértice buscado en Voronoi
			public Vector2 GetCircumcenter()
			{
				Vector2? c = GeometryUtils.CircleCenter(v1, v2, v3);
				if (c.HasValue) return c.Value;

				// Son colineares
				return (v1 + v2 + v3) / 3;
			}

			public static Triangle SuperTriangle =>
				new(new Vector2(-2, -1), new Vector2(2, -1), new Vector2(0, 3));

#if UNITY_EDITOR
			public void OnGizmosDrawWire(Matrix4x4 matrix, float thickness = 1, Color color = default) =>
				GizmosExtensions.DrawTriWire(
					Vertices.Select(vertex => matrix.MultiplyPoint3x4(vertex.ToVector3xz())).ToArray(),
					thickness,
					color
				);

			public void OnGizmosDraw(Matrix4x4 matrix, Color color = default, bool projectedOnTerrain = false)
			{
				var terrain = Terrain.activeTerrain;
				Vector3[] verticesInWorld = Vertices
					.Select(
						vertex =>
						{
							Vector3 v = matrix.MultiplyPoint3x4(vertex.ToVector3xz());
							return terrain != null && projectedOnTerrain ? terrain.Project(v) : v;
						}
					)
					.ToArray();

				GizmosExtensions.DrawTri(verticesInWorld, color);
			}
#endif
		}

		[HideInInspector] public Vector2[] seeds = Array.Empty<Vector2>();
		[HideInInspector] public List<Vector2> vertices = new();
		[HideInInspector] public List<Triangle> triangles = new();

		// Algoritmo de Bowyer-Watson
		// - Por cada punto busca los triangulos cuyo círculo circunscrito contenga al punto
		// - Elimina los triangulos invalidos y crea nuevos triangulos con el punto
		// points deben estar Normalizados entre [0,1]
		public Triangle[] Triangulate(Vector2[] points)
		{
			Reset();

			foreach (Vector2 p in points)
				Triangulate(p);

			RemoveBoundingBox();

			ended = true;

			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();

			return triangles.ToArray();
		}

		public void Run() => triangles = Triangulate(seeds).ToList();

		#region PROGRESIVE RUN

		[HideInInspector] public int iterations;
		[HideInInspector] public bool ended;
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
			if (iterations > seeds.Length)
				return;

			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();

			if (iterations == seeds.Length)
				RemoveBoundingBox();
			else
				Triangulate(seeds[iterations]);

			iterations++;

			ended = iterations > seeds.Length;
		}

		public void Reset()
		{
			iterations = 0;
			ended = false;
			triangles = new List<Triangle>();
			vertices = new List<Vector2>();
			removedTris = new List<Triangle>();
			addedTris = new List<Triangle>();
			polygon = new List<Edge>();
		}

		public void Triangulate(Vector2 point)
		{
			if (vertices.Any(v => Vector2.Distance(v, point) < GeometryUtils.Epsilon)) return;
			vertices.Add(point);

			// Si no hay triangulos, creamos el SuperTriangulo o una Bounding Box
			if (triangles.Count == 0)
			{
				Triangle[] bb = GetBoundingBoxTriangles();

				triangles.Add(bb[0]);
				triangles.Add(bb[1]);
			}

			polygon = new List<Edge>();
			var neighbours = new List<Triangle>();

			// Triangulos que se deben eliminar
			List<Triangle> badTris = triangles.Where(t => GeometryUtils.PointInCirle(point, t.v1, t.v2, t.v3)).ToList();

			// Ignoramos los ejes compartidos por los triangulos invalidos
			// Guardamos el Triangulo Vecino y la Arista
			foreach (Triangle t in badTris)
				for (var i = 0; i < 3; i++)
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
			var newTris = new Triangle[polygon.Count];
			for (var i = 0; i < polygon.Count; i++)
			{
				Edge e = polygon[i];
				Triangle neighbour = neighbours[i];
				newTris[i] = new Triangle(e.begin, e.end, point);
				if (neighbour != null)
					newTris[i].SetNeighbour(neighbour, 0); // SetNeighbour() es Reciproco
			}

			// Ordenamos los nuevo triangulos del poligono CCW
			newTris = newTris.OrderBy(
					t =>
					{
						Vector2 polarPos = t.v1 - point;
						return Mathf.Atan2(polarPos.y, polarPos.x);
					}
				)
				.ToArray();


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

		#endregion


		#region BOUNDING BOX or SUPERTRIANGLE

		// Vertices almancenados para buscar al final todos los triangulos que lo tengan
		private Vector2[] boundingVertices = Array.Empty<Vector2>();

		public Triangle[] GetBoundingBoxTriangles()
		{
			var bounds = new Bounds2D(Vector2.one * -.1f, Vector2.one * 1.1f);
			var t1 = new Triangle(bounds.BR, bounds.TL, bounds.BL);
			var t2 = new Triangle(bounds.TL, bounds.BR, bounds.TR);

			t1.neighbours[0] = t2;
			t2.neighbours[0] = t1;

			boundingVertices = bounds.Corners;

			return new[] { t1, t2 };
		}

		public void InitializeSuperTriangle()
		{
			var superTri = Triangle.SuperTriangle;
			triangles.Add(superTri);
			boundingVertices = superTri.Vertices;
		}

		// Elimina todos los Triangulos que contengan un vertice del SuperTriangulo/s
		// Reasigna el vecino a de cualquier triangulo vecino que tenga como vecino al triangulo eliminado a null
		// Y repara el borde
		public void RemoveBoundingBox() => RemoveBorder(boundingVertices);

		private void RemoveBorder(Vector2[] points)
		{
			// Cogemos todos los Triangulos que contengan un vertice del SuperTriangulo
			HashSet<Triangle> trisToRemove = points.SelectMany(FindTrianglesAroundVertex).ToHashSet();

			foreach (Triangle t in trisToRemove)
			{
				// Eliminamos las referencias de los vecinos
				foreach (Triangle neighbour in t.neighbours)
				{
					if (neighbour == null) continue;
					for (var i = 0; i < neighbour.neighbours.Length; i++)
						if (neighbour.neighbours[i] == t)
							neighbour.neighbours[i] = null;
				}

				triangles.Remove(t);
			}

			// Reparamos el borde para que sea CONVEXO

			// Cogemos de cada triangulo que forme parte del borde su arista de borde
			// y guardamos el indice del triangulo al que pertenece para asignar vecinos despues
			Border[] borderEdges = Borders;

			// Iteramos los ejes por PARES, para ver si son convexos o no
			// Si NO es convexo, añadimos un triangulo al borde
			for (var i = 0; i < borderEdges.Length; i++)
			{
				Border border = borderEdges[i];
				Border nextBorder = borderEdges[(i + 1) % borderEdges.Length];

				// Siendo los ejes v1 - v2, v2 - v3
				// Es convexo si el vertice intermedio v2 esta a la derecha de la linea v1 - v3
				Vector2 v1 = border.edge.begin, v2 = border.edge.end, v3 = nextBorder.edge.end;
				if (!IsConcave(border.edge, nextBorder.edge)) continue;

				var tri = new Triangle(v3, v2, v1);
				tri.SetNeighbour(nextBorder.tri, 0);
				tri.SetNeighbour(border.tri, 1);
				triangles.Add(tri);
			}
		}

		private void RemovePointFromBorder(Vector2 point)
		{
		}

		private struct Border
		{
			public readonly Triangle tri;
			public readonly Edge edge;

			public Vector2 polarPos => edge.begin - Vector2.one * .5f;
			public float polarAngle => Mathf.Atan2(polarPos.y, polarPos.x);

			public Border(Triangle tri, Edge edge)
			{
				this.tri = tri;
				this.edge = edge;
			}
		}

		// Lista de Aristas que forman el Borde
		// (Busca las que no tengan triangulo vecino)
		// Ordenadas CCW por sus coords polares respecto a 0,0
		private Border[] Borders
		{
			get
			{
				List<Border> borders = new();
				foreach (Triangle tri in triangles)
				{
					if (!tri.IsBorder) continue;

					Triangle tri1 = tri;
					borders.AddRange(tri.BorderEdges.Select(e => new Border(tri1, e)));
				}

				return borders.OrderBy(border => border.polarAngle).ToArray();
			}
		}

		// Comprueba si dos aristas son CONCAVAS (el vertice intermedio esta a la Izquierda)
		// Presupone que se suceden (e1.end == e2.begin)
		private bool IsConcave(Edge e1, Edge e2) =>
			GeometryUtils.IsLeft(e1.begin, e2.end, e1.end);

		private bool IsConvex(Edge e1, Edge e2) =>
			GeometryUtils.IsRight(e1.begin, e2.end, e1.end);

		#endregion


		public Triangle[] FindTrianglesAroundVertex(Vector2 vertex) =>
			triangles.Where(t => t.Vertices.Any(v => v.Equals(vertex))).ToArray();

#if UNITY_EDITOR

		#region DEBUG

		public void OnDrawGizmos(Matrix4x4 matrix, bool wire = false, bool projectOnTerrain = false)
		{
			// VERTICES
			Gizmos.color = Color.grey;
			foreach (Vector2 vertex in seeds)
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(vertex.ToVector3xz()), .1f);

			// DELAUNAY TRIANGULATION
			Color[] colors = Color.cyan.GetRainBowColors(triangles.Count, 0.02f);

			for (var i = 0; i < triangles.Count; i++)
			{
				Triangle tri = triangles[i];
				if (ended && !wire)
					tri.OnGizmosDraw(matrix, colors[i], projectOnTerrain);
				else
					tri.OnGizmosDrawWire(matrix, 2, Color.white);
			}

			// ADDED TRIANGLES
			colors = Color.cyan.GetRainBowColors(addedTris.Count, 0.02f);
			foreach (Triangle t in addedTris) t.OnGizmosDrawWire(matrix, 3, Color.white);
			for (var i = 0; i < addedTris.Count; i++)
				addedTris[i].OnGizmosDraw(matrix, colors[i]);

			// DELETED TRIANGLES
			foreach (Triangle t in removedTris) t.OnGizmosDrawWire(matrix, 3, Color.red);

			// POLYGON
			foreach (Edge e in polygon) e.OnGizmosDraw(matrix, 3, Color.green);

			// Highlight Border
			GizmosHightlightBorder(matrix);
		}

		private void GizmosHightlightBorder(Matrix4x4 matrix) => GizmosExtensions.DrawPolygonWire(
			Borders.Select(t => t.edge.begin).Select(p => matrix.MultiplyPoint3x4(p.ToVector3xz())).ToArray(),
			10,
			Color.red
		);

		#endregion

#endif
	}
}
