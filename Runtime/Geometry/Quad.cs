using UnityEngine;

namespace DavidUtils.Geometry
{
	public struct Quad
	{
		public Vector3[] vertices;

		public Quad(Vector3 center, float size, Vector3 normal)
		{
			if (normal == default) normal = Vector3.up;

			// Corners depend on normal
			vertices = new[]
			{
				center + Quaternion.AngleAxis(45, normal) * Vector3.right * size / 2,
				center + Quaternion.AngleAxis(45, normal) * Vector3.forward * size / 2,
				center + Quaternion.AngleAxis(45, normal) * Vector3.left * size / 2,
				center + Quaternion.AngleAxis(45, normal) * Vector3.back * size / 2
			};
		}
	}
}
