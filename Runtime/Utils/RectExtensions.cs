using UnityEngine;

namespace DavidUtils.Utils
{
	public static class RectExtensions
	{
		public static Rect ScaleBy(this Rect rect, Vector2 scale) => new(rect.position, rect.size.ScaleBy(scale));
	}
}
