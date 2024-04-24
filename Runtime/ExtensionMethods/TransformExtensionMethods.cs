using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
    public static class TransformExtensionMethods
    {
        // Locks the rotation of the transform to the horizontal plane
        public static Transform LockRotationVertical(this Transform transform)
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            return transform;
        }

        public static void Billboard(
            this Transform transform,
            Transform target,
            bool verticalLock = false
        )
        {
            // transform.rotation = Quaternion.LookRotation(target.forward, target.up);
            transform.LookAt(target);
            if (verticalLock)
                transform.LockRotationVertical();
        }
    }
}
