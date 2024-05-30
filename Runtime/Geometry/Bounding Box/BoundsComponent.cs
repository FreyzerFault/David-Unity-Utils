using DavidUtils.CustomAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Geometry.Bounding_Box
{
	public class BoundsComponent : MonoBehaviour
	{
		public bool is2D;

		[ConditionalField("is2D", true)]
		public Bounds bounds3D;

		[FormerlySerializedAs("bounds2D")]
		[ConditionalField("is2D", false)]
		public AABB_2D aabb2D = AABB_2D.NormalizedAABB;

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
			get => aabb2D.Size;
			set
			{
				aabb2D.Size = value;
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

		private void Sincronize2D() => aabb2D = new AABB_2D(bounds3D, XZplane);
		private void Sincronize3D() => bounds3D = aabb2D.To3D(XZplane, XZplane ? bounds3D.size.y : bounds3D.size.z);
	}
}
