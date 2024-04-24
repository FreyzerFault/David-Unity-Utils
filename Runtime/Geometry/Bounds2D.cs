using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DavidUtils.Geometry
{
    public struct Bounds2D
    {
        public enum Side { Left, Right, Top, Bottom }
        
        public Vector2 min;
        public Vector2 max;
        
        public Vector2 bl => min;
        public Vector2 br => new Vector2(max.x, min.y);
        public Vector2 tl => new Vector2(min.x, max.y);
        public Vector2 tr => max;
        
        public Bounds2D(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(Vector2 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
        public bool OutOfBounds(Vector2 p) => !Contains(p);

        // Punto en el borde del rectangulo
        public bool PointOnBorder(Vector2 p, out Side side)
        {
            side = Side.Left;
            bool inBottom = GeometryUtils.PointOnSegment(p, bl, br);
            bool inRight = GeometryUtils.PointOnSegment(p, br, tr);
            bool inTop = GeometryUtils.PointOnSegment(p, tr, tl);
            bool inLeft = GeometryUtils.PointOnSegment(p, tl, bl);
            if (inBottom) side = Side.Bottom;
            if (inRight) side = Side.Right;
            if (inTop) side = Side.Top;
            if (inLeft) side = Side.Left;
            return inBottom || inRight || inTop || inLeft;
        }
        
        public Vector2 GetCorner(Side side0, Side side1) => side0 switch
        {
            Side.Left => side1 == Side.Bottom ? bl : tl,
            Side.Right => side1 == Side.Bottom ? br : tr,
            Side.Top => side1 == Side.Left ? tl : tr,
            Side.Bottom => side1 == Side.Left ? bl : br,
            _ => Vector2.zero
        };

        // Interseccion Segmento Segmento con los 4 lados del rectangulo
        public IEnumerable<Vector2> Intersections_Segment(Vector2 a, Vector2 b)
        {
            Vector2? ib = GeometryUtils.IntersectionSegmentSegment(a, b, bl, br);
            Vector2? ir = GeometryUtils.IntersectionSegmentSegment(a, b, br, tr);
            Vector2? it = GeometryUtils.IntersectionSegmentSegment(a, b, tl, tr);
            Vector2? il = GeometryUtils.IntersectionSegmentSegment(a, b, bl, tl);
            
            Vector2?[] intersections = { ib, ir, it, il };
            return intersections.Where(i => i.HasValue).Select(i => i.Value);
        }
        
        // Interseccion Ray Segmento con los 4 lados del rectangulo
        public IEnumerable<Vector2> Intersections_Ray(Vector2 p, Vector2 dir)
        {
            Vector2? ib = GeometryUtils.IntersectionRaySegment(p, dir, bl, br);
            Vector2? ir = GeometryUtils.IntersectionRaySegment(p, dir, br, tr);
            Vector2? it = GeometryUtils.IntersectionRaySegment(p, dir, tl, tr);
            Vector2? il = GeometryUtils.IntersectionRaySegment(p, dir, bl, tl);
            
            Vector2?[] intersections = { ib, ir, it, il };
            return intersections.Where(i => i.HasValue).Select(i => i.Value);
        }
    }
}
