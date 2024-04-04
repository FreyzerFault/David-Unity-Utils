using UnityEngine;

namespace DavidUtils.UI.Icons
{
    public class ArrowPointer : MonoBehaviour
    {
        private Transform _icon;

        public Transform target;
        public float radius = 10;

        public bool fixIcon = true;

        private void Awake()
        {
            _icon = transform.GetChild(0);
        }

        private void Update()
        {
            if (target != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    Vector3.forward,
                    (target.position - transform.position).normalized
                );
                transform.position = transform.parent.position +
                                     (target.position - transform.parent.position).normalized * radius;
            }

            // ICON sin rotar
            if (fixIcon) _icon.transform.rotation = Quaternion.identity;
        }
    }
}