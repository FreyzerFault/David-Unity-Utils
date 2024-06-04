using DavidUtils.CustomAttributes;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
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
			get => is2D ? aabb2D.Size.ToV3(XZplane) : bounds3D.size;
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

		public Vector3 Min => is2D ? aabb2D.min.ToV3(XZplane) : bounds3D.min;
		public Vector3 Max => is2D ? aabb2D.max.ToV3(XZplane) : bounds3D.max;

		public Matrix4x4 BoundsToLocalMatrix =>
			Matrix4x4.Scale(is2D ? aabb2D.Size.Inverse() : bounds3D.size.Inverse()) *
			Matrix4x4.Rotate(is2D && XZplane ? AABB_2D.RotationToXYplane : Quaternion.identity) *
			Matrix4x4.Translate(-Min);

		public Matrix4x4 LocalToBoundsMatrix => Matrix4x4.TRS(
			Min,
			is2D && XZplane ? AABB_2D.RotationToXZplane : Quaternion.identity,
			is2D ? aabb2D.Size : bounds3D.size
		);
		public Matrix4x4 WorldToLocalMatrix => BoundsToLocalMatrix * transform.worldToLocalMatrix;
		public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix * LocalToBoundsMatrix;

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

		#region DEBUG

#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			SincronizeBounds();
			if (is2D)
				GizmosExtensions.DrawQuadWire(LocalToWorldMatrix, 5, Color.green);
			else
				GizmosExtensions.DrawCubeWire(LocalToWorldMatrix, 5, Color.green, false);
		}

#endif

		#endregion

		public void AdjustTransformToBounds(MonoBehaviour obj)
		{
			obj.transform.localPosition = Min;
			obj.transform.localRotation = XZplane ? AABB_2D.RotationToXZplane : Quaternion.identity;
			obj.transform.localScale = is2D ? aabb2D.Size.ToV3xy().WithZ(1) : bounds3D.size;
		}
	}
}
