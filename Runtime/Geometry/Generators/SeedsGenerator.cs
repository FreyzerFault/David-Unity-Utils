﻿using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.DevTools.Reflection;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Rendering;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators
{
	[RequireComponent(typeof(BoundsComponent))]
	public class SeedsGenerator : MonoBehaviour
	{
		public enum SeedsDistribution { Random, Regular, SinWave }

		[Header("SEEDS")]
		[ExposedField]
		public int randSeed = 10;

		[ExposedField] [SerializeField]
		private int numSeeds = 10;
		public int NumSeeds
		{
			get => numSeeds;
			set
			{
				numSeeds = value;
				GenerateSeeds();
				OnSeedsUpdated();
			}
		}
		
		public SeedsDistribution seedsDistribution = SeedsDistribution.Random;

		[HideInInspector] public List<Vector2> seeds = new();
		public List<Vector2> Seeds
		{
			get => seeds;
			set
			{
				seeds = value;
				NumSeeds = seeds.Count;
				OnSeedsUpdated();
			}
		}

		public bool SeedsAreGenerated => seeds?.Count == NumSeeds;

		#region BOUNDS

		[Space] private BoundsComponent boundsComp;
		public BoundsComponent BoundsComp => boundsComp ??= GetComponent<BoundsComponent>();
		public AABB_2D AABB => BoundsComp.aabb2D;

		public Matrix4x4 WorldToLocalMatrix => BoundsComp.WorldToLocalMatrix;
		public Matrix4x4 LocalToWorldMatrix => BoundsComp.LocalToWorldMatrix;
		public Vector3 ToWorld(Vector2 pos) => LocalToWorldMatrix.MultiplyPoint3x4(pos.ToV3xz());

		#endregion


		#region UNITY

		protected virtual void Awake()
		{
			Reset();
			GenerateSeeds();
			InitializeRenderer();

			BoundsComp.OnChanged += PositionRenderer;
		}

		protected virtual void Start() => InstantiateRenderer();

		#endregion


		#region MAIN METHODS

		public virtual void Reset()
		{
			seeds = new List<Vector2>();
			Renderer.Clear();
		}

		public virtual void OnSeedsUpdated() => UpdateRenderer();

		/// <summary>
		///     Randomiza la Generación de Seeds
		/// </summary>
		public void RandomizeSeeds() => RandomizeSeeds(-1);

		public void RandomizeSeeds(int newRandSeed)
		{
			randSeed = newRandSeed == -1 ? Random.Range(1, int.MaxValue) : newRandSeed;
			GenerateSeeds();
			OnSeedsUpdated();
		}

		public void GenerateSeeds()
		{
			seeds = GenerateSeeds(NumSeeds, randSeed, seedsDistribution).ToList();
			DeleteRedundant();
		}

		private void DeleteRedundant() => seeds = seeds.Where(
				(s1, i) => seeds
					.Where((_, j) => j != i) // Exclude itself
					.All(s2 => Vector2.Distance(s1, s2) > 0.01f) // Todos superan el minimo de distancia
			)
			.ToList();

		#endregion


		#region SEEDS MODIFICATION

		/// <summary>
		///     Mueve una semilla a una nueva posición siempre y cuando:
		///     No colisione con otra, no sea la misma posición y esté dentro del rango [0,1]
		/// </summary>
		/// <returns>True si Cumple con los requisitos y se ha movido</returns>
		public virtual bool MoveSeed(int index, Vector2 newPos)
		{
			newPos = newPos.Clamp01();
			Vector2 oldPos = seeds[index];
			if (newPos == oldPos)
				return false;

			// Si colisiona con otra semilla no la movemos
			bool collision = seeds
				.Where((_, i) => i != index)
				.Any(p => Vector2.Distance(p, newPos) < 0.001f);

			if (collision) return false;

			seeds[index] = newPos;

			Renderer.UpdateObj(index, newPos);

			return true;
		}

		#endregion


		#region RENDERER

		[Space]
		private PointsRenderer _seedsRenderer;
		private PointsRenderer Renderer => _seedsRenderer ??= GetComponentInChildren<PointsRenderer>(true);

		private readonly bool projectOnTerrain = true;

		public bool DrawSeeds
		{
			get => Renderer.Active;
			set => Renderer.Active = value;
		}

		protected virtual void InitializeRenderer()
		{
			_seedsRenderer ??= Renderer ?? UnityUtils.InstantiateObject<PointsRenderer>(transform, "Seeds Renderer");
			_seedsRenderer.ToggleShadows(false);
			PositionRenderer();
		}

		protected virtual void InstantiateRenderer()
		{
			if (Renderer == null) return;
			Renderer.InstantiateObjs(seeds.ToV3(), "Seed");
		}

		protected virtual void UpdateRenderer() => Renderer.UpdateAllObj(seeds.ToV3());

		/// <summary>
		/// Posiciona el Renderer de las Seeds ajustado al AABB
		/// </summary>
		protected virtual void PositionRenderer()
		{
			if (Renderer == null) return;
			BoundsComp.TransformToBounds_Local(Renderer);
			Renderer.transform.Translate(Vector3.back * .5f);
		}

		#endregion

		
		#region UI CONTROL

		[ExposedField]
		public string NumSeedsStr
		{
			get => numSeeds.ToString();
			set
			{
				bool isDigit = int.TryParse(value, out int num);
				if (!isDigit) num = 16;
				NumSeeds = num;
			}
		}

		#endregion


		#region GENERATION ALGORITHMS

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
				if (cellRow == 0)
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

		#endregion


		#region DEBUG

		public bool drawGizmos;
		public bool drawGrid = true;

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		protected virtual void OnDrawGizmos()
		{
			if (!drawGizmos) return;
			if (drawGrid) GizmosGrid(Color.blue);
		}

		private void GizmosGrid(Color color = default, float thickness = 2)
		{
			if (seeds.IsNullOrEmpty()) return;

			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(NumSeeds));
			if (projectOnTerrain && Terrain.activeTerrain != null)
				GizmosExtensions.DrawGrid_OnTerrain(cellRows, cellRows, LocalToWorldMatrix, thickness, color);
			else
				GizmosExtensions.DrawGrid(cellRows, cellRows, LocalToWorldMatrix, thickness, color);
		}

		#endregion
	}
}
