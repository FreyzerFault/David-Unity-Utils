using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
	public static class RectExtensions
	{
		public static Rect ScaleBy(this Rect rect, Vector2 scale) => new(rect.position, rect.size.ScaleBy(scale));
	}
}
