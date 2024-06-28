using System.Collections;
using System.Collections.Generic;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering;
using Geometry.Algorithms;
using UnityEngine;
using RenderMode = DavidUtils.Rendering.DelaunayRenderer.DelaunayRenderMode;

namespace DavidUtils.Geometry.Generators
{
	public class DelaunayGenerator : SeedsGenerator
	{
		#region DELAUNAY

		[Space]
		[Header("DELAUNAY TRIANGULATION")]
		public Delaunay delaunay = new();

		protected List<Triangle> Triangles
		{
			get => delaunay.triangles;
			set => delaunay.triangles = value;
		}
		protected int TrianglesCount => delaunay.triangles.Count;

		#endregion


		#region UNITY

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

		#endregion


		#region MAIN METHODS

		public override void Reset()
		{
			base.Reset();

			ResetDelaunay();
		}

		public void ResetDelaunay()
		{
			if (animationCoroutine != null)
				StopCoroutine(animationCoroutine);

			delaunay ??= new Delaunay(seeds);
			delaunay.Seeds = seeds;

			Renderer?.Clear();
		}


		public virtual void Run()
		{
			if (seeds.IsNullOrEmpty()) GenerateSeeds();
			
			ResetDelaunay();
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

		protected virtual void Run_OneIteration() => delaunay.Run_OnePoint();

		protected void OnTrianglesUpdated() => UpdateRenderer();

		#endregion


		#region ANIMATION

		public bool runOnStart = true;

		public bool animatedDelaunay = true;
		public float delayMilliseconds = 10;
		public float DelaySeconds => delayMilliseconds / 1000;
		protected Coroutine animationCoroutine;

		public virtual bool AnimatedDelaunay
		{
			get => animatedDelaunay && Renderer.gameObject.activeSelf;
			set
			{
				animatedDelaunay = value;
				Renderer.gameObject.SetActive(value);
			}
		}

		public virtual IEnumerator RunCoroutine()
		{
			ResetDelaunay();
			if (DrawDelaunay && AnimatedDelaunay)
			{
				while (!delaunay.ended)
				{
					delaunay.Run_OnePoint();
					OnTrianglesUpdated();
					yield return new WaitForSecondsRealtime(DelaySeconds);
				}
			}
			else
			{
				delaunay.RunTriangulation();
				OnTrianglesUpdated();
			}
		}

		#endregion


		#region SEEDS MODIFICATIONS

		public override void OnSeedsUpdated()
		{
			base.OnSeedsUpdated();

			Run();
		}

		public override bool MoveSeed(int index, Vector2 newPos)
		{
			bool moved = base.MoveSeed(index, newPos);

			if (!moved) return false;

			delaunay.Seeds = seeds;
			delaunay.RunTriangulation();

			UpdateRenderer();

			return true;
		}

		#endregion


		#region RENDERING

		[Space]
		[SerializeField]
		protected DelaunayRenderer delaunayRenderer;
		private DelaunayRenderer Renderer => delaunayRenderer ??=
			GetComponentInChildren<DelaunayRenderer>(true) 
			?? UnityUtils.InstantiateObject<DelaunayRenderer>(transform, "DELAUNAY Renderer");

		
		public bool DrawDelaunay
		{
			get => Renderer.gameObject.activeSelf;
			set => Renderer.gameObject.SetActive(value);
		}
		public bool DelaunayWire
		{
			get => delaunayRenderer.RenderMode == RenderMode.Wire;
			set => delaunayRenderer.RenderMode = value ? RenderMode.Wire : RenderMode.OutlineMesh;
		}
		
		protected override void InitializeRenderer()
		{
			delaunayRenderer ??= Renderer;
			delaunayRenderer.gameObject.ToggleShadows(false);
			delaunayRenderer.Delaunay = delaunay;

			base.InitializeRenderer();
		}

		protected override void UpdateRenderer()
		{
			base.UpdateRenderer();
			delaunayRenderer.UpdateDelaunay();
		}

		protected override void PositionRenderer()
		{
			base.PositionRenderer();
			if (Renderer == null) return;
			BoundsComp.TransformToBounds_Local(Renderer);
			Renderer.transform.Translate(Vector3.back * .5f);
		}

		
		
		#endregion



		#region DEBUG

#if UNITY_EDITOR

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (!drawGizmos || !DrawDelaunay) return;

			delaunay.OnDrawGizmos(LocalToWorldMatrix, DelaunayWire);
		}

#endif

		#endregion
	}
}
