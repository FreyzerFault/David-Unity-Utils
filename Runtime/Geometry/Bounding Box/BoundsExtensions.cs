using DavidUtils.Utils;
using UnityEngine;

namespace DavidUtils.Geometry.Bounding_Box
{
	public static class BoundsExtensions
	{
        /// <summary>
        ///     Devuelve un punto aleatorio dentro de los limites de la Bounding Box.
        ///     Respetando un offset para que objetos extensos no sobresalgan.
        /// </summary>
        public static Vector3 GetRandomPointInBounds(this Bounds bounds, Vector3 offsetPadding) =>
			VectorExtensions.GetRandomPos(bounds.min + offsetPadding, bounds.max - offsetPadding);

        public static void DrawGizmos(this Bounds bounds, float thickness = 1, Color color = default)
        {
	        Gizmos.color = color;
	        Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
	}
}
