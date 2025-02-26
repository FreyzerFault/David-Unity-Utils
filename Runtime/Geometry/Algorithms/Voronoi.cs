using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.MouseInputs;
using Geometry.Algorithms;
using UnityEngine;

namespace DavidUtils.Geometry.Algorithms
{
	[Serializable]
	public class Voronoi
	{
		// Distancia mínima a la que pueden estar los vértices.
		// Una vez se genera el Voronoi, se simplifican los polígonos para evitar colisiones
		private const float VERTEX_COLLISION_RADIUS = 0.01f;

		[HideInInspector] public Delaunay delaunay;
		public List<Polygon> polygons = new();

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

		public int PolygonCount => polygons?.Count ?? 0;
		public int SeedCount => _seeds?.Count ?? 0;

		// Vertices sin repetir de todas las regiones
		public Vector2[] AllVertices => polygons.SelectMany(r => r.Vertices).Distinct().ToArray();

		public Action<Polygon> onPolygonAdded;

		public Voronoi(IEnumerable<Vector2> seeds, Delaunay delaunay = null)
		{
			_seeds = seeds.ToList();
			this.delaunay = delaunay ?? new Delaunay(_seeds);
			polygons = new List<Polygon>(SeedCount);
		}

		public void Reset()
		{
			polygons = new List<Polygon>(SeedCount);
			_iteration = 0;
		}

		#region GENERATION

		public void GenerateDelaunay() => delaunay.Triangulate(_seeds);

		public IEnumerable<Polygon> GenerateVoronoi()
		{
			// Se necesita triangular las seeds primero.
			if (delaunay.NotGenerated) GenerateDelaunay();

			foreach (Vector2 seed in _seeds)
			{
				Polygon poly = GeneratePolygon(seed);
				polygons.Add(poly);
				onPolygonAdded?.Invoke(poly);
			}

			return polygons;
		}

