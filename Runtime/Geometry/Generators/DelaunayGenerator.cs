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

		protected override void Awake()
		{
			base.Awake();
			Initialize();
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

		public override void Initialize()
		{
			base.Initialize();

			if (animationCoroutine != null)
				StopCoroutine(animationCoroutine);

			delaunay ??= new Delaunay(seeds);
			delaunay.Seeds = seeds;
		}


		public virtual void Run()
		{
			Initialize();
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


		#region SEEDS

		public override void OnSeedsUpdated()
		{
			base.OnSeedsUpdated();

			Initialize();
			Run();
		}


		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			delaunay.Seeds = seeds;
			delaunay.RunTriangulation();

			UpdateRenderers();

			return true;
		}

		#endregion


		#region RENDERING

		private Color[] triColors;

		private MeshRenderer delaunayMeshRenderer;
		private MeshFilter delaunayMeshFilter;

		private GameObject delaunaylineParent;
		private LineRenderer[] delaunayLineRenderers;

		public bool DrawDelaunay
		{
			get => delaunay.draw;
			set
			{
				delaunay.draw = value;
				UpdateVisibility();
			}
		}
		public bool DelaunayWire
		{
			get => delaunay.draw && delaunay.drawWire;
			set
			{
				delaunay.drawWire = value;
				UpdateVisibility();
			}
		}

		private void FillColorsRainbow() => triColors = Color.cyan.GetRainBowColors(TrianglesCount, 0.05f);

		protected override void InitializeRenderObjects()
		{
			base.InitializeRenderObjects();

			// LINE PARENT
			delaunaylineParent = ObjectGenerator.InstantiateEmptyObject(transform, "DELAUNAY Line Renderers");

			var mObj = ObjectGenerator.InstantiateEmptyObject(transform, "DELAUNAY Line Renderers");
			delaunayMeshRenderer = mObj.AddComponent<MeshRenderer>();
			delaunayMeshFilter = mObj.AddComponent<MeshFilter>();
			
			ObjectGenerator.InstantiateMeshRenderer(new Mesh(), transform, out delaunayMeshRenderer, out delaunayMeshFilter, "DELAUNAY Mesh");

			ClearRenderers();
			UpdateVisibility();
		}

		private void OnTrianglesUpdated() => UpdateRenderers();

		protected virtual void UpdateRenderers()
		{
			// Update Colors
			if (triColors == null || triColors.Length != TrianglesCount)
				FillColorsRainbow();

			// MESH
			delaunayMeshFilter.sharedMesh = ObjectGenerator.CreateMesh(Triangles.ToArray(), true, triColors);

			// LINE
			// Vacio todos los Line Renderers
			foreach (LineRenderer lr in delaunayLineRenderers)
				lr.positionCount = 0;

			// Por cada Triangulo, actualizo un Line Renderer
			// Y cuando no haya suficientes, creo mas Line Renderers
			for (var i = 0; i < TrianglesCount; i++)
			{
				LineRenderer lr;
				Triangle tri = Triangles[i];

				if (i >= delaunayLineRenderers.Length)
				{
					lr = tri.InstantiateLineRenderer(delaunaylineParent.transform, Color.white);
					delaunayLineRenderers = delaunayLineRenderers.Append(lr).ToArray();
				}
				else
					delaunayLineRenderers[i].SetPoints(Triangles[i].Vertices.Select(v => v.ToV3xz()).ToArray());
			}

			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			delaunayMeshFilter.gameObject.SetActive(DrawDelaunay && !DelaunayWire);
			delaunaylineParent.SetActive(DelaunayWire);
		}

		protected override void ClearRenderers()
		{
			base.ClearRenderers();

			if (delaunayLineRenderers != null)
				foreach (LineRenderer t in delaunayLineRenderers)
					Destroy(t.gameObject);

			delaunayLineRenderers = Array.Empty<LineRenderer>();

			if (delaunayMeshFilter != null)
				delaunayMeshFilter.mesh.Clear();
		}

		#endregion


#if UNITY_EDITOR

		#region DEBUG

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			delaunay.OnDrawGizmos(transform.localToWorldMatrix);
		}

		#endregion

#endif
	}
}
