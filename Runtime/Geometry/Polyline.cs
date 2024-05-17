using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Geometry
{
        public struct Polyline
        {
            public const float DEFAULT_THICKNESS = .1f;
            public const int DEFAULT_SMOOTHNESS = 5;
            
            public Vector3[] points;
            public bool loop;

            public int PointsCount => points.Length;
			
            public static Material Material => new(Shader.Find("Sprites/Default"));

            public Polyline(IEnumerable<Vector3> points, bool loop = false)
            {
                this.points = points.ToArray();
                this.loop = loop;
            }
			
            // 2D Constructor
            public Polyline(IEnumerable<Vector2> points, bool loop = false, bool XZplane = true)
            : this(points.Select(p => XZplane ? p.ToV3xz() : p.ToV3xy()).ToArray(), loop) { }
        }
    }
