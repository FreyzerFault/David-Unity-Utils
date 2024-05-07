using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DavidUtils.Geometry.Generators
{
	public class DelaunayGenerator : SeedsGenerator
	{
		public Delaunay delaunay = new();
		protected List<Delaunay.Triangle> Triangles
		{
			get => delaunay.triangles;
			set => delaunay.triangles = value;
		}

		public bool runOnStart = true;

		public bool animated = true;
		public float delay = 0.1f;
		protected Coroutine animationCoroutine;

		protected override void Awake()
		{
			base.Awake();
			Initialize();
		}

		protected virtual void Start()
		{
			if (runOnStart) Run();
		}

		private void Update()
		{
			// NEXT ITERATION
			if (Input.GetKeyDown(KeyCode.Space)) Run_OneIteration();

			// STOP ANIMATION
			if (Input.GetKeyDown(KeyCode.Escape)) StopCoroutine(animationCoroutine);
		}

		public virtual void Initialize()
		{
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
			if (animated)
				yield return delaunay.AnimationCoroutine(delay);
			else
				delaunay.Run();
		}

		protected virtual void Run_OneIteration() => delaunay.Run_OnePoint();


		#region SEEDS

		// protected override void OnSeedsUpdated() => Run();

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
