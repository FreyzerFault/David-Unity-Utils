﻿using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;

namespace DavidUtils.Geometry.Algorithms
{
	[Serializable]
	public class Delaunay
	{
		private List<Vector2> _seeds;
		public List<Vector2> Seeds
		{
			get => _seeds;
			set
			{
				_seeds = value;
				Reset();
			}
		}

		[HideInInspector] public List<Vector2> vertices = new();
		[HideInInspector] public List<Triangle> triangles = new();

		public int SeedCount => _seeds.Count;
		public int TriangleCount => triangles?.Count ?? 0;
		public int VerticesCount => vertices.Count;

		public bool NotGenerated => triangles == null || triangles.Count == 0;

		public Delaunay(IEnumerable<Vector2> seeds = null) =>
			_seeds = seeds?.ToList() ?? new List<Vector2>();


		public IEnumerable<Triangle> FindTrianglesAroundVertex(Vector2 vertex) =>
			triangles.Where(t => t.Vertices.Any(v => v.Equals(vertex)));


		#region TRIANGULATION

		public List<Triangle> RunTriangulation() => triangles = Triangulate(_seeds).ToList();

		// Algoritmo de Bowyer-Watson
		// - Por cada punto busca los triangulos cuyo círculo circunscrito contenga al punto
		// - Elimina los triangulos invalidos y crea nuevos triangulos con el punto
		// points deben estar Normalizados entre [0,1]
		public IEnumerable<Triangle> Triangulate(List<Vector2> points = null)
		{
			Seeds = points ?? _seeds;

			foreach (Vector2 p in Seeds)
				Triangulate(p);

			RemoveBoundingBox();

			ended = true;

			_removedTris = new List<Triangle>();
			_addedTris = new List<Triangle>();
			_holePolygon = new List<Edge>();

			return triangles;
		}

		/// <summary>
		///     Delaunay Incremental Bowyer–Watson Algorithm
		///     Elimina los Triangulos en los que el Vertice este dentro de su Circulo Circunscrito
		///     Crea un Polígono con el hueco que se forma al eliminar los triángulos
		///     Y añade un triangulo por cada arista del polígono
		///     Por definición, los triángulos generados deben ser LEGALES
		///     Por lo que no hace falta flipear ninguna arista
		/// </summary>
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

			_holePolygon = new List<Edge>();
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
					_holePolygon.Add(e);
				}


			// var badTrisIndices = badTris.Select(t => triangles.IndexOf(t));
			// Debug.Log($"Bad Tris: {string.Join(", ", badTrisIndices)}");
			// Debug.Log($"Sorted: {String.Join(", ", polygon.Select(e => $"[{Vector2.SignedAngle(Vector2.right, e.begin - point)}, {Vector2.SignedAngle(Vector2.right, e.end - point)} ({triangles.IndexOf(e.tOpposite)})]")) }");

			// Rellenamos el poligono con nuevos triangulos validos por cada arista del poligono
			// Le asignamos t0 como vecino a la arista
			var newTris = new Triangle[_holePolygon.Count];
			for (var i = 0; i < _holePolygon.Count; i++)
			{
				Edge e = _holePolygon[i];
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
			_removedTris = badTris;
			_addedTris = newTris.ToList();
		}

		#endregion


		#region LEGALIZATION

		/// <summary>
		///     Legaliza el triangulo. Si no es legal, hace FLIP
		///     Comprueba despues todos los vecinos de forma recursiva hasta que todos sean legales
		/// </summary>
		/// <returns>True si era ilegal y se legalizo</returns>
		private bool Legalize(Triangle tri, out Triangle[] flippedTris, int recursiveCalls = 0, int maxCalls = 100)
		{
			flippedTris = null;

			if (IsLegal(tri, out List<int> illegalSides)) return false;

			Flip(tri, illegalSides[0], out flippedTris);

			// PARA si supera el maximo de llamadas recursivas
			if (recursiveCalls >= maxCalls) return true;
			recursiveCalls++;

			// Comprobamos vecinos de forma recursiva
			foreach (Triangle flippedTri in flippedTris)
			{
				// Temp var to use out var in lambda
				Triangle[] flippedTemp = flippedTris;
				foreach (Triangle neighbour in flippedTri.neighbours.Where(t => !flippedTemp.Contains(t)))
					if (neighbour != null)
						Legalize(neighbour, out Triangle[] _, recursiveCalls, maxCalls);
			}

			return true;
		}

