using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using DavidUtils.Geometry.Generators;
using UnityEngine;

namespace DavidUtils.Geometry.Testing
{
    public class VoronoiTest: TestRunner
    {
        public int seed = 999;
        private int iteration = 0;
        private VoronoiGenerator generator;
        public VoronoiGenerator Generator => generator ??= GetComponent<VoronoiGenerator>();
        
        
        protected override void Awake()
        {
            InitializeTests();
            base.Awake();
        }

        private void InitializeTests()
        {
            AddTest(RunVoronoiTest, new TestInfo(
                    $"VORONOI", 
                    VoronoiIsValid
                ));
        }
        
        private void RunVoronoiTest()
        {
            if (iteration == 0)
                Generator.randSeed = seed;
            else
                Generator.RandomizeSeeds();
            
            Generator.Run();
            
            Debug.Log($"<color=#00aa00><b> Test #{iteration}: seed {Generator.randSeed}</b> </color>", this);
            
            iteration++;
        }

        private bool VoronoiIsValid()
        {
            bool regionsGenerated = Generator.Regions.NotNullOrEmpty();
            bool cornersIn4Regions = Generator.Regions.Count(r => 
                r.Vertices.Any(v =>
                    AABB_2D.NormalizedAABB.Corners.Any(c => Vector2.Distance(c, v) < 0.01f))
            ) >= 4;
            return regionsGenerated && cornersIn4Regions;
        }
    }
}
