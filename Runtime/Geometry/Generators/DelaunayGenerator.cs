using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
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

			_renderer.Clear();
		}


		public virtual void Run()
		{
			Reset();
			animationCoroutine = StartCoroutine(RunCoroutine());
		}

		protected virtual IEnumerator RunCoroutine()
		{
			if (animatedDelaunay)
			{
				while (!delaunay.ended)
				{
					delaunay.Run_OnePoint();
					OnTrianglesUpdated();
					yield return new WaitForSecondsRealtime(delayMilliseconds);
				}
			}
			else
			{
				delaunay.RunTriangulation();
				OnTrianglesUpdated();
			}
		}

		protected virtual void Run_OneIteration() => delaunay.Run_OnePoint();

		private void OnTrianglesUpdated() => _renderer.Update(Triangles.ToArray());


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

			_renderer.Update(tris.ToArray());

			return true;
		}

		#endregion


		#region RENDERING

		[Serializable]
		private class Renderer
		{
			// MESH
			public MeshRenderer meshRenderer;
			public MeshFilter meshFilter;

			// LINE
			public Transform lineParent;
			public List<LineRenderer> lineRenderers;

			public bool active = true;
			public bool wire;

			public void Initialize(Transform parent)
			{
				lineParent = ObjectGenerator.InstantiateEmptyObject(parent, "DELAUNAY Line Renderers").transform;

				MeshExtensions.InstantiateMeshRenderer(
					out meshRenderer,
					out meshFilter,
					new Mesh(),
					parent,
					"VORONOI Mesh Renderers"
				);

				UpdateVisibility();
			}

			public void UpdateVisibility()
			{
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
					InstatiateLineRenderer(triangle, colors[i]);
				}
				else
				{
					lineRenderers[i].SetPoints(triangle.Vertices3D_XY.ToArray());
				}
			}

			public void Update(Triangle[] triangles)
			{
				if (triangles.Length != colors.Count)
					SetRainbowColors(triangles.Length);

				// MESH
				meshFilter.sharedMesh = triangles.CreateMesh(colors.ToArray());

				// LINE
				for (var i = 0; i < triangles.Length; i++)
					UpdateTri(triangles[i], i);

				if (triangles.Length >= lineRenderers.Count) return;

				// Elimina los Renderers sobrantes
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
			}


			#region COLOR

			public List<Color> colors;

			protected void SetRainbowColors(int numColors)
			{
				if (colors == null || colors.Count == 0)
				{
					colors = Color.cyan.GetRainBowColors(numColors).ToList();
					return;
				}

				if (numColors > colors.Count)
					colors.AddRange(colors[^1].GetRainBowColors(numColors - colors.Count));
				else if (numColors < colors.Count)
					colors.RemoveRange(numColors, colors.Count - numColors);
			}

			#endregion
		}

		private Renderer _renderer;

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			_renderer.Initialize(transform);
		}

		#endregion

		#region UI CONTROL

		public bool DrawDelaunay
		{
			get => delaunay.draw;
			set
			{
				delaunay.draw = value;
				_renderer.UpdateVisibility();
			}
		}
		public bool DelaunayWire
		{
			get => delaunay.draw && delaunay.drawWire;
			set
			{
				delaunay.drawWire = value;
				_renderer.UpdateVisibility();
			}
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			delaunay.OnDrawGizmos(transform.localToWorldMatrix);
		}

#endif

		#endregion
	}
}
