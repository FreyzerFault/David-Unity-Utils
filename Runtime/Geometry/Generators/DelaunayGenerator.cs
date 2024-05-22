using System.Collections;
using System.Collections.Generic;
using DavidUtils.Rendering;
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

		[SerializeField] private Triangles2DRenderer delaunayRenderer = new();

		protected override void InitializeRenderer()
		{
			base.InitializeRenderer();
			delaunayRenderer.Initialize(transform, "DELAUNAY Renderer");

			delaunayRenderer.renderObj.transform.Translate(Vector3.up);
		}

		protected override void InstantiateRenderer()
		{
			base.InstantiateRenderer();

			delaunayRenderer.Update(Triangles.ToArray());
			if (projectOnTerrain && Terrain.activeTerrain != null)
				delaunayRenderer.ProjectOnTerrain(Terrain.activeTerrain);
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			delaunayRenderer.Update(Triangles.ToArray());
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
