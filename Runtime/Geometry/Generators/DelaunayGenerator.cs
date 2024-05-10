using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

		[FormerlySerializedAs("animated")] public bool animatedDelaunay = true;
		[FormerlySerializedAs("delay")] public float delayMilliseconds = 0.1f;
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

		protected virtual void Update()
		{
			// NEXT ITERATION
			if (Input.GetKeyDown(KeyCode.Space)) Run_OneIteration();

			// STOP ANIMATION
			if (Input.GetKeyDown(KeyCode.Escape)) StopCoroutine(animationCoroutine);
		}

		public virtual void Initialize()
		{
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
				while (!delaunay.ended)
				{
					delaunay.Run_OnePoint();
					yield return new WaitForSecondsRealtime(delayMilliseconds);
				}
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
