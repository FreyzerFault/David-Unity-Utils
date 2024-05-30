using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.MouseInput;
using DavidUtils.TerrainExtensions;
using UnityEngine;

namespace Geometry.Algorithms
{
	[Serializable]
	public class Voronoi
	{
		[HideInInspector] public Delaunay delaunay;

		[HideInInspector] public List<Polygon> regions;

		public List<Vector2> seeds;
		public List<Vector2> Seeds
		{
			get => seeds;
			set
			{
				seeds = value;
				Reset();
			}
		}

		private Triangle[] Triangles => delaunay.triangles.ToArray();

		public Voronoi(IEnumerable<Vector2> seeds, Delaunay delaunay = null)
		{
			this.seeds = seeds.ToList();
			regions = new List<Polygon>();
			this.delaunay = delaunay ?? new Delaunay(this.seeds);
		}

		public void Reset()
		{
			_iteration = 0;
			regions = new List<Polygon>();
			delaunay.Reset();
		}

		public void GenerateDelaunay() => delaunay.Triangulate(seeds);

		public IEnumerable<Polygon> GenerateVoronoi()
		{
			// Se necesita triangular las seeds primero.
			if (Triangles.Length == 0) GenerateDelaunay();

			foreach (Vector2 regionSeed in seeds) regions.Add(GenerateRegionPolygon(regionSeed));

			return regions;
		}

		#region PROGRESSIVE RUN

		private int _iteration;

		// Habra terminado cuando para todas las semillas haya una region
		public bool Ended => regions != null && regions.Count == seeds.Count;

		public void Run_OneIteration()
		{
			if (Ended) return;

			if (!delaunay.ended) Debug.LogError("Delaunay no ha terminado antes de generar Voronoi");

			regions.Add(GenerateRegionPolygon(seeds[_iteration]));

			_iteration++;
		}

		// Genera los vertices de una region a partir de la semilla y los triangulos generados con Delaunay
		private Polygon GenerateRegionPolygon(Vector2 seed)
		{
			var vertices = new List<Vector2>();
			Triangle[] regionTris = delaunay.FindTrianglesAroundVertex(seed).ToArray();
			AABB_2D aabb = AABB_2D.NormalizedAABB;

			// Obtenemos cada circuncentro CCW
			vertices.AddRange(regionTris.Select(t => t.GetCircumcenter()));

			if (vertices.Count == 0) return new Polygon(vertices.ToArray(), seed);

			// Para que la Region este dentro de unas fronteras
			// Aplicamos algunas modificaciones para RECORTAR o EXPANDIR la región al borde

			// EXTENDER MEDIATRIZ
			// Si la semilla forma parte del borde
			if (regionTris.Any(t => t.IsBorder))
				foreach (Triangle t in regionTris)
				{
					// Para cada triangulo del borde, si el circuncentro no esta fuera de la Bounding Box
					// Extendemos la mediatriz de la arista del borde hasta la Bounding Box hasta que intersecte
					if (aabb.OutOfBounds(t.GetCircumcenter())) continue;

					for (var i = 0; i < 3; i++)
					{
						Triangle neigh = t.neighbours[i];
						if (neigh != null) continue;

						Edge edge = t.Edges[i];
						if (edge.Vertices.All(v => v != seed)) continue;

						// Buscamos la Arista de la Bounding Box que intersecta la Mediatriz
						// Usamos un Rayo que salga del Triangulo para encontrar la interseccion
						// La direccion del rayo debe ser PERPENDICULAR a la arista hacia la derecha (90º CCW) => [-y,x]
						Vector2[] intersections = aabb.Intersections_Ray(edge.Median, edge.MediatrizRight).ToArray();

						vertices.AddRange(intersections);
					}
				}

			if (vertices.Count <= 2) return new Polygon(vertices.ToArray(), seed);


			// Ordenamos los vertices CCW antes de hacer mas modificaciones
			vertices = vertices.SortByAngle(seed).ToList();

			// RECORTE
			// Clampeamos cada Region a la Bounding Box
			vertices = aabb.CropPolygon(vertices.ToArray()).ToList();

			if (vertices.Count <= 2) return new Polygon(vertices.ToArray(), seed);

			// ESQUINAS
			// Añadimos las esquinas de la Bounding Box, buscando las regiones que tengan vertices pertenecientes a dos bordes distintos
			AABB_2D.Side? lastBorderSide;
			bool lastIsOnBorder = aabb.PointOnBorder(vertices[^1], out lastBorderSide);
			for (var i = 0; i < vertices.Count; i++)
			{
				Vector2 vertex = vertices[i];
				bool isOnBorder = aabb.PointOnBorder(vertex, out AABB_2D.Side? borderSide);
				if (!isOnBorder || !lastIsOnBorder || lastBorderSide.Value == borderSide.Value)
				{
					lastIsOnBorder = isOnBorder;
					lastBorderSide = borderSide;
					continue;
				}

				// Solo añadimos la esquina si el vertice y su predecesor pertenecen a dos bordes distintos
				Vector2 corner = aabb.GetCorner(lastBorderSide.Value, borderSide.Value);
				vertices.Insert(i, corner);
				break;
			}

			// Ordenamos los vertices CCW
			return new Polygon(vertices.SortByAngle(seed), seed);
		}

