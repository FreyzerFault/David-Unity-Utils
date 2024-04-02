using UnityEngine;

namespace DavidUtils
{
    public static class AnimationCurveUtils
    {
        public static AnimationCurve CopyCurve(AnimationCurve curve) => new(curve.keys);

        public static AnimationCurve DefaultCurve() => new(new Keyframe(0, 0), new Keyframe(1, 1));
        
        // Samplea la curva a una LOOK UP TABLE para paralelizar el Evaluate en Jobs
        public static float[] BakeLut(this AnimationCurve self, int sampleResolution = 1024)
        {
            var lookUpTable = new float[sampleResolution];
            for (var i = 0; i < sampleResolution; i++) 
                lookUpTable[i] = self.Evaluate((float) i / sampleResolution);
            return lookUpTable;
        }
    }
}