		// Genera los vertices de un poligono a partir de la semilla y los triangulos generados con Delaunay
		private Polygon GeneratePolygon(Vector2 seed)
		{
			var polygon = new Polygon();
			Triangle[] tris = delaunay.FindTrianglesAroundVertex(seed).ToArray();
			Triangle[] borderTris = tris
				.Where(t => t.IsBorder && t.BorderEdges.Any(e => e.Vertices.Any(v => v == seed)))
				.ToArray();
			bool isBorder = borderTris.NotNullOrEmpty();
			AABB_2D aabb = AABB_2D.NormalizedAABB;

			// Obtenemos cada circuncentro CCW
			polygon.Vertices = tris.Select(t => t.GetCircumcenter()).ToArray();

			// if (polygon.VertexCount == 0) return polygon;

			// Para que el Poligono este dentro de unas fronteras
			// Aplicamos algunas modificaciones para RECORTAR o EXPANDIR el poligono al borde

			// ORDENAMOS CCW
			polygon = polygon.SortCCW();

			// RECORTE
			// Clampeamos cada Poligono a la Bounding Box
			polygon = aabb.CropPolygon(polygon);

			// EXTENDER MEDIATRIZ
			// Si la semilla forma parte del borde
			if (isBorder)
			{
				// Para cada triangulo del borde, si el circuncentro no esta fuera de la Bounding Box
				// Extendemos la mediatriz de la arista del borde hasta la Bounding Box hasta que intersecte
				Edge[] mediatrizEdges = borderTris
					// Triangulos con Circuncentro dentro de la Bounding Box
					.Where(t => aabb.Contains(t.GetCircumcenter()))
					// Cogemos solo las aristas borde que conectan con la semilla
					.SelectMany(t => t.BorderEdges.Where(e => e.Vertices.Any(v => v == seed)))
					.ToArray();

				// Usamos un Rayo de la Mediatriz hacia fuera (derecha)
				// Añadimos las intersecciones con el AABB como nuevos vertices
				polygon.Vertices = polygon.Vertices
					.Concat(mediatrizEdges.Select(e => aabb.Intersections_Ray(e.Median, e.MediatrizRightDir).First()))
					.ToArray();

				polygon = polygon.SortCCW();
			}

			if (polygon.VertexCount < 2) return polygon;

			// Eliminamos repetidos
			polygon.Vertices = polygon.Vertices.Distinct().ToArray();

			if (polygon.VertexCount < 2) return polygon;

			// ESQUINAS
			// Añadimos las esquinas de la Bounding Box
			// buscando los poligonos que tengan vertices adyacentes pertenecientes a dos bordes distintos
			if (isBorder)
			{
				List<Vector2> vertices = polygon.Vertices.ToList();

				for (var i = 0; i < polygon.VertexCount; i++)
				{
					Edge edge = polygon.Edges[i];

					// NOT CORNERS
					if (aabb.Corners.Contains(edge.begin) || aabb.Corners.Contains(edge.end)) continue;

					// BOTH on Borders
					bool beginOnBorder = aabb.PointOnBorder(edge.begin, out AABB_2D.Side? beginBorderSide);
					bool endOnBorder = aabb.PointOnBorder(edge.end, out AABB_2D.Side? endBorderSide);
					bool bothOnBorder = endOnBorder && beginOnBorder;

					// Borders of each vertex are DIFFERENT
					bool bordersAreDifferent = bothOnBorder && beginBorderSide!.Value != endBorderSide!.Value;

					if (!bordersAreDifferent) continue;

					// Añadimos la esquina si el eje empieza y acaba en un borde distinto, y si no esta ya añadida
					Vector2 corner = aabb.GetCorner(beginBorderSide.Value, endBorderSide.Value);
					if (vertices.All(v => corner != v))
						vertices.Insert(i + 1, corner);
				}

				// Hay una o mas esquinas añadidas
				// Problema: Puede que haya vertices colineares que sobran
				// Asignamos como vertices SOLO si no son colineares con sus vecinos
				if (vertices.Count > polygon.VertexCount)
					for (var i = 0; i < vertices.Count; i++)
					{
						Vector2 prev = vertices[(i - 1 + vertices.Count) % vertices.Count];
						Vector2 vertex = vertices[i];
						Vector2 next = vertices[(i + 1) % vertices.Count];

						if (!GeometryUtils.PointOnLine(prev, next, vertex)) continue;

						// Vertex es COLINEAR con sus vecinos, lo eliminamos
						vertices.RemoveAt(i);
						i--;
					}

				polygon.Vertices = vertices.ToArray();
			}
			
			return polygon.SortCCW();
		}

		#endregion


		#region POSTPROCESADO

		/// <summary>
		///     SIMPLIFICACION de los POLÍGONOS
		///     Convertimos los vertices demasiado cerca (a una distancia mínima de colisión) en su centroide
		/// </summary>
		public void SimplifyPolygons()
		{
			// Se guardan en este mapa [Vértice => Centroide de las colisiones]
			Dictionary<Vector2, Vector2> simplifications = new();

			List<Vector2> allVertices = AllVertices.ToList();
			for (var i = 0; i < allVertices.Count; i++)
			{
				Vector2 vertex = allVertices[i];
				Vector2[] collisions = allVertices
					.Except(new[] { vertex })
					.Where(v => Vector2.Distance(v, vertex) < VERTEX_COLLISION_RADIUS)
					.ToArray();

				if (collisions.Length == 0) continue;

				Vector2 centroid = collisions.Append(vertex).Center();

				// Guardamos el centroide de los vertices que hemos simplificado para luego sustituirlos en cada polígono
				simplifications.Add(vertex, centroid);
				collisions.ForEach(
					c =>
					{
						// Si la colision fue un centroide, al que apunta otro vértice
						// actualizamos su centroide por este
						KeyValuePair<Vector2, Vector2>[] lastCollisionsToMerge =
							simplifications.Where(pair => pair.Value == c).ToArray();

						if (lastCollisionsToMerge.IsNullOrEmpty())
							simplifications.Add(c, centroid);
						else
							foreach (Vector2 originalPoint in lastCollisionsToMerge.Select(pair => pair.Key))
								simplifications[originalPoint] = centroid;
					}
				);

				// Los sustituimos por el centroide para comparar los siguientes con este
				allVertices[i] = centroid;
				collisions.ForEach(c => allVertices.Remove(c));
			}

			// Para cada polígono aplicamos las simplificaciones a sus vértices
			polygons = polygons
				.Select(
					r =>
						new Polygon(
							// Si se simplificó, lo sustituimos
							r.Vertices.Select(v => simplifications.GetValueOrDefault(v, v))
								// Como varios vértices se sustituirán por el mismo, eliminamos los vertices repetidos
								.Distinct()
								.ToArray()
						)
				)
				.ToList();
		}

