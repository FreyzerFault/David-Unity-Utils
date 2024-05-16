using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
	public static class LineRendererExtensions
	{
		public static void CopyLineRendererPoints(this LineRenderer lr, LineRenderer other)
		{
			lr.positionCount = other.positionCount;
			var points = new Vector3[other.positionCount];
			other.GetPositions(points);
			lr.SetPositions(points);
		}

		public static void SetPoints(this LineRenderer lr, Vector3[] points)
		{
			if (lr.positionCount != points.Length)
				lr.positionCount = points.Length;
			lr.SetPositions(points);
		}
	}
}
