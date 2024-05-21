using System;
using System.Linq;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators.Showcase
{
	public class VoronoiShowcase : VoronoiGenerator
	{
		public float speed = .1f;
		public float turnSpeed = 1f;
		private readonly float collisionMargin = .01f;

		private readonly int maxDirectionTries = 10;

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

		private Vector2 currentVel = Vector2.zero;

		private void MoveSeedsRandom()
		{
			for (var i = 0; i < seeds.Count; i++)
			{
				var canMove = false;
				var iterations = 0;
				do
				{
					canMove = MoveSeed(i) || iterations >= maxDirectionTries;
					Random.InitState(Random.Range(1, 9999999));
					iterations++;
				} while (!canMove);
			}
		}

		private bool MoveSeed(int i)
		{
			Vector2 newDir = (seedDirections[i] + Random.insideUnitCircle).normalized;
			newDir = Vector2.SmoothDamp(seedDirections[i], newDir, ref currentVel, Time.deltaTime / turnSpeed)
				.normalized;
			Vector2 newPos = seeds[i] + newDir * (speed * Time.deltaTime);

			// Si se va a salir de los Bounds, cambia de direccion
			if (!newPos.IsIn01() || !ValidPosition(i, newPos))
			{
				newDir = -newDir;
				newPos = (seeds[i] + newDir * (speed * Time.deltaTime)).Clamp01();
			}

			if (!ValidPosition(i, newPos)) return false;

			seedDirections[i] = newDir;
			seeds[i] = newPos;

			return true;
		}

		private bool ValidPosition(int seedIndex, Vector2 newPos)
			=> seeds[seedIndex].IsIn01() &&
			   !seeds
				   .Take(seedIndex) // Skipea i => [0, ..., i - 1, i + 1, ..., n]
				   .Concat(seeds.Skip(seedIndex + 1))
				   // Si alguno de los seeds esta demasiado cerca, no es valida
				   .Any(s => Vector2.Distance(newPos, s) < collisionMargin);

		protected override void OnDrawGizmos()
		{
			Matrix4x4 m = transform.localToWorldMatrix;

			for (var i = 0; i < seeds.Count; i++)
			{
				Vector2 pos = seeds[i];
				Vector2 dir = seedDirections[i];
				GizmosExtensions.DrawCircle(m.MultiplyPoint3x4(pos.ToV3xz()), Vector3.up, .1f, Color.red);
				GizmosExtensions.DrawArrowWire(
					m.MultiplyPoint3x4(pos.ToV3xz()),
					m.MultiplyVector(dir.ToV3xz()),
					Vector3.forward,
					.05f,
					color: Color.gray
				);
			}
		}
	}
}
