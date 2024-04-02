using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
    public static class VectorExtensions
    {
        public static Vector3 GetRandomPos(Vector3 min, Vector3 max) =>
            new(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)
            );
    }
}