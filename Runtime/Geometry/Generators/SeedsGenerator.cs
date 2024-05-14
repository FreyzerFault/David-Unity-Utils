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

		protected Color[] seedColors = Array.Empty<Color>();

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

		protected virtual void Awake()
		{
			GenerateSeeds();
			SetSeedsRainbowColors();
		}

		protected virtual void Start()
		{
			Initialize();
			InitializeRenderObjects();
			InstantiateSeeds();
		}

		public virtual void Initialize()
		{
		}

		public virtual void OnSeedsUpdated()
		{
			SetSeedsRainbowColors();
			InstantiateSeeds();
		}

		public void RandomizeSeeds()
		{
			randSeed = Random.Range(1, int.MaxValue);
			GenerateSeeds();
			OnSeedsUpdated();
		}

		public void GenerateSeeds() => seeds = GenerateSeeds(numSeeds, randSeed, seedsDistribution).ToList();

		#region COLOR

		protected void SetSeedsRainbowColors() =>
			seedColors = Color.red.GetRainBowColors(numSeeds);

		#endregion

		#region INSTANTIATE SPHERES IN WORLD

		private GameObject seedsParent;

		protected MeshRenderer[] spheresMr;
		protected MeshFilter[] spheresMf;


		public bool projectOnTerrain = true;
		[SerializeField] private bool drawSeeds = true;
		public bool DrawSeeds
		{
			get => drawSeeds;
			set
			{
				drawSeeds = value;
				UpdateVisibility();
			}
		}

		protected virtual void InitializeRenderObjects() => seedsParent = new GameObject("SEEDS")
		{
			transform =
			{
				parent = transform,
				localPosition = Vector3.zero,
				localRotation = Quaternion.identity,
				localScale = Vector3.one
			}
		};

		protected virtual void ClearRenderers()
		{
			if (spheresMf == null) return;
			foreach (MeshFilter meshFilter in spheresMf)
				Destroy(meshFilter.gameObject);

			spheresMf = Array.Empty<MeshFilter>();
			spheresMr = Array.Empty<MeshRenderer>();
		}

		protected void InstantiateSeeds()
		{
			ClearRenderers();
			spheresMf = new MeshFilter[seeds.Count];
			spheresMr = new MeshRenderer[seeds.Count];

			for (var i = 0; i < seeds.Count; i++)
			{
				Vector2 seed = seeds[i];
				var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.parent = transform;
				sphere.transform.localPosition = seed.ToV3xz();
				sphere.transform.localScale = Vector3.one * 0.3f / transform.lossyScale.x;

				spheresMr[i] = sphere.GetComponent<MeshRenderer>();
				spheresMf[i] = sphere.GetComponent<MeshFilter>();

				// COLOR
				Color[] colors = spheresMf[i].sharedMesh.vertices.Select(_ => seedColors[i].RotateHue(.5f)).ToArray();
				spheresMf[i].mesh.SetColors(colors);

				// MATERIAL
				spheresMr[i].sharedMaterial = Resources.Load<Material>("Materials/Geometry Unlit");
			}
		}


		/// <summary>
		///     Activa o desactiva los Renderers
		/// </summary>
		protected virtual void UpdateVisibility()
		{
			foreach (MeshFilter meshFilter in spheresMf)
				meshFilter.gameObject.SetActive(drawSeeds);
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

		#endregion

		#region DEBUG

		public bool drawGrid = true;

		private static readonly int BaseColorId = Shader.PropertyToID("_Base_Color");

		// Draw Quad Bounds and Grid if Seed Distribution is Regular along a Grid
		protected virtual void OnDrawGizmos()
		{
			if (drawSeeds) GizmosSeeds();
			if (drawGrid) GizmosBoundingBox();
		}

		protected void GizmosBoundingBox()
		{
			Matrix4x4 matrix = transform.localToWorldMatrix;
			Color gridColor = Color.blue;
			if (seedsDistribution == SeedsDistribution.Random)
				GizmosBoundingBox(matrix, gridColor);
			else
				GizmosGrid(matrix, gridColor);
		}

		private void GizmosBoundingBox(Matrix4x4 matrix, Color color = default)
		{
			if (projectOnTerrain)
				GizmosExtensions.DrawQuadWire_OnTerrain(matrix, 5, color);
			else
				GizmosExtensions.DrawQuadWire(matrix, 5, color);
		}

		private void GizmosGrid(Matrix4x4 matrix, Color color = default)
		{
			int cellRows = Mathf.FloorToInt(Mathf.Sqrt(seeds.Count));
			if (projectOnTerrain)
				GizmosExtensions.DrawGrid_OnTerrain(cellRows, cellRows, matrix, 5, color);
			else
				GizmosExtensions.DrawGrid(cellRows, cellRows, matrix, 5, color);
		}

		protected void GizmosSeeds()
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
