using UnityEngine;

namespace DavidUtils.Geometry
{
	public class AABB2D
	{
		public Vector2 max;
		public Vector2 min;

		public AABB2D(Vector2 min, Vector2 max)
		{
			this.min = min;
			this.max = max;
		}

		public Vector3 Min3 => new(min.x, 0, min.y);
		public Vector3 Max3 => new(max.x, 0, max.y);

		public float Width => max.x - min.x;
		public float Height => max.y - min.y;
		public Vector2 Size => new(Width, Height);

		/// <summary>
		///     Comprueba si el Punto esta dentro del AABB
		/// </summary>
		/// <param name="p"></param>
		/// <returns>true si dentro</returns>
		public bool IsInside(Vector2 p) => p.x <= max.x && p.x >= min.x && p.y <= max.y && p.y >= min.y;

		// CLOCKWISE
		public Vector2[] Vertices => new[]
		{
			new Vector2(min.x, min.y),
			new Vector2(min.x, max.y),
			new Vector2(max.x, max.y),
			new Vector2(max.x, min.y)
		};
	}
}