		#endregion


		#region PROGRESSIVE RUN

		private int _iteration;

		// Habra terminado cuando para todas las semillas haya un poligono
		public bool Ended => (polygons.NotNullOrEmpty() && polygons.Count == _seeds.Count) || _iteration >= _seeds.Count;

		public void Run_OneIteration()
		{
			if (Ended) return;

			if (!delaunay.ended) Debug.LogError("Delaunay no ha terminado antes de generar Voronoi");

			Polygon poly = GeneratePolygon(_seeds[_iteration]);
			polygons.Add(poly);
			onPolygonAdded?.Invoke(poly);

			_iteration++;
		}

		#endregion


		#region BORDES

		/// <summary>
		///     Comprueba, a partir de sus triangulos que la rodean, si esta semilla forma parte del borde del Voronoi
		///     (No de la Bounding Box)
		///     Buscamos sus triangulos del borde (les falta un vecino) y cogemos el EJE que forma parte del borde (mismo index que
		///     el vecino)
		///     Si la semilla es uno de los vertices de ese eje, significa que está en el borde
		/// </summary>
		private bool SeedInBorder(
			Vector2 seed, out List<Edge> borderEdges, out List<Vector2> circumcenters,
			Triangle[] tris = null
		)
		{
			borderEdges = new List<Edge>();
			circumcenters = new List<Vector2>();

			tris ??= delaunay.FindTrianglesAroundVertex(seed).ToArray();

			// Si no hay ningun triangulo del borde, la seed no puede serlo
			if (tris.All(t => !t.IsBorder)) return false;

			// Buscamos los 2 Triangulos del Borde, y cogemos los ejes del borde (triangulo vecino == null)
			// Uno de sus vertices debe ser la semilla para constar como borde
			foreach (Triangle borderTri in tris.Where(t => t.IsBorder))
				for (var i = 0; i < 3; i++)
				{
					if (borderTri.neighbours[i] != null) continue;

					// Si la semilla no esta en el eje del borde, no se considera borde
					if (borderTri.Edges[i].Vertices.All(v => v != seed)) return false;

					borderEdges.Add(borderTri.Edges[i]);
					circumcenters.Add(borderTri.GetCircumcenter());
				}

			if (borderEdges.Count == 2) return true;
			throw new Exception(
				$"Se han encontrado {borderEdges.Count} ejes del borde. Algo ha ido mal porque debería ser 2"
			);
		}

		#endregion


		#region TEST INSIDE POLYGON

		public Polygon? GetPolygon(Vector2 point) =>
			polygons?.Count > 0 ? polygons[GetPolygonIndex(point)] : null;

		public int GetPolygonIndex(Vector2 point)
		{
			if (polygons.Count == 0 || !point.IsIn01()) return -1;

			// Distancias con cada centroide
			Tuple<int, float>[] distances = _seeds
				.Select((s, i) => new Tuple<int, float>(i, Vector2.Distance(s, point)))
				// Ordenamos por distancia -> La 1º será la más cercana
				.OrderBy(t => t.Item2)
				.ToArray();

			if (distances.Length == 0) return -1;
			return distances[0].Item1;
		}