		/// <summary>
		///     Comprueba si el triangulo es legal
		///     (Alguno de los triangulos vecinos tiene un vértice opuesto
		///     dentro del circulo circunscrito formado por el triangulo)
		///     Guarda los indices de los lados ilegales en illegalSides
		/// </summary>
		private bool IsLegal(Triangle tri, out List<int> illegalSides)
		{
			illegalSides = new List<int>();
			for (var i = 0; i < 3; i++)
			{
				Edge edge = tri.Edges[i];
				Triangle neighbour = tri.neighbours[i];
				if (neighbour == null) continue;
				if (GeometryUtils.PointInCirle(neighbour.GetOppositeVertex(edge, out int _), tri.v1, tri.v2, tri.v3))
					illegalSides.Add(i);
			}

			return illegalSides.Count == 0;
		}

		/// <summary>
		///     Flipea la arista del triangulo tri.Edges[side]
		///     Eliminando el triangulo y su vecino, y creando nuevos
		///     Actualiza los vecinos también
		/// </summary>
		private void Flip(Triangle tri, int side, out Triangle[] flippedTris)
		{
			Edge edge = tri.Edges[side];
			Triangle neighbour = tri.neighbours[side];

			Vector2 opposite1 = tri.GetOppositeVertex(edge, out int _);
			Vector2 opposite2 = neighbour.GetOppositeVertex(edge, out int opSide2);

			Triangle newTri1 = new(opposite1, opposite2, edge.end);
			Triangle newTri2 = new(opposite2, opposite1, edge.begin);

			// NEIGHBOURS
			newTri1.SetAllNeightbours(new[] { newTri2, neighbour.neighbours[opSide2], tri.neighbours[(side + 1) % 3] });
			newTri2.SetAllNeightbours(
				new[] { newTri1, tri.neighbours[(side + 2) % 3], neighbour.neighbours[(opSide2 + 2) % 3] }
			);

			_addedTris = new List<Triangle>(new[] { newTri1, newTri2 });
			_removedTris = new List<Triangle>(new[] { tri, neighbour });

			triangles.Remove(tri);
			triangles.Remove(neighbour);

			triangles.AddRange(_addedTris);

			flippedTris = new[] { newTri1, newTri2 };
		}

		#endregion


		#region BORDER

		// Lista de Aristas que forman el Borde
		// (Busca las que no tengan triangulo vecino)
		// Ordenadas CCW por sus coords polares respecto a 0,0
		private IEnumerable<Border> Borders
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

