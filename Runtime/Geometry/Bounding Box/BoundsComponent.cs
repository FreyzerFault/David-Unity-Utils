using DavidUtils.CustomAttributes;
using UnityEngine;

namespace DavidUtils.Geometry.Bounding_Box
{
    public class BoundsComponent : MonoBehaviour
    {
        public bool is2D;
        
        [ConditionalField("is2D", true)]
        public Bounds bounds3D;
        
        [ConditionalField("is2D", false)]
        public Bounds2D bounds2D = Bounds2D.NormalizedBounds;

        [ConditionalField("is2D", false)]
        public bool XZplane = true;

        public Vector3 Center
        {
            get => bounds3D.center;
            set
            {
                bounds3D.center = value;
                Sincronize2D();
            }
        }

        public Vector3 Size
        {
            get => bounds3D.size;
            set
            {
                bounds3D.size = value;
                Sincronize2D();
            }
        }

        public Vector2 Size2D
        {
            get => bounds2D.Size;
            set
            {
                bounds2D.Size = value;
                Sincronize3D();
            }
        }
        
        public Vector3 Min => bounds3D.min;
        public Vector3 Max => bounds3D.max;
        
        public Matrix4x4 LocalToBoundsMatrix => Matrix4x4.TRS(Min, Quaternion.identity, Size);

        private void Awake() => SincronizeBounds();

        private void OnEnable() => SincronizeBounds();

        private void OnValidate() => SincronizeBounds();

        
        private void SincronizeBounds()
        {
            if (is2D) Sincronize3D();
            else Sincronize2D();
        }

        private void Sincronize2D() => bounds2D = new Bounds2D(bounds3D, XZplane);
        private void Sincronize3D() => bounds3D = bounds2D.To3D(XZplane, XZplane ? bounds3D.size.y : bounds3D.size.z);
    }
}
