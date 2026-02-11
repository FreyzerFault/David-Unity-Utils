using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.Utils;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Geometry.Generators;
using UnityEngine;

namespace DavidUtils.Tests.Runtime.Geometry
{
	public class VoronoiTest : TestRunner
	{
		private VoronoiGenerator _generator;
		public VoronoiGenerator Generator => _generator ??= GetComponent<VoronoiGenerator>();

		public int seed = 999;

		protected override void Awake()
		{
			base.Awake();
			
			_generator.Init();
		}


		protected override void InitializeTests()
		{
			AddTest(
				Generator.RunCoroutine,
				new TestInfo(
					"VORONOI",
					VoronoiIsValid
				)
			);

			onStartAllTests = () =>
			{
				if (iteration == 0)
				{
					Generator.randSeed = seed;
					Generator.GenerateSeeds();
				}
				else
				{
					Generator.RandomizeSeeds();
				}

				Debug.Log($"<color=#00aaaa><b> Test #{iteration}: seed {Generator.randSeed}</b> </color>", this);
			};
		}

		private bool VoronoiIsValid()
		{
			bool regionsGenerated = Generator.Polygons.NotNullOrEmpty();
			bool cornersIn4Regions = Generator.Polygons.Count(
				r =>
					r.Vertices.Any(
						v =>
							AABB_2D.NormalizedAABB.Corners.Any(c => Vector2.Distance(c, v) < 0.01f)
					)
			) >= 4;
			return regionsGenerated && cornersIn4Regions;
		}
	}
}
