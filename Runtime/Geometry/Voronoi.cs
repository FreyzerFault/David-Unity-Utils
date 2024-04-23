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
	public class Voronoi
	{
		public enum SeedDistribution { Random, Regular, SinWave }

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

			// Obtenemos cada circuncentro CCW
			// Los ordenamos en sentido antihorario (a partir de su Coord. Polar respecto a la semilla)
			var polygon = new List<Vector2>();
			foreach (Delaunay.Triangle tri in regionTris)
			{
				Vector2? circumcenter = tri.GetCircumcenter();

				// Podria ser un triangulo con vertices colineales. En ese caso lo ignoramos
				if (!circumcenter.HasValue) continue;

				polygon.Add(circumcenter.Value);

				if (!tri.IsBorder) continue;

				// Si es borde, añadimos el punto de intersección con el borde
				for (var i = 0; i < 3; i++)
				{
					if (tri.neighbours[i] != null) continue;

					// ES BORDE:
					// Mediatriz = [circunc. -> p medio = (a+b)/2] 
					Delaunay.Edge borderEdge = tri.Edges[i];
					Vector2 m = (borderEdge.begin + borderEdge.end) / 2;
					Vector2 mediatriz = (m - circumcenter.Value).normalized;

					// Buscamos la Arista de la Bounding Box mas cercana a la mediatriz
					Vector2[] boxVertices = { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
					Delaunay.Edge[] boxEdges =
					{
						new(boxVertices[0], boxVertices[1]),
						new(boxVertices[1], boxVertices[2]),
						new(boxVertices[2], boxVertices[3]),
						new(boxVertices[3], boxVertices[0])
					};

					Delaunay.Edge nearestEdge = boxEdges
						.OrderBy(e => GeometryUtils.DistanceToLine(m, e.begin, e.end))
						.First();

					// Calculamos la interseccion de la mediatriz con el borde
					Vector2? intersection = GeometryUtils.IntersectionPoint(
						circumcenter.Value,
						circumcenter.Value + mediatriz,
						nearestEdge.begin,
						nearestEdge.end
					);

					if (intersection.HasValue)
						polygon.Add(intersection.Value);
				}
			}

			// Creamos la región
			regions.Add(new Polygon(polygon.ToArray(), seed));
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
		public bool drawDelaunayTriangulation = true;
		public bool projectOnTerrain = true;

		public void OnDrawGizmos(Vector3 pos, Vector2 size)
		{
			GizmosSeeds(pos, size);
			GizmosGrid(pos, size);
			GizmosRegions(pos, size);

			// DELAUNAY
			if (drawDelaunayTriangulation)
				delaunay?.OnDrawGizmos(pos, size, projectOnTerrain);
		}

		// Draw Seeds as Spheres
		private void GizmosSeeds(Vector3 pos, Vector2 size)
		{
			if (!drawSeeds) return;

			Gizmos.color = Color.grey;
			foreach (Vector2 seed in seeds)
			{
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
				GizmosExtensions.DrawQuadWire(pos, size.ToVector3xz());
			}
			else
			{
				// GRID
				int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
				GizmosExtensions.DrawGrid(cellRows, cellRows, pos, size);
			}
		}

		private void GizmosRegions(Vector3 pos, Vector2 size)
		{
			if (!drawRegions || regions is not { Count: > 0 }) return;

			Color[] colors = Color.red.GetRainBowColors(regions.Count);
			for (var i = 0; i < regions.Count; i++)
				regions[i].OnDrawGizmos(pos, size, 5, colors[i]);
		}

		#endregion
	}
}
