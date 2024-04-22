using System;
using System.Linq;
using DavidUtils.DebugExtensions;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
	public class Voronoi
	{
		public Vector2[] seeds;
		public Polygon[] regions;

		public Delaunay delaunay = new();
		private Delaunay.Triangle[] _triangles = Array.Empty<Delaunay.Triangle>();

		private Vector3[] SeedsInWorld => seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public Voronoi(Vector2[] seeds)
		{
			this.seeds = seeds;
			regions = new Polygon[seeds.Length];
		}

		public Voronoi(int numSeeds)
			: this(GeometryUtils.GenerateRandomSeeds_RegularDistribution(numSeeds))
		{
		}
		
		public void GenerateSeeds(int numSeeds = -1)
		{
			Reset();
			seeds = GeometryUtils.GenerateRandomSeeds_RegularDistribution(numSeeds == -1 ? seeds.Length : numSeeds);
			delaunay.vertices = seeds;
		}

		public void Reset()
		{
			_iteration = 0;
			_triangles = Array.Empty<Delaunay.Triangle>();
			delaunay.Reset();
			regions = Array.Empty<Polygon>();
		}

		public void GenerateDelaunay()
		{
			delaunay.vertices = seeds;
			_triangles = delaunay.Triangulate(seeds);
		}
		
		public void GenerateVoronoi()
		{
			if (_triangles.Length == 0) GenerateDelaunay();
			
			// TODO: Implementar Voronoi usando GenerateVoronoi_OneIteration() iterativamente
		}

		#region PROGRESSIVE RUN

		private int _iteration;
		
		public void GenerateVoronoi_OneIteration()
		{
			if (_triangles.Length == 0) GenerateDelaunay();
			
			// TODO: Implementar Voronoi Iteration

			_iteration++;
		}

		#endregion
		
		
		#region DEBUG

		public void OnDrawGizmos(Vector3 pos, Vector2 size, Color seedColor = default)
		{
			// SEEDS
			Gizmos.color = seedColor;
			foreach (Vector2 seed in seeds)
			{
				Vector2 seedScaled = seed * size;
				Vector3 seedPos = new Vector3(seedScaled.x, 0, seedScaled.y) + pos;
				Gizmos.DrawSphere(seedPos, .1f);
			}

			// GRID
			Gizmos.color = Color.blue;
			GizmosExtensions.DrawQuadWire(pos, size.ToVector3xz());
			
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
			GizmosExtensions.DrawGrid(cellRows, cellRows, pos, size);
			
			// REGIONES
			Color[] colors = Color.red.GetRainBowColors(regions.Length);
			for (var i = 0; i < regions.Length; i++)
				regions[i].OnDrawGizmos(pos, size, 5, colors[i]);
		}

		#endregion
	}
}
