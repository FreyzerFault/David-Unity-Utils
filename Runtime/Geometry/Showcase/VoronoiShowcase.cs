using System;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Generators;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Showcase
{
	public class VoronoiShowcase : VoronoiGenerator
	{
		private readonly float speed = .1f;

		private Vector2[] seedDirections = Array.Empty<Vector2>();

		protected override void Awake()
		{
			Animated = false;

			base.Awake();
		}

		private void InitializeDirections() =>
			seedDirections = seeds.Select(_ => Random.insideUnitCircle).ToArray();

		public override void OnSeedsUpdated()
		{
			base.OnSeedsUpdated();

			if (seedDirections?.Length != seeds.Count)
				InitializeDirections();
		}

		protected override void Update()
		{
			base.Update();

			MoveSeedsRandom();
			OnSeedsUpdated();
		}

		private void MoveSeedsRandom()
		{
			for (var i = 0; i < seeds.Count; i++)
			{
				seedDirections[i] += Random.insideUnitCircle * .5f;
				seedDirections[i].Normalize();
				seeds[i] += seedDirections[i] * (speed * Time.deltaTime);
				seeds[i] = seeds[i].Clamp01();
			}
		}

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			Matrix4x4 m = transform.localToWorldMatrix;

			for (var i = 0; i < seeds.Count; i++)
			{
				Vector2 pos = seeds[i];
				Vector2 dir = seedDirections[i];
				GizmosExtensions.DrawArrowWire(m.MultiplyPoint3x4(pos.ToV3xz()), m.MultiplyVector(dir.ToV3xz()));
			}
		}
	}
}