		#endregion

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

		public Polygon? GetRegion(Vector2 point) =>
			regions?.Count > 0
				? regions[GetRegionIndex(point)]
				: null;

		public int GetRegionIndex(Vector2 point)
		{
			if (regions.Count == 0 || !point.IsIn01()) return -1;

			Tuple<int, float>[] distances = regions
				.Select((r, i) => new Tuple<int, float>(i, Vector2.Distance(r.centroid, point)))
				.OrderBy(t => t.Item2)
				.ToArray();

			if (distances.Length == 0) return -1;
			return distances[0].Item1;
		}

		#region DEBUG

#if UNITY_EDITOR

		public void OnDrawGizmos(
			Matrix4x4 matrix, float centeredScale = .9f, Color[] colors = null, bool wire = false,
			bool projectOnTerrain = false
		)
		{
			if (regions is not { Count: > 0 }) return;

			if (colors is null || colors.Length != regions.Count)
				colors = Color.red.GetRainBowColors(regions.Count);

			// Region Polygons
			for (var i = 0; i < regions.Count; i++)
			{
				Polygon scaledRegion = regions[i].ScaleByCenter(centeredScale);
				if (wire) scaledRegion.OnDrawGizmosWire(matrix, 5, colors[i], projectOnTerrain);
				else scaledRegion.OnDrawGizmos(matrix, colors[i], projectOnTerrain);
			}
		}

		public void DrawRegionGizmos_Highlighted(
			Polygon region, Matrix4x4 matrix, float regionScale = .9f, bool projectOnTerrain = false
		) =>
			region.ScaleByCenter(regionScale + .01f).OnDrawGizmosWire(matrix, 5, Color.yellow, projectOnTerrain);

		public void DrawRegionGizmos_Detailed(Polygon region, Matrix4x4 matrix, bool projectOnTerrain = false)
		{
			AABB_2D aabb = AABB_2D.NormalizedAABB;

			// VERTEX in Bounding Box Edges
			foreach (Vector2 vertex in region.vertices)
			{
				Gizmos.color = aabb.PointOnBorder(vertex, out AABB_2D.Side? _) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(vertex.ToV3xz()), .1f);
			}

			// Triangulos usados para generar la region
			foreach (Triangle t in delaunay.FindTrianglesAroundVertex(region.centroid))
			{
				t.OnGizmosDrawWire(matrix, 5, Color.white, projectOnTerrain);

				// CIRCUNCENTROS
				Vector2 c = t.GetCircumcenter();

				Gizmos.color = aabb.OutOfBounds(c) ? Color.red : Color.green;
				Gizmos.DrawSphere(matrix.MultiplyPoint3x4(c.ToV3xz()), .05f);

				if (!t.IsBorder || aabb.OutOfBounds(c)) continue;

				// BORDER EDGE
				foreach (Edge borderEdge in t.BorderEdges)
				{
					if (borderEdge.begin != region.centroid && borderEdge.end != region.centroid) continue;

					// Interseccion con la Bounding Box hacia fuera del triangulo
					// Debe tener 1, porque todas las aristas deben estar dentro de la Boundig Box
					Vector2 intersections =
						aabb.Intersections_Ray(borderEdge.Median, borderEdge.MediatrizRight).First();

					Vector3 a = matrix.MultiplyPoint3x4(borderEdge.Median.ToV3xz());
					Vector3 b = matrix.MultiplyPoint3x4(intersections.ToV3xz());

					if (projectOnTerrain)
					{
						var terrain = Terrain.activeTerrain;
						a = terrain.Project(a);
						b = terrain.Project(b);
					}

					GizmosExtensions.DrawLineThick(a, b, 6, Color.red);
				}
			}
		}

		public bool MouseInRegion(out int regionIndex, Vector3 originPos, Vector2 size)
		{
			regionIndex = -1;
			MouseInputUtils.MouseInArea_CenitalView(originPos, size, out Vector2 normalizedPos);
			for (var i = 0; i < regions.Count; i++)
			{
				if (!regions[i].Contains_RayCast(normalizedPos)) continue;
				regionIndex = i;
				return true;
			}

			return false;
		}

#endif

		#endregion
	}
}