				return borders.OrderBy(border => border.PolarAngle).ToArray();
			}
		}


		/// <summary>
		///     Crea un borde a partir de un vertice.
		///     Recoge todas las aristas opuestas al vertice de los triangulos a los que pertenece
		///     Y los ordena CCW
		/// </summary>
		private IEnumerable<Border> GetBordersAround(Vector2 centerVertex, IEnumerable<Triangle> trisAround = null)
		{
			trisAround ??= FindTrianglesAroundVertex(centerVertex);
			IEnumerable<Border> borders = trisAround
				.SelectMany(
					t =>
					{
						List<Border> borders = new();
						for (var i = 0; i < 3; i++)
						{
							Edge edge = t.Edges[i];
							Triangle neighbour = t.neighbours[i];
							if (edge.begin != centerVertex && edge.end != centerVertex)
								borders.Add(new Border(neighbour, edge));
						}

						return borders;
					}
				);

			return Border.SortByAngle(borders, centerVertex);
		}

		#endregion


		#region PROGRESIVE RUN

		[HideInInspector] public int iterations;
		[HideInInspector] public bool ended;
		private List<Triangle> _removedTris = new();
		private List<Triangle> _addedTris = new();
		private List<Edge> _holePolygon = new();

		public void Run_OnePoint()
		{
			if (iterations > SeedCount)
				return;

			_removedTris.Clear();
			_addedTris.Clear();
			_holePolygon.Clear();

			if (iterations == SeedCount)
				RemoveBoundingBox();
			else
				Triangulate(_seeds[iterations]);

			iterations++;

			ended = iterations > _seeds.Count;
		}

		public void Reset()
		{
			iterations = 0;
			ended = false;
			triangles = new List<Triangle>(SeedCount);
			vertices = new List<Vector2>(SeedCount);
			_removedTris = new List<Triangle>();
			_addedTris = new List<Triangle>();
			_holePolygon = new List<Edge>();
		}

		#endregion


		#region BOUNDING BOX or SUPERTRIANGLE

		// Vertices almancenados para buscar al final todos los triangulos que lo tengan
		private Vector2[] _boundingVertices = Array.Empty<Vector2>();

		public Triangle[] GetBoundingBoxTriangles()
		{
			AABB_2D bounds = new(Vector2.one * -.1f, Vector2.one * 1.1f);
			Triangle t1 = new(bounds.BR, bounds.TL, bounds.BL);
			Triangle t2 = new(bounds.TL, bounds.BR, bounds.TR);

			t1.neighbours[0] = t2;
			t2.neighbours[0] = t1;

			_boundingVertices = bounds.Corners;

			return new[] { t1, t2 };
		}

		public void InitializeSuperTriangle()
		{
			Triangle superTri = Triangle.SuperTriangle;
			triangles.Add(superTri);
			_boundingVertices = superTri.Vertices;
		}

		// Elimina todos los Triangulos que contengan un vertice del SuperTriangulo/s
		// Reasigna el vecino a de cualquier triangulo vecino que tenga como vecino al triangulo eliminado a null
		// Y repara el borde
		public void RemoveBoundingBox() => RemoveBorder(_boundingVertices);

		private void RemoveBorder(IEnumerable<Vector2> points)
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
			List<Border> borderEdges = Borders.ToList();

			// Iteramos los ejes por PARES, para ver si son convexos o no
			// Si NO es convexo, añadimos un triangulo al borde
			var allConvex = true;
			do
			{
				for (var i = 0; i < borderEdges.Count; i++)
				{
					Border border = borderEdges[i];
					Border nextBorder = borderEdges[(i + 1) % borderEdges.Count];

					allConvex = !Edge.IsConcave(border.edge, nextBorder.edge);

					if (allConvex) continue;

					// Creamos el triangulo exterior para hacerlo convexo
					Vector2 v1 = border.edge.begin, v2 = border.edge.end, v3 = nextBorder.edge.end;
					Triangle tri = new(v3, v2, v1);
					tri.SetNeighbour(nextBorder.tri, 0);
					tri.SetNeighbour(border.tri, 1);
					triangles.Add(tri);

					// Legalizamos el Triangulo
					// Si fuera ilegal se flipea, por lo que hay que cambiar tri por el triangulo
					// del borde entre los nuevos triangulos flipeados
					if (Legalize(tri, out Triangle[] flippedTris))
						tri = flippedTris.First(t => t.IsBorder);

					// Sustituimos los bordes por el nuevo borde exterior del nuevo triangulo
					borderEdges.RemoveAt(i);
					borderEdges.RemoveAt(i >= borderEdges.Count ? 0 : i);

					Border[] newBorders = tri.BorderEdges.Select(e => new Border(tri, e)).ToArray();
					if (i >= borderEdges.Count)
						borderEdges.AddRange(newBorders);
					else
						borderEdges.InsertRange(i, newBorders);

					break;
				}
			} while (!allConvex);
		}

		private void RemovePointFromBorder(Vector2 point)
		{
			// TODO Desencapsular el RemoveBorder() de punto en punto
		}

		public struct Border
		{
			public readonly Triangle tri;
			public readonly Edge edge;

			public Vector2 PolarPos => edge.begin - Vector2.one * .5f;
			public float PolarAngle => Mathf.Atan2(PolarPos.y, PolarPos.x);

			public Border(Triangle tri, Edge edge)
			{
				this.tri = tri;
				this.edge = edge;
			}

			// Ordena el borde respecto a un centro dado
			public static IEnumerable<Border> SortByAngle(IEnumerable<Border> borders, Vector2 center) =>
				borders.OrderBy(
					e =>
					{
						Vector2 polarCoord = e.edge.begin - center;
						return Mathf.Atan2(
							polarCoord.y,
							polarCoord.x
						);
					}
				);
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR


		public void OnDrawGizmos(Matrix4x4 localToWorldMatrix, bool wire = true, bool projectOnTerrain = false)
		{
			// TRIANGULATION
			Color[] colors = Color.cyan.GetRainBowColors(triangles.Count, 0.02f);

			triangles.ForEach(
				(t, i) =>
				{
					if (ended && !wire) t.GizmosDraw(localToWorldMatrix, colors[i], projectOnTerrain);
					else t.GizmosDrawWire(localToWorldMatrix, 2, Color.white, projectOnTerrain);
				}
			);

			// ADDED TRIANGLES
			_addedTris.ForEach(
				(t, i) =>
				{
					t.GizmosDrawWire(localToWorldMatrix, 3, Color.white, projectOnTerrain);
					t.GizmosDraw(localToWorldMatrix, colors[i], projectOnTerrain);
				}
			);

			// DELETED TRIANGLES
			_removedTris.ForEach(t => t.GizmosDrawWire(localToWorldMatrix, 3, Color.red, projectOnTerrain));

			// Hole POLYGON
			_holePolygon.ForEach(e => e.DrawGizmos(localToWorldMatrix, 3, Color.green, projectOnTerrain));

			// Highlight Border
			GizmosBorder_Highlighted(localToWorldMatrix);
		}

		private void GizmosBorder_Highlighted(Matrix4x4 matrix) => GizmosExtensions.DrawPolygonWire(
			Borders
				.Select(t => t.edge.begin)
				.Select(p => matrix.MultiplyPoint3x4(p))
				.ToArray(),
			10,
			Color.red
		);


#endif

		#endregion
	}
}
