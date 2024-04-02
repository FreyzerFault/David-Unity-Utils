using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
    public static class BoundsExtensions
    {
        // Devuelve un punto aleatorio dentro de los limites de la caja
        // con un offset para que los objetos extensos spawneen contenidos en la caja
        // ignoreHeight = true -> spawnea en el suelo de la caja siempre
        public static Vector3 GetRandomPointInBounds(this Bounds bounds, Vector3 offsetPadding, bool ignoreHeight = false)
        {
            Vector3 maxPosition = bounds.max;
            Vector3 minPosition = bounds.min;

            // Sumamos el offset al MINIMO y lo restamos al MAXIMO
            // para hacer unos margenes y que no spawnee medio objeto fuera de la caja 
            minPosition += offsetPadding;
            maxPosition -= offsetPadding;

            // Usa el suelo de la caja si ignora la altura
            if (ignoreHeight)
                maxPosition.y = minPosition.y;

            return VectorExtensions.GetRandomPos(minPosition, maxPosition);
        }
    }
}