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
	}
}
