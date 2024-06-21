using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Geometry.Generators;
using UnityEngine;

namespace DavidUtils.Geometry.Testing
{
	public class VoronoiTest : TestRunner
	{
		private VoronoiGenerator generator;
		public VoronoiGenerator Generator => generator ??= GetComponent<VoronoiGenerator>();

		public int seed = 999;


		protected override void InitializeTests()
		{
			AddTest(
				Generator.RunCoroutine,
				new TestInfo(
					"VORONOI",
					VoronoiIsValid
				)
			);

			OnStartTest = () =>
			{
				if (iterations == 0)
				{
					Generator.randSeed = seed;
					Generator.GenerateSeeds();
				}
				else
				{
					Generator.RandomizeSeeds();
				}

				Debug.Log($"<color=#00aaaa><b> Test #{iterations}: seed {Generator.randSeed}</b> </color>", this);
			};
		}

		private bool VoronoiIsValid()
		{
			bool regionsGenerated = Generator.Regions.NotNullOrEmpty();
			bool cornersIn4Regions = Generator.Regions.Count(
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
