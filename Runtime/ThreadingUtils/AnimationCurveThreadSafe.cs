﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DavidUtils.ThreadingUtils
{
    [BurstCompile]
    public struct SampledAnimationCurve : IDisposable
    {
        private NativeArray<float> _sampledCurve;

        public bool IsEmpty => !_sampledCurve.IsCreated || _sampledCurve.Length == 0;

        public SampledAnimationCurve(AnimationCurve curve, int samples)
            : this()
        {
            if (curve == null) return;

            Sample(curve, samples);
        }

        /// <param name="samples">Must be 2 or higher</param>
        public void Sample(AnimationCurve curve, int samples)
        {
            if (!_sampledCurve.IsCreated || _sampledCurve.Length != samples)
            {
                _sampledCurve.Dispose();
                _sampledCurve = new NativeArray<float>(samples, Allocator.Persistent);
            }

            var timeFrom = curve.keys[0].time;
            var timeTo = curve.keys[^1].time;
            var timeStep = (timeTo - timeFrom) / (samples - 1);

            for (var i = 0; i < samples; i++) _sampledCurve[i] = curve.Evaluate(timeFrom + i * timeStep);
        }

        public void Dispose() => _sampledCurve.Dispose();

        /// <param name="time">Must be from 0 to 1</param>
        public float Evaluate(float time)
        {
            var len = _sampledCurve.Length - 1;
            var clamp01 =
                time < 0
                    ? 0
                    : time > 1
                        ? 1
                        : time;
            var floatIndex = clamp01 * len;
            var floorIndex = (int)math.floor(floatIndex);
            if (floorIndex == len) return _sampledCurve[len];

            var lowerValue = _sampledCurve[floorIndex];
            var higherValue = _sampledCurve[floorIndex + 1];
            return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
        }
    }

    [BurstCompile]
    public struct AnimationCurveThreadSafe : IDisposable
    {
        public NativeArray<Keyframe> keys;

        public bool IsEmpty => !keys.IsCreated || keys.Length == 0;

        public AnimationCurveThreadSafe(AnimationCurve curve)
            : this()
        {
            if (curve == null) return;

            SetAnimationCurve(curve);
        }

        public void SetAnimationCurve(AnimationCurve curve)
        {
            if (keys.IsCreated && keys.Length == curve.keys.Length) return;

            keys.Dispose();
            keys = new NativeArray<Keyframe>(curve.keys, Allocator.Persistent);
        }

        public float Evaluate(float time)
        {
            for (var i = 0; i < keys.Length - 1; i++)
            {
                var key = keys[i];
                var nextKey = keys[i + 1];
                var inRange = key.time <= time && nextKey.time >= time;
                if (!inRange) continue;

                var dt = nextKey.time - key.time;

                var startWeight = key.outWeight;
                var endWeight = nextKey.inWeight;

                var startSlope = key.outTangent * dt;
                var endSlope = nextKey.inTangent * dt;

                var t2 = time * time;
                var t3 = t2 * time;

                var a = 2 * t3 - 3 * t2 + 1;
                var b = t3 - 2 * t2 + time;
                var c = t3 - t2;
                var d = -2 * t3 + 3 * t2;

                return a * key.value + b * startSlope + c * endSlope + d * nextKey.value;
                //
                // var t = Mathf.InverseLerp(key.time, nextKey.time, time);
                // return Mathf.Lerp(key.value, nextKey.value, t);
            }

            return keys[^1].value;
        }

        public void Dispose() => keys.Dispose();
    }
}