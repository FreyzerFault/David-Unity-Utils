using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
        public readonly struct Polyline
        {
            public const float DEFAULT_THICKNESS = .1f;
            public const int DEFAULT_SMOOTHNESS = 5;
            
            public readonly Vector3[] points;
            public readonly bool loop;

            public int PointsCount => points.Length;
			
            public static Material Material => new(Shader.Find("Sprites/Default"));

            public Polyline(IEnumerable<Vector3> points, bool loop = false)
            {
                this.points = points.ToArray();
                this.loop = loop;
            }
			
            // 2D Constructor
            public Polyline(IEnumerable<Vector2> points, bool loop = false, bool isXZplane = true)
            : this(points.Select(p => isXZplane ? p.ToV3XZ() : p.ToV3XY()).ToArray(), loop) { }
        }
    }
