using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugExtensions;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry
{
	[Serializable]
	public class Voronoi
	{
		public enum SeedDistribution { Random, Regular, SinWave }

		public int seed = 10;
		public SeedDistribution seedDistribution = SeedDistribution.Random;

		[HideInInspector] public Delaunay delaunay = new();

		[HideInInspector] public Vector2[] seeds;
		[HideInInspector] public List<Polygon> regions;

		private Delaunay.Triangle[] Triangles => delaunay.triangles.ToArray();

		private Vector3[] SeedsInWorld => seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public Voronoi(Vector2[] seeds)
		{
			this.seeds = seeds;
			regions = new List<Polygon>();
		}

		public Voronoi(int numSeeds)
			: this(Array.Empty<Vector2>()) => GenerateSeeds(numSeeds);

		public void GenerateSeeds(int numSeeds = -1)
		{
			Reset();
			Random.InitState(seed);
			numSeeds = numSeeds == -1 ? seeds.Length : numSeeds;
			seeds = seedDistribution switch
			{
				SeedDistribution.Random => GeometryUtils.GenerateSeeds_RandomDistribution(numSeeds),
				SeedDistribution.Regular => GeometryUtils.GenerateSeeds_RegularDistribution(numSeeds),
				SeedDistribution.SinWave => GeometryUtils.GenerateSeeds_WaveDistribution(numSeeds),
				_ => GeometryUtils.GenerateSeeds_RegularDistribution(numSeeds)
			};

			// Las convertimos en vertices para triangularlos con Delaunay primero
			delaunay.vertices = seeds;
		}

		public void Reset()
		{
			iteration = 0;
			regions = new List<Polygon>();
			delaunay.Reset();
		}

		public void GenerateDelaunay()
		{
			delaunay.vertices = seeds;
			delaunay.Triangulate(seeds);
		}

		public Polygon[] GenerateVoronoi()
		{
			// Se necesita triangular las seeds primero.
			if (Triangles.Length == 0) GenerateDelaunay();

			for (var i = 0; i < seeds.Length; i++)
				GenerateRegion(i);

			return regions.ToArray();
		}

		#region PROGRESSIVE RUN

		public int iteration;

		// Habra terminado cuando para todas las semillas haya una region
		public bool Ended => regions.Count == seeds.Length;

		public void Run_OneIteration()
		{
			if (Ended) return;

			if (Triangles.Length == 0) GenerateDelaunay();

			GenerateRegion(iteration);

			iteration++;
		}

		// Genera una region a partir de una semilla
		private void GenerateRegion(int seedIndex)
		{
			Vector2 seed = seeds[seedIndex];

			// Filtramos todos los triangulos alrededor de la semilla
			Delaunay.Triangle[] regionTris = delaunay.FindTrianglesAroundVertex(seed);
			
			var bounds = new Bounds2D(Vector2.zero, Vector2.one);

			// Obtenemos cada circuncentro CCW
			// Los ordenamos en sentido antihorario (a partir de su Coord. Polar respecto a la semilla)
			var polygon = new List<Vector2>();
			foreach (Delaunay.Triangle tri in regionTris)
			{
				Vector2? circumcenter = tri.GetCircumcenter();

				// Podria ser un triangulo con vertices colineales. En ese caso lo ignoramos
				if (!circumcenter.HasValue) continue;

				Vector2 c = circumcenter.Value;

				polygon.Add(c);
				
				if (!tri.IsBorder || bounds.OutOfBounds(c)) continue;
				// Si el triangulo forma parte del borde y su circuncentro no esta fuera de la BB
				// Añadimos un vertice al poligono que sera la interseccion de la mediatriz con la BB
				for (var i = 0; i < 3; i++)
				{
					if (tri.neighbours[i] != null) continue;
				
					Delaunay.Edge borderEdge = tri.Edges[i];
					Vector2 m = (borderEdge.begin + borderEdge.end) / 2;
				
					// Buscamos la Arista de la Bounding Box que intersecta el Rayo [Circuncentro - Mediatriz]
					Vector2 intersection = bounds.Intersections_Ray(c, m - c).First();
				
					polygon.Add(intersection);
				}
			}
			
			// Clampeamos cada Region a la Bounding Box
			for (var i = 0; i < polygon.Count; i++)
			{
				Vector2 vertex = polygon[i];
				
				if (bounds.Contains(vertex)) continue;
				
				// Si esta fuera de la Bounding Box, buscamos la interseccion de sus aristas con la BB
				Vector2 prev = polygon[(i - 1 + polygon.Count) % polygon.Count];
				Vector2 next = polygon[(i + 1) % polygon.Count];
				IEnumerable<Vector2> i1 = bounds.Intersections_Segment(vertex, prev);
				IEnumerable<Vector2> i2 = bounds.Intersections_Segment(vertex, next);
				
				// Borramos el vertice actual
				polygon.RemoveAt(i);
				
				// Insertamos tantos vertices como intersecciones haya
				polygon.InsertRange(i, i2);
				polygon.InsertRange(i, i1);
			}
			
			// Añadimos las esquinas de la Bounding Box, buscando las regions que tengan vertices pertenecientes a dos bordes distintos
			Bounds2D.Side lastBorderSide;
			bool lastIsOnBorder = bounds.PointOnBorder(polygon[0], out lastBorderSide);
			for (var i = 1; i < polygon.Count; i++)
			{
				Vector2 vertex = polygon[i];
				bool isOnBorder = bounds.PointOnBorder(vertex, out Bounds2D.Side borderSide);
				if (!isOnBorder || !lastIsOnBorder || lastBorderSide == borderSide) continue;
				
				// Solo añadimos la esquina si el vertice y su predecesor pertenecen a dos bordes distintos
				Vector2 borderVertex = bounds.GetCorner(lastBorderSide, borderSide);
				polygon.Insert(i, borderVertex);
				break;
			}

			// Creamos la región
			regions.Add(
				new Polygon(polygon 
						//  Ordenamos los vertices CCW
						.OrderBy(p => Vector2.SignedAngle(Vector2.right, p - seed))
						.ToArray(), 
					seed));
		}


		public IEnumerator AnimationCoroutine(float delay = 0.1f)
		{
			while (!Ended)
			{
				Run_OneIteration();
				yield return new WaitForSecondsRealtime(delay);
			}

			yield return null;
		}

		#endregion


		#region DEBUG

		public bool drawSeeds = true;
		public bool drawGrid = true;
		public bool drawRegions = true;
		public bool wireRegions = false;
		public bool drawDelaunayTriangulation = false;
		public bool wireTriangulation = true;
		public bool projectOnTerrain = true;

		public void OnDrawGizmos(Vector3 pos, Vector2 size)
		{
			GizmosSeeds(pos, size);
			GizmosGrid(pos, size);
			GizmosRegions(pos, size, wireRegions);

			// DELAUNAY
			if (drawDelaunayTriangulation)
				delaunay?.OnDrawGizmos(pos, size, wireTriangulation, projectOnTerrain);
		}

		// Draw Seeds as Spheres
		private void GizmosSeeds(Vector3 pos, Vector2 size)
		{
			if (!drawSeeds) return;

			Color[] colors = Color.red.GetRainBowColors(regions.Count);
			Gizmos.color = Color.grey;

			HashSet<Delaunay.Triangle> drawnedTri = new HashSet<Delaunay.Triangle>();

			for (var i = 0; i < seeds.Length; i++)
			{
				Vector2 seed = seeds[i];
				Color color = colors[i];
				// var seedTris = delaunay.FindTrianglesAroundVertex(seed);
				// if (seedTris is { Length: > 0 } && seedTris.All(t => !drawnedTri.Contains(t)))
				// 	foreach (Delaunay.Triangle triangle in seedTris)
				// 	{
				// 		if (!drawnedTri.Add(triangle)) continue;
				// 		triangle.OnGizmosDraw(pos, size, color);
				// 	}

				Vector2 seedScaled = seed * size;
				Vector3 seedPos = new Vector3(seedScaled.x, 0, seedScaled.y) + pos;
				Gizmos.DrawSphere(seedPos, .1f);
			}
		}

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		private void GizmosGrid(Vector3 pos, Vector2 size)
		{
			if (!drawGrid) return;

			Gizmos.color = Color.blue;
			if (seedDistribution == SeedDistribution.Random)
			{
				// Surrounding Bound only
				GizmosExtensions.DrawQuadWire(pos + size.ToVector3xz() / 2, size, Quaternion.FromToRotation(Vector3.up, Vector3.forward), 5, Color.blue);
			}
			else
			{
				// GRID
				int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
				GizmosExtensions.DrawGrid(cellRows, cellRows, pos + size.ToVector3xz() / 2, size, Quaternion.FromToRotation(Vector3.up, Vector3.forward), 5, Color.blue);
			}
		}

		private void GizmosRegions(Vector3 pos, Vector2 size, bool wire = false)
		{
			if (!drawRegions || regions is not { Count: > 0 }) return;

			Color[] colors = Color.red.GetRainBowColors(regions.Count);
			for (var i = 0; i < regions.Count; i++)
			{
				Vector3 regionCentroid = pos + (regions[i].centroid * size).ToVector3xz();
				if (wire)
					regions[i].OnDrawGizmosWire(regionCentroid, size, 0.99f, 5, colors[i]);
				else
					regions[i].OnDrawGizmos(regionCentroid, size, 0.9f, colors[i]);
			}
		}

		#endregion
	}
}
