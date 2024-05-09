using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators
{
	public class SeedsGenerator : MonoBehaviour
	{
		public enum SeedsDistribution { Random, Regular, SinWave }

		public int randSeed = 10;

		public int numSeeds = 10;
		public SeedsDistribution seedsDistribution = SeedsDistribution.Random;

		[HideInInspector] public List<Vector2> seeds = new();
		public List<Vector2> Seeds
		{
			get => seeds;
			set
			{
				seeds = value;
				numSeeds = seeds.Count;
				OnSeedsUpdated();
			}
		}

		public bool SeedsAreGenerated => seeds?.Count == numSeeds;

		#region TO WORLD COORDS

		protected Vector3[] Seeds3D_XY => Seeds.Select(seed => seed.ToV3xy()).ToArray();
		protected Vector3[] Seeds3D_XZ => Seeds.Select(seed => seed.ToV3xz()).ToArray();

		protected Vector3[] SeedsInWorld_XZ =>
			Seeds.Select(s => transform.localToWorldMatrix.MultiplyPoint3x4(s.ToV3xz())).ToArray();
		protected Vector3[] SeedsInWorld_XY =>
			Seeds.Select(s => transform.localToWorldMatrix.MultiplyPoint3x4(s.ToV3xy())).ToArray();

		#endregion

		public Bounds2D Bounds => Bounds2D.NormalizedBounds;

		protected virtual void Awake() => GenerateSeeds();

		protected virtual void OnSeedsUpdated()
		{
		}

		public void RandomizeSeeds()
		{
			randSeed = Random.Range(1, int.MaxValue);
			GenerateSeeds();
		}

		public void GenerateSeeds() => Seeds = GenerateSeeds(numSeeds, randSeed, seedsDistribution).ToList();

		/// <summary>
		///     Genera un set de puntos 2D random dentro del rango [0,0] -> [1,1]
		///     Con distintos tipos de distribución
		/// </summary>
		public static Vector2[] GenerateSeeds(
			int numSeeds, int randSeed = 1, SeedsDistribution seedsDistribution = SeedsDistribution.Regular
		)
		{
			Random.InitState(randSeed);

			return seedsDistribution switch
			{
				SeedsDistribution.Random => GenerateSeeds_RandomDistribution(numSeeds),
				SeedsDistribution.Regular => GenerateSeeds_RegularDistribution(numSeeds),
				SeedsDistribution.SinWave => GenerateSeeds_WaveDistribution(numSeeds),
				_ => GenerateSeeds_RegularDistribution(numSeeds)
			};
		}


		/// <summary>
		///     Genera un set de puntos 2D random dentro del rango [0,0] -> [1,1]
		/// </summary>
		public static Vector2[] GenerateSeeds_RandomDistribution(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			for (var i = 0; i < numSeeds; i++) seeds[i] = new Vector2(Random.value, Random.value);
			return seeds;
		}

		/// <summary>
		///     Genera un set de puntos 2D random dentro del rango [0,0] -> [1,1]
		///     Repartidos equitativamente en una grid NxN
		///     N = floor(sqrt(numSeeds))
		///     9 seeds => 3x3; 10 seeds => 3x3, el restante se genera empezando desde 0,0 en adelante
		/// </summary>
		public static Vector2[] GenerateSeeds_RegularDistribution(int numSeeds)
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

		/// <summary>
		///     Genera un set de puntos 2D en el rango [0,0] -> [1,1]
		///     Forman una onda senoidal en una grid NxN
		/// </summary>
		public static Vector2[] GenerateSeeds_WaveDistribution(int numSeeds)
		{
			var seeds = new Vector2[numSeeds];
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(numSeeds));
			float cellSize = 1f / cellRows;

			int cellRow = 0, cellCol = 0;

			for (var i = 0; i < numSeeds; i++)
			{
				var cellOrigin = new Vector2(cellRow * cellSize, cellCol * cellSize);
				seeds[i] = new Vector2(.5f * cellSize, (Mathf.Sin(i) + 1) / 2 * cellSize) + cellOrigin;

				// Next Row
				cellRow = (cellRow + 1) % cellRows;

				// Jump Column
				if (cellRow == cellRows - 1)
					cellCol = (cellCol + 1) % cellRows;
			}

			return seeds;
		}

		#region DEBUG

		public bool projectOnTerrain = true;
		public bool drawSeeds = true;
		public bool drawGrid = true;
		protected Color[] seedColors = Array.Empty<Color>();

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		protected virtual void OnDrawGizmos()
		{
			if (drawSeeds) DrawSeeds();
			if (drawGrid) DrawBoundingBox();
		}

		protected void DrawBoundingBox()
		{
			Matrix4x4 matrix = transform.localToWorldMatrix;
			Color gridColor = Color.blue;
			if (seedsDistribution == SeedsDistribution.Random)
				DrawBoundingBox(matrix, gridColor);
			else
				DrawGrid(matrix, gridColor);
		}

		private void DrawBoundingBox(Matrix4x4 matrix, Color color = default)
		{
			if (projectOnTerrain)
				GizmosExtensions.DrawQuadWire_OnTerrain(matrix, 5, color);
			else
				GizmosExtensions.DrawQuadWire(matrix, 5, color);
		}

		private void DrawGrid(Matrix4x4 matrix, Color color = default)
		{
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Count));
			if (projectOnTerrain)
				GizmosExtensions.DrawGrid_OnTerrain(cellRows, cellRows, matrix, 5, color);
			else
				GizmosExtensions.DrawGrid(cellRows, cellRows, matrix, 5, color);
		}

		protected void DrawSeeds()
		{
			Gizmos.color = seedColors?.Length > 0 ? seedColors[0] : Color.grey;
			var terrain = Terrain.activeTerrain;
			Vector3[] seedsInWorld = projectOnTerrain
				? SeedsInWorld_XZ.Select(s => terrain.Project(s)).ToArray()
				: SeedsInWorld_XZ;
			for (var i = 0; i < seedsInWorld.Length; i++)
			{
				if (seedColors?.Length > 0)
					Gizmos.color = seedColors[i];
				Gizmos.DrawSphere(seedsInWorld[i], 0.1f);
			}
		}

		#endregion
	}
}
