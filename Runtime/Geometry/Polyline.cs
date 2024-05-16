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
            public Color[] colors;
            public float thickness;
            public int smoothness;
            public bool loop;

            public int PointsCount => points.Length;
			
            public int ColorCount => colors?.Length ?? 0;
            public Color[] Colors => colors == null || colors.Length == 0 ? new Color[] { Color.gray } : colors;
            public Color Color => Colors[0];
            public Gradient Gradient => Colors.ToGradient();
			
            public static Material Material => new(Shader.Find("Sprites/Default"));

            public Polyline(IEnumerable<Vector3> points, Color[] colors = null, float thickness = DEFAULT_THICKNESS, int smoothness = DEFAULT_SMOOTHNESS, bool loop = false)
            {
                this.points = points.ToArray();
                this.colors = colors;
                this.thickness = thickness;
                this.smoothness = smoothness;
                this.loop = loop;
            }
			
            // 2D Constructor
            public Polyline(IEnumerable<Vector2> points, Color[] colors = null, float thickness = DEFAULT_THICKNESS, int smoothness = DEFAULT_SMOOTHNESS, bool loop = false, bool XZplane = true)
            : this(points.Select(p => XZplane ? p.ToV3xz() : p.ToV3xy()).ToArray(), colors, thickness, smoothness, loop) { }

            public LineRenderer Instantiate(Transform parent, string name = "LINE")
            {
                GameObject obj = ObjectGenerator.InstantiateEmptyObject(parent, name);
                var lr = obj.AddComponent<LineRenderer>();
				
                if (ColorCount > 1)
                    lr.colorGradient = Gradient;
                else
                    lr.startColor = lr.endColor = Color;
				
                lr.sharedMaterial = Material;
                lr.widthMultiplier = thickness;
                lr.numCapVertices = lr.numCornerVertices = smoothness;
                lr.loop = loop;
				
                lr.positionCount = PointsCount;
                lr.SetPositions(points);

                lr.useWorldSpace = false;

                return lr;
            }
        }
    }
