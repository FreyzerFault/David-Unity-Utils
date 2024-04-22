using UnityEngine;

namespace DavidUtils.Geometry
{
	public struct Quad
	{
		public Vector3[] vertices;

		public Quad(Vector3 pos, Vector3 extent)
		{
			vertices = new[]
			{
				pos,
				pos + Vector3.Project(extent, Vector3.forward),
				pos + extent,
				pos + Vector3.Project(extent, Vector3.right)
			};
		}
	}
}
