using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry
{
	public class Voronoi
	{
		private readonly Vector2[] seeds;
		private readonly Polygon[] _regions;

		private Vector3[] SeedsInWorld => seeds.Select(seed => new Vector3(seed.x, 0, seed.y)).ToArray();

		public Voronoi(Vector2[] seeds)
		{
			this.seeds = seeds;
			_regions = new Polygon[seeds.Length];
			GenerateVoronoi();
		}

		public Voronoi(int numSeeds)
			: this(GenerateRandomSeeds_RegularDistribution(numSeeds))
		{
		}

		private static Vector2[] GenerateRandomSeeds(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			for (var i = 0; i < numSeeds; i++) seeds[i] = new Vector2(Random.value, Random.value);
			return seeds;
		}

		private static Vector2[] GenerateRandomSeeds_RegularDistribution(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(numSeeds));
			float cellSize = 1f / cellRows;

			int cellRow = 0, cellCol = 0;

			for (var i = 0; i < numSeeds; i++)
			{
				var cellOrigin = new Vector2(cellRow * cellSize, cellCol * cellSize);
				seeds[i] = new Vector2(Random.value * cellSize, Random.value * cellSize) + cellOrigin;

				// Next Row
				cellRow = (cellRow + 1) % cellRows;

				// Jump Column
				if (cellRow == cellRows - 1)
					cellCol = (cellCol + 1) % cellRows;
			}

			return seeds;
		}

		private void GenerateVoronoi()
		{
			// TODO
			// Delaunay.Triangle[] triangles = Delaunay.Triangulate(seeds);
		}

		public void OnDrawGizmos(Vector3 pos, float size, Color seedColor = default)
		{
			Gizmos.color = seedColor;
			foreach (Vector2 seed in seeds)
			{
				Vector2 seedScaled = seed * size;
				Vector3 seedPos = new Vector3(seedScaled.x, 0, seedScaled.y) + pos;
				Gizmos.DrawSphere(seedPos, 1f);
			}

			Color[] colors = Color.red.GetRainBowColors(_regions.Length);
			for (var i = 0; i < _regions.Length; i++)
				_regions[i].OnDrawGizmos(colors[i]);


			Gizmos.color = Color.blue;
			var quad = new Quad(pos + new Vector3(1, 0, 1) * size / 2, size, Vector3.up);
			Gizmos.DrawLineStrip(quad.vertices, true);


			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Length));
			float cellSize = 1f / cellRows;

			for (var y = 0; y < cellRows; y++)
			for (var x = 0; x < cellRows; x++)
			{
				var cellQuad = new Quad(pos + new Vector3(x, 0, y) * cellSize * size, cellSize * size, Vector3.up);
				Gizmos.DrawLineStrip(cellQuad.vertices, true);
			}
		}
	}
}