		public bool IsInsidePolygon(Vector2 point, int polygonIndex) =>
			GetPolygonIndex(point) == polygonIndex;

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		public void OnDrawGizmos(
			Matrix4x4 matrix, float centeredScale = .9f, Color[] colors = null, bool wire = false,
			bool projectOnTerrain = false
		)
		{
			if (polygons is not { Count: > 0 }) return;

			if (colors is null || colors.Length != polygons.Count)
				colors = Color.red.GetRainBowColors(polygons.Count);

			// Polygons
			for (var i = 0; i < polygons.Count; i++)
			{
				Polygon scaledPolygon = polygons[i].ScaleByCenter(centeredScale);
				if (wire) scaledPolygon.DrawGizmosWire(matrix, 5, colors[i], projectOnTerrain);
				else scaledPolygon.DrawGizmos(matrix, colors[i], projectOnTerrain: projectOnTerrain);
			}
		}

		public void DrawPolygonGizmos_Highlighted(
			int index, Matrix4x4 matrix, float polygonScale = .9f, bool projectOnTerrain = false
		)
		{
			if (index < 0 || index >= polygons.Count) return;
			polygons[index]
				.ScaleByCenter(polygonScale + .01f)
				.DrawGizmosWire(matrix, 5, Color.yellow, projectOnTerrain);
		}

		public void DrawPolygonGizmos_Detailed(int index, Matrix4x4 localToWorldMatrix, bool projectOnTerrain = false)
		{
			if (index < 0 || index >= polygons.Count) return;
			Polygon polygon = polygons[index];
			Vector2 seed = _seeds[index];

			// localToWorldMatrix *= Matrix4x4.Translate(Vector3.back * 5);


			// VERTEX in Bounding Box Edges => Draw in red
			polygon.DrawGizmosVertices_CheckAABBborder(localToWorldMatrix);

			// Triangulos usados para generar el poligono
			foreach (Triangle t in delaunay.FindTrianglesAroundVertex(seed))
			{
				t.GizmosDrawWire(localToWorldMatrix, 6, Color.cyan, projectOnTerrain);

				// CIRCUNCENTROS
				t.GizmosDrawCircumcenter(localToWorldMatrix);

				// BORDER EDGE
				foreach (Edge borderEdge in t.BorderEdges)
				{
					// Resalta el borde
					borderEdge.DrawGizmos(localToWorldMatrix, 7, Color.red, projectOnTerrain);

					// Extension de Mediatrices hacia el AABB
					if (borderEdge.begin != seed && borderEdge.end != seed) continue;

					// Interseccion con la Bounding Box hacia fuera del triangulo
					// Debe tener 1, porque todas las aristas deben estar dentro de la Boundig Box
					Vector2 intersection = AABB_2D.NormalizedAABB
						.Intersections_Ray(borderEdge.Median, borderEdge.MediatrizRightDir)
						.First();

					Vector3 a = localToWorldMatrix.MultiplyPoint3x4(borderEdge.Median);
					Vector3 b = localToWorldMatrix.MultiplyPoint3x4(intersection);

					var terrain = Terrain.activeTerrain;
					if (projectOnTerrain && terrain != null)
					{
						a = terrain.Project(a);
						b = terrain.Project(b);
					}

					if (projectOnTerrain && terrain != null)
						GizmosExtensions.DrawLineThick(
							terrain.ProjectSegmentToTerrain(a, b),
							6,
							Color.red
						);
					else
						GizmosExtensions.DrawLineThick(a, b, 6, Color.red);
				}
			}
		}

		public bool MouseInPolygon(out int polygonIndex, Vector3 originPos, Vector2 size)
		{
			polygonIndex = -1;
			MouseInputUtils.MouseInArea_CenitalView(originPos, size, out Vector2 normalizedPos); 
			polygonIndex = GetPolygonIndex(normalizedPos);
			return polygonIndex >= 0 && polygonIndex < PolygonCount;
		}

#endif

		#endregion
	}
}
