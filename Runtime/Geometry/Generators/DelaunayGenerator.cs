using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Geometry.Rendering;
using DavidUtils.TerrainExtensions;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class DelaunayGenerator : SeedsGenerator
	{
		public Delaunay delaunay = new();
		protected List<Triangle> Triangles
		{
			get => delaunay.triangles;
			set => delaunay.triangles = value;
		}
		protected int TrianglesCount => delaunay.triangles.Count;

		public bool runOnStart = true;

		public bool animatedDelaunay = true;
		public float delayMilliseconds = 0.1f;
		protected Coroutine animationCoroutine;

		public virtual bool Animated
		{
			get => animatedDelaunay;
			set => animatedDelaunay = value;
		}

		protected override void Start()
		{
			base.Start();

			if (runOnStart) Run();
		}

		protected virtual void Update()
		{
			// NEXT ITERATION
			if (Input.GetKeyDown(KeyCode.Space)) Run_OneIteration();

			// STOP ANIMATION
			if (Input.GetKeyDown(KeyCode.Escape)) StopCoroutine(animationCoroutine);
		}

		public override void Reset()
		{
			base.Reset();

			if (animationCoroutine != null)
				StopCoroutine(animationCoroutine);

			delaunay ??= new Delaunay(seeds);
			delaunay.Seeds = seeds;

			delaunayRenderer.Clear();
		}


		public virtual void Run()
		{
			Reset();
			if (animatedDelaunay)
			{
				animationCoroutine = StartCoroutine(RunCoroutine());
			}
			else
			{
				delaunay.RunTriangulation();
				OnTrianglesUpdated();
			}
		}

		protected virtual IEnumerator RunCoroutine()
		{
			while (!delaunay.ended)
			{
				delaunay.Run_OnePoint();
				OnTrianglesUpdated();
				yield return new WaitForSecondsRealtime(delayMilliseconds);
			}
		}

		protected virtual void Run_OneIteration() => delaunay.Run_OnePoint();

		protected void OnTrianglesUpdated() => UpdateRenderer();


		#region SEEDS

		public override void OnSeedsUpdated()
		{
			base.OnSeedsUpdated();

			Reset();
			Run();
		}

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			delaunay.Seeds = seeds;
			List<Triangle> tris = delaunay.RunTriangulation();

			delaunayRenderer.Update(tris.ToArray());

			return true;
		}

		#endregion


		#region RENDERING

		[SerializeField] private Renderer delaunayRenderer;

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			delaunayRenderer.Initialize(transform);
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();

			delaunayRenderer.Update(Triangles.ToArray());
			if (projectOnTerrain && Terrain.activeTerrain != null)
				delaunayRenderer.ProjectOnTerrain(Terrain.activeTerrain);
		}

		protected virtual void UpdateRenderer()
		{
			base.UpdateRenderer();
			delaunayRenderer.Update(Triangles.ToArray());
		}

		[Serializable]
		private class Renderer
		{
			// MESH
			public MeshRenderer meshRenderer;
			public MeshFilter meshFilter;

			// LINE
			public Transform lineParent;
			public List<LineRenderer> lineRenderers = new();

			public LineRenderer borderLine;

			public bool active = true;
			public bool wire;

			public void Initialize(Transform parent)
			{
				lineParent = ObjectGenerator.InstantiateEmptyObject(parent, "DELAUNAY Line Renderers").transform;

				MeshRendererExtensions.InstantiateMeshRenderer(
					out meshRenderer,
					out meshFilter,
					new Mesh(),
					parent,
					"DELAUNAY Mesh Renderers"
				);

				lineParent.Translate(Vector3.up);
				meshRenderer.transform.Translate(Vector3.up);

				borderLine = LineRendererExtensions.LineRenderer(
					parent,
					"DELAUNAY Border Line",
					colors: new[] { Color.red },
					loop: true,
					thickness: .2f
				);
				borderLine.transform.Translate(Vector3.up * 2);

				UpdateVisibility();
			}

			public void UpdateVisibility()
			{
				borderLine.gameObject.SetActive(active);
				lineParent.gameObject.SetActive(active && wire);
				meshRenderer.gameObject.SetActive(active && !wire);
			}

			public void InstatiateLineRenderer(Triangle triangle, Color color) =>
				lineRenderers.Add(triangle.LineRenderer(lineParent, color));

			public void UpdateTri(Triangle triangle, int i)
			{
				if (i >= lineRenderers.Count)
				{
					SetRainbowColors(i + 1);
					InstatiateLineRenderer(triangle, Color.white);
				}
				else
				{
					lineRenderers[i].SetPoints(triangle.Vertices3D_XZ.ToArray());
				}
			}

			public void Update(Triangle[] triangles)
			{
				if (!active) return;

				if (triangles.Length != colors.Count)
					SetRainbowColors(triangles.Length);

				// MESH
				meshFilter.sharedMesh = triangles.CreateMesh(colors.ToArray());

				// LINE
				for (var i = 0; i < triangles.Length; i++)
					UpdateTri(triangles[i], i);

				// BORDER
				UpdateBorderLine(triangles);

				// Elimina los Renderers sobrantes
				if (triangles.Length >= lineRenderers.Count) return;

				int removeCount = lineRenderers.Count - triangles.Length;

				for (int i = triangles.Length; i < lineRenderers.Count; i++)
					Destroy(lineRenderers[i].gameObject);

				lineRenderers.RemoveRange(triangles.Length, removeCount);
			}

			public void Clear()
			{
				foreach (LineRenderer t in lineRenderers)
					Destroy(t.gameObject);

				lineRenderers.Clear();
				meshFilter.sharedMesh.Clear();

				borderLine.positionCount = 0;
			}

			public void UpdateBorderLine(Triangle[] tris)
			{
				Vector2[] points = tris.SelectMany(t => t.BorderEdges.Select(e => e.begin)).ToArray();
				points = points.SortByAngle(points.Center());
				borderLine.SetPoints(points.ToV3xz().ToArray());
			}


			public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f)
			{
				terrain.ProjectMeshInTerrain(meshFilter.sharedMesh, meshFilter.transform, offset);

				foreach (LineRenderer lr in lineRenderers)
					lr.SetPoints(lr.GetPoints().Select(p => terrain.Project(p, offset)).ToArray());
			}


			#region COLOR

			public List<Color> colors = new();

			protected void SetRainbowColors(int numColors)
			{
				if (colors == null || colors.Count == 0)
				{
					colors = Color.cyan.GetRainBowColors(numColors).ToList();
					return;
				}

				if (numColors > colors.Count)
					colors.AddRange(
						(colors.Count == 0 ? Color.magenta : colors.Last())
						.GetRainBowColors(numColors - colors.Count + 1)
						.Skip(1)
					);
				else if (numColors < colors.Count)
					colors.RemoveRange(numColors, colors.Count - numColors);
			}

			#endregion
		}

		#endregion

		#region UI CONTROL

		public bool DrawDelaunay
		{
			get => delaunayRenderer.active;
			set
			{
				delaunayRenderer.active = value;
				delaunayRenderer.UpdateVisibility();
			}
		}
		public bool DelaunayWire
		{
			get => delaunayRenderer.active && delaunayRenderer.wire;
			set
			{
				delaunayRenderer.wire = value;
				delaunayRenderer.UpdateVisibility();
			}
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (DrawDelaunay)
				delaunay.OnDrawGizmos(transform.localToWorldMatrix, DelaunayWire);
		}

#endif

		#endregion
	}
}
