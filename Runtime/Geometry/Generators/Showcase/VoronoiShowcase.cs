using System;
using System.Linq;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Geometry.Generators.Showcase
{
	public class VoronoiShowcase : VoronoiGenerator
	{
		public float speed = .1f;
		public float turnSpeed = 1f;
		private const float CollisionMargin = .01f;
		private const int MaxDirectionTries = 10;

		private Vector2[] _seedDirections = Array.Empty<Vector2>();


		#region UNITY

		protected override void Start()
		{
			AnimatedDelaunay = false;
			AnimatedVoronoi = false;

			base.Start();

			InitializeDirections();
		}

		protected override void Update()
		{
			base.Update();

			MoveSeedsRandom();
			OnSeedsUpdated();
		}

		#endregion

		
		#region SEED MOVING

		private void InitializeDirections() =>
			_seedDirections = seeds.Select(_ => Random.insideUnitCircle).ToArray();

		public override void OnSeedsUpdated()
		{
			base.OnSeedsUpdated();

			if (_seedDirections?.Length != seeds.Count)
				InitializeDirections();
		}

		private void MoveSeedsRandom()
		{
			for (var i = 0; i < seeds.Count; i++)
			{
				bool canMove;
				var iterations = 0;
				do
				{
					canMove = MoveSeed(i) || iterations >= MaxDirectionTries;
					Random.InitState(Random.Range(1, 9999999));
					iterations++;
				} while (!canMove);
			}
		}

		private Vector2 _currentVel = Vector2.zero;
		
		private bool MoveSeed(int i)
		{
			Vector2 newDir = (_seedDirections[i] + Random.insideUnitCircle).normalized;
			newDir = Vector2.SmoothDamp(_seedDirections[i], newDir, ref _currentVel, Time.deltaTime / turnSpeed)
				.normalized;
			Vector2 newPos = seeds[i] + newDir * (speed * Time.deltaTime);

			// Si se va a salir de los Bounds, cambia de direccion
			if (!newPos.IsIn01() || !ValidPosition(i, newPos))
			{
				newDir = -newDir;
				newPos = (seeds[i] + newDir * (speed * Time.deltaTime)).Clamp01();
			}

			if (!ValidPosition(i, newPos)) return false;

			_seedDirections[i] = newDir;
			seeds[i] = newPos;

			return true;
		}

		private bool ValidPosition(int seedIndex, Vector2 newPos)
			=> seeds[seedIndex].IsIn01() &&
			   !seeds
				   .Take(seedIndex) // Skipea i => [0, ..., i - 1, i + 1, ..., n]
				   .Concat(seeds.Skip(seedIndex + 1))
				   // Si alguno de los seeds esta demasiado cerca, no es valida
				   .Any(s => Vector2.Distance(newPos, s) < CollisionMargin);

		#endregion


		#region UI CONTROL

		public float Speed
		{
			get => speed;
			set => speed = value;
		}

		public float TurnSpeed
		{
			get => turnSpeed;
			set => turnSpeed = value;
		}

		#endregion


		#region DEBUG
		
		#if UNITY_EDITOR

		protected override void OnDrawGizmos()
		{
			for (var i = 0; i < seeds.Count; i++)
			{
				Vector3 pos = LocalToWorldMatrix.MultiplyPoint3x4(seeds[i]);
				Vector3 dir = LocalToWorldMatrix.MultiplyVector(_seedDirections[i]);
				GizmosExtensions.DrawCircle(pos, Vector3.up, .1f, Color.red);
				GizmosExtensions.DrawArrow(
					GizmosExtensions.ArrowCap.Triangle,
					pos,
					dir * 0.02f,
					Vector3.up,
					.1f,
					Color.gray,
					0.5f
				);
			}
		}
		
		#endif

		#endregion
	}
}